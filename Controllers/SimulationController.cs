using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MSPGeneratorWeb.Models;
using MSPGeneratorWeb.ViewModel;
using MSPGeneratorWeb.HostedSimulationService;
using Microsoft.AspNet.Identity;

namespace MSPGeneratorWeb.Controllers
{
    /// <summary>
    /// <c>SimulationController</c> - třída pro nastavení parametrů simulace a zobrazení výsledků simulace.
    /// </summary>
    /// <remarks>Dostupné pouze přihlášeným uživatelům.</remarks>
    [Authorize]
    public class SimulationController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext(); // databáze pro web
        private SimulationServiceClient proxy_simulation; // propojení s back-endem
        private UserID tester; // pro simulaci
        private List<WebSenateModels> mySenates = new List<WebSenateModels>(); // alokace: seznam senátů přihlášeného uživatele
        private List<AlgorithmInfo> algo = new List<AlgorithmInfo>(); // alokace: seznam algoritmů ze serveru (back-end)
        public string UzivatelId { get; set; }

        #region PropertyHelpers
        /// <summary>
        /// <c>GetSenates</c> - pomocná metoda pro nastavení vlastnosti "mySenates" (seznamu senátů).
        /// </summary>
        /// <remarks>Data bere ze session nebo z databáze.</remarks>
        //
        // pomocná metoda (nelze volat z webu)
        private void GetSenates(bool fromDB = false)
        {
            // do "mySenates" uložíme seznam senátů přihlášeného uživatele (prioritně ze session, příp. z DB):
            if (fromDB || Session["senates"] == null) // chceme senáty z DB, příp. nejsou-li v session (vypršela?), bereme je z DB
                mySenates = db.WebSenateModels.Where(wsm => wsm.SenateCreator.Id == UzivatelId && wsm.Enabled == true).OrderBy(wsm => wsm.SenateName).ToList();
            else // vezmeme senáty ze session (https://stackoverflow.com/questions/1259934/store-list-to-session)
                mySenates = (List<WebSenateModels>)Session["senates"];
        }

        /// <summary>
        /// <c>HtmlSenates</c> - pomocná metoda pro výpis seznamu senátů ve View.
        /// </summary>
        /// <returns>Vrací řetězec (kód HTML pro odrážkový seznam).</returns>
        /// <remarks>Data bere z vlastnosti "mySenates" nebo ze svého druhého vstupu (odlišné datové typy!).</remarks>
        //
        // pomocná metoda (nelze volat z webu)
        private string HtmlSenates(bool fromProperty = true, List<Senate> senates = null)
        {
            string htmlOut;
            // vrátíme HTML (odrážkový seznam senátů) pro výpis na web
            htmlOut = "";
            if (fromProperty) // List<WebSenateModels>
            {
                foreach (WebSenateModels sen in mySenates) // z vlastnosti "mySenates"
                    htmlOut += "<li>" + sen.SenateName + " (zatížení: " + sen.Load.ToString() + ", aktuální vytíženost: " + sen.Acases.ToString() + ")</li>";
            }
            else // List<Senate>
            {
                foreach (Senate sen in senates) // ze vstupu "senates"
                    htmlOut += "<li>" + sen.ID + " (zatížení: " + sen.Load.ToString() + ", aktuální vytíženost: " + sen.ActiveCases.ToString() + ")</li>";
            }
            if (htmlOut != "")
                htmlOut = "<ul>" + htmlOut + "</ul>"; // seznam senátů mohl být prázdný
            return htmlOut;
        }


        /// <summary>
        /// <c>Initialize</c> - pomocná metoda pro nastavení vlastností "UzivatelId" a "tester".
        /// </summary>
        /// <returns>Vrací řetězec (kód HTML pro odrážkový seznam).</returns>
        /// <remarks>Data bere z vlastnosti "mySenates" nebo ze svého druhého vstupu (odlišné datové typy!).</remarks>
        //
        // pomocná metoda (nelze volat z webu)
        private void Initialize()
        {
            UzivatelId = User.Identity.GetUserId(); // ID přihlášeného uživatele
            tester = new UserID(User.Identity.Name); // username přihlášeného uživatele (objekt pro back-end)
        }


        /// <summary>
        /// <c>InitializeBackEnd</c> - pomocná metoda pro nastavení vlastností "proxy_simulation" a "algo".
        /// </summary>
        /// <remarks>Připojuje se k serverové části (back-end), kde získá seznam dostupných algoritmů.</remarks>
        //
        // pomocná metoda (nelze volat z webu)
        private string InitializeBackEnd()
        {
            string myError = null;
            try
            {
                proxy_simulation = new SimulationServiceClient("BasicHttpBinding_SimulationService"); // propojení s back-endem
                algo = proxy_simulation.GetAlgorithmInfo(tester);  // seznam objektů AlgorithmInfo (ze serveru)
            }
            catch (Exception e)
            {
                myError = "Chyba: nepodařilo se spojit s jádrem aplikace (back-end). Jedná se o chybu serveru - kontaktujte správce GNP (viz odkaz dole).";
            }

            return myError;
        }

        #endregion

        #region WebActions
        /// <summary>
        /// <c>Index</c> (GET) - "webová" metoda pro vykreslení formuláře, kde se zadávají parametry simulace (URL: /Simulation nebo /Simulation/Index; HTTP GET).
        /// </summary>
        /// <remarks><para>ViewModel: SimulationViewModels.cs, třída SimulationViewModel.</para><para>Views: Simulation/Index.cshtml + _IndexPartial.cshtml.</para><para>Pouze pro přihlášené uživatele.</para></remarks>
        //
        // GET: Simulation
        public ActionResult Index()
        {
            string resInitBE;

            Initialize();
            resInitBE = InitializeBackEnd(); // připojení k serveru s back-endem
            if (resInitBE != null)
            {
                TempData.Add("error", resInitBE); // chybové hlášení pro přesměrování
                return RedirectToAction("Error", "Simulation");
            }
            proxy_simulation.Close(); // odpojení od back-endu

            for (int a = 0; a < algo.Count; a++) // konverze dvojteček na mezery v názvech algoritmů
                algo[a].AlgorithmName = ConvertAlgorithmName(algo[a].AlgorithmName);
            // seznam senátů přihlášeného uživatele:
            Session["senates"] = null; // vyprázdnit data od minule (pokud již proběhla jiná simulace)
            Session["equiv"] = null;
            GetSenates(true); // explicitně vybereme data o senátech z DB (mohly nastat změny)

            // hodnoty pro View:
            ViewBag.alg_n = algo.Count;
            ViewBag.sen_n = mySenates.Count;
            ViewBag.Message = null;

            SimulationViewModel SVM = new SimulationViewModel(mySenates, 1, algo, 1); // implic. hodnoty do formuláře
            Session["senates"] = mySenates;
            Session["equiv"] = SVM.IsEquiv;
            ViewBag.senates = HtmlSenates(); // výpis vybraných senátů do HTML

            return View(SVM);
        }

        /// <summary>
        /// <c>Index</c> (POST) - "webová" metoda pro zpracování přihlašovacího formuláře (URL: /Simulation/Index; HTTP POST).
        /// </summary>
        /// <remarks><para>Vstupem jsou data z formuláře pro nastavení parametrů simulace.</para><para>Pokud nastane nějaká chyba (např. není vybraný algoritmus), znovu vykreslí formulář (model a view jsou stejné jako u GET-varianty této metody).</para><para>Pokud je formulář správně vyplněn, tak přesměruje na /Simulation/Run, která data pošle na back-end a zobrazí výsledky simulace.</para><para>Pouze pro přihlášené uživatele.</para></remarks>
        //
        // POST: Simulation
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(int NumCases, int NumIters, int?[] algos) //([Bind(Include = "NumCases,NumIters,alg[]")] SimulationViewModel SVM)
        {
            int i = 1; // pro číslování chyb, které mohly nastat "mimo" během vyplňování formuláře
            string resInitBE;
            List<Senate> senatesToSim = new List<Senate>();
            List<AlgorithmInfo> algInfo = new List<AlgorithmInfo>();

            ViewBag.Error = null;
            Initialize();
            resInitBE = InitializeBackEnd(); // připojení k serveru s back-endem
            if (resInitBE != null)
            {
                TempData.Add("error", resInitBE); // chybové hlášení pro přesměrování
                return RedirectToAction("Error", "Simulation");
            }
            proxy_simulation.Close(); // odpojení od back-endu

            for (int a = 0; a < algo.Count; a++) // konverze dvojteček na mezery v názvech algoritmů
                algo[a].AlgorithmName = ConvertAlgorithmName(algo[a].AlgorithmName);

            // seznam senátů přihlášeného uživatele (prioritně ze session, příp. z DB):
            GetSenates(); // nastaví vlastnost "mySenates"

            if (mySenates.Count == 0) // máme teď nějaké senáty?
            {
                ViewBag.Error += "Chyba" + i++.ToString() + ": v průběhu nastavování parametrů simulace došlo k odstranění senátů!<br>";
                ViewBag.sen_n = 0;
            }

            if (NumCases < 1 || NumCases > 10000)
                ViewBag.Error += "Chyba" + i++.ToString() + ": počet případů musí být číslo od 1 do 10000!<br>";
            if (NumIters < 1 || NumIters > 10000)
                ViewBag.Error += "Chyba" + i++.ToString() + ": počet iterací musí být číslo od 1 do 10000!<br>";
            if (algo.Count == 0) // jsou algoritmy na serveru dostupné?
            {
                ViewBag.Error += "Chyba" + i++.ToString() + ": algoritmy pro simulaci nejsou dostupné! (Pravděpodobně došlo ke změně back-endu aplikace.)<br>";
                ViewBag.alg_n = 0;
            }
            if (algo.Count > 0 && algos == null)
                ViewBag.Error += "Chyba" + i++.ToString() + ": musíte vybrat alespoň jeden algoritmus!<br>";

            // rozhodnutí o tom, zda se bude provádět simulace nebo se zpět zobrazí formulář (s chybou)
            if (ViewBag.Error != null)
            {
                SimulationViewModel SVM = new SimulationViewModel(mySenates, NumCases, algo, NumIters);
                ViewBag.senates = HtmlSenates();
                return View(SVM); // byla chyba ve formuláři nebo někdo smazal senáty nebo na serveru nejsou algoritmy
            }
            else
            {
                for (int k = 0; k < algos.Length; k++)
                {
                    // if (algos[k] == k)
                    algInfo.Add(algo[(int)algos[k]]); // do simulace půjdou jen vybrané algoritmy
                }
                foreach (WebSenateModels senat in mySenates)
                {
                    senatesToSim.Add(new Senate(senat.SenateName, true, senat.Load, senat.Acases)); // senáty do simulace (jiná třída na back-endu)
                }
                TempData["Senates"] = senatesToSim;
                TempData["NumCases"] = NumCases;
                TempData["NumIters"] = NumIters;
                TempData["AlgInfo"] = algInfo;
                return RedirectToAction("Run"); // přesměrování na výsledky
            }
        }

        /// <summary>
        /// <c>Run</c> (přesměrováno; GET) - "webová" metoda pro zobrazení výsledků simulace (URL: /Simulation/Run, ale když nemá TempData, tak přesměruje na /Simulation/Index).
        /// </summary>
        /// <remarks><para>ViewModel: SimulationResViewModels.cs, třída SimulationResViewModel.</para><para>Views: Simulation/Run.cshtml.</para><para>Pouze pro přihlášené uživatele.</para><para>Tuto metodu lze spustit jedině po vyplnění formuláře s parametry simulace (nemá-li TempData, tak uživatele přesměruje na daný formulář.</para><para>Generuje také data pro XLS-soubor s výsledky simulace.</para><para>Lze volat jen jednou. Pro opakované volání je nutno znovu vyplnit formulář s parametry simulace! (Zajištěno přesměrováním.)</para></remarks>
        //
        // Redirect: Simulation/Run
        // https://stackoverflow.com/questions/129335/how-do-you-redirect-to-a-page-using-the-post-verb
        // https://stackoverflow.com/questions/26751087/detect-if-page-was-redirected-from-redirecttoaction-method
        public ActionResult Run()
        {
            if (TempData.Count == 0) // nemáme data z Index()
            {
                return RedirectToAction("Index"); // přesměrování
            }
            // jsou data => připravíme HTML pro výpis senátů, a pak spustíme simulaci a zpracujeme výsledky:

            SimulationParams par = new SimulationParams(); // objekt pro back-end (simulační parametry, musí být serveru správně předány)

            List<AlgorithmInfo> algInfo = TempData["AlgInfo"] as List<AlgorithmInfo>;
            List<Senate> senates = TempData["Senates"] as List<Senate>;
            int nCases = (int)TempData["NumCases"];
            int nIters = (int)TempData["NumIters"];
            string resInitBE;

            GetSenates(); // nastaví "mySenates" a "htmlSenates", prioritně ze session
            ViewBag.senates = HtmlSenates(false, senates);

            Initialize();
            resInitBE = InitializeBackEnd(); // připojení k serveru s back-endem, získání seznamu algoritmů
            if (resInitBE != null)
            {
                TempData.Add("error", resInitBE); // chybové hlášení pro přesměrování
                return RedirectToAction("Error", "Simulation");
            }

            par.User = tester;// nastav si uzivatele
            par.Senates = senates; // seznam senatu pro simulaci
            par.AlgorithmsToSimulate = (from a in algInfo select int.Parse(a.AlgorithmID)).ToList(); // IDecka algoritmu
            par.IterationsCount = nIters; // nastaveni poctu iteraci
            par.CasesToDistribution = nCases; // pocet rozdelovanych pripadu v kazde iteraci
            SimulationResViewModel SRVM = new SimulationResViewModel(); //

            try
            {
                var results = proxy_simulation.DoSimulation(par); // proveď simulaci (běží na serveru)
                var report = new SimulationReport(algo, par, results);
                SRVM.Senates.AddRange(from p in par.Senates select p.ID); // jména senátů

                foreach (var a_result in results) // prochazej vysledky po algoritmech
                {
                    SRVM.AlgId.Add(a_result.UsedAlgorithm);// pridej identifikator pouziteho algoritmu, coz je int
                    SRVM.AlgName.Add(ConvertAlgorithmName(algo[a_result.UsedAlgorithm].AlgorithmName));
                    SRVM.Data.Add(a_result.Data);// pridej data ze simulace konkretniho algoritmu, tedy predavam List<List<int>> - vyznam indexu prvni pres iterace, druhy  pres senaty
                    SRVM.MinMaxDiff.Add(a_result.MaxDifference);  // vypocet "max. okamžité odchylky" v rámci každé iterace; tohle dělá back-end (od 28. 3. 2018)
                                 
                    // vypocet a pridani statistickych informaci:

                    /* Do MAX je pridavan seznam maximalnich hodnot pres iterace, tj. pro kazdy senat je zde jedna polozka v seznamu.
                       Ostatni statisticke veliciny analogicky...
                     */
                    SRVM.Max.Add(GetMax(a_result.Data));// spocti maximum a pridej to do seznamu
                    SRVM.Min.Add(GetMin(a_result.Data));// spocti minimum a pridej to do seznamu
                    List<double> avglist;
                    SRVM.Avg.Add(avglist = GetAvg(a_result.Data));// spocti prumery a pridej do seznamu
                    SRVM.Stdev.Add(GetStdev(a_result.Data, avglist));// spocti odchylky a pridej do seznamu. Pro odchylky potrebuji prumery, proto ten pomocny seznam
                }
                //            SRVM.TestovaciVypis();

                // report.CreateReportToFile("report.xlsx"); // zapise do souboru na serveru (pro Plastiaka)
                var memory = report.CreateReportOnTheFly(); // vytvor v pameti. Poslání response: http://howtodomssqlcsharpexcelaccess.blogspot.cz/2014/05/aspnet-how-to-create-excel-file-in.html

                if (!TempData.ContainsKey("BytesData"))
                    TempData.Add("BytesData", memory.ToArray()); // prevod na pole Bytu

                ViewBag.isEquiv = Session["equiv"];
                return View(SRVM);
            }
            catch (Exception e)
            {
                TempData.Add("error", e.Message);
                return RedirectToAction("Error", "Simulation");
            }
            finally
            {
                proxy_simulation.Close(); // odpojení od back-endu
            }
        }

        /// <summary>
        /// <c>Error</c> (GET) - "webová" metoda pro zobrazení chyb simulace (je na ni přesměrováno z Run(), pokud nastane při spuštění simulace chyba).
        /// </summary>
        /// <remarks><para>ViewModel: žádný.</para><para>Views: Simulation/Error.cshtml.</para><para>Pouze pro přihlášené uživatele.</para><para>Tuto metodu lze spustit jedině po vyplnění formuláře s parametry simulace (nemá-li TempData, tak uživatele přesměruje na daný formulář.</para><para>Generuje také data pro XLS-soubor s výsledky simulace.</para><para>Lze volat jen jednou. Pro opakované volání je nutno znovu vyplnit formulář s parametry simulace! (Zajištěno přesměrováním.)</para></remarks>
        //
        // Redirect: Simulation/Error
        [HttpGet]
        public ActionResult Error()
        {
            ViewBag.Error = TempData["error"];
            return View();
        }

        /// <summary>
        /// <c>Download</c> - "webová" metoda pro odeslání XLS souboru do prohlížeče (XLS obsahuje výsledky simulace).
        /// </summary>
        /// <remarks><para>ViewModel: žádný.</para><para>Views: žádné, zůstává otevřená stránka /Simulation/Run, ale tlačítko pro Download se zakáže.</para><para>Pouze pro přihlášené uživatele.</para><para>Odešle data XLS-souboru (s výsledky simulace) do prohlížeče.</para><para>Lze volat jen jednou, pak se tlačítko "zneaktivní" (a uživatel musí vyplnit nový formulář pro simulaci).</para></remarks>
        //
        // POST: Simulation/Download
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Download()
        {
            return File(TempData["BytesData"] as Byte[], System.Net.Mime.MediaTypeNames.Application.Octet, "report" + DateTime.Now.ToLongTimeString() + ".xlsx");
        }

#endregion

#region StatisticHelpers
        /************ METODY PRO VYPOCET STATISTICKYCH INFORMACI PRO KONTROLER SimulationControler *******************************/

        /// <summary>
        /// Vraci maximum pro kazdy senat pres vsechni iterace. Pozor v ramci jednoho algoritmu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private List<int> GetMax(List<List<int>> data)
        {
            int senatescount = data.First().Count();//vezmi delku prvniho pole, vsechna pole maji stejnou delku
            List<int> maxlist = new List<int>();//pro kazdy senat
            for (int i = 0; i < senatescount; i++)//pres vsechny senaty
            {
                var senatedata = from x in data select x[i];//obsahuje vzdy iterator-neco jako seznam-dat pro i-ty senat
                maxlist.Add(senatedata.Max());//nalezni a pridej maximum do seznamu
            }
            return maxlist;
        }

        private List<int> GetMin(List<List<int>> data)
        {
            int senatescount = data.First().Count();//vem delku prvniho pole, vsechny pole maji stejnou delku
            List<int> minlist = new List<int>();//pro kazdy senat
            for (int i = 0; i < senatescount; i++)//pres vsechny senaty
            {
                var senatedata = from x in data select x[i];//obsahuje vzdy iterator-neco jako seznam-dat pro i ty senat
                minlist.Add(senatedata.Min());//nalezni a pridej minimum do seznamu
            }
            return minlist;
        }


        private List<double> GetAvg(List<List<int>> data)
        {
            int senatescount = data.First().Count();//vem delku prvniho pole, vsechny pole maji stejnou delku
            List<double> avglist = new List<double>();//pro kazdy senat
            for (int i = 0; i < senatescount; i++)//pres vsechny senaty
            {
                var senatedata = from x in data select x[i];//obsahuje vzdy iterator-neco jako seznam-dat pro i ty senat
                avglist.Add(senatedata.Average());//nalezni a pridej prumer do seznamu
            }
            return avglist;
        }

        private List<double> GetStdev(List<List<int>> data, List<double> avglist)
        {
            int senatescount = data.First().Count();//vem delku prvniho pole, vsechny pole maji stejnou delku
            List<double> stdevlist = new List<double>();
            for (int i = 0; i < senatescount; i++)//pres vsechny senaty
            {
                var squared = from y in (from x in data select x[i]) select Math.Pow(y - avglist[i], 2);//iterator pres druhou mocninu 
                if (squared.Count() > 1)
                    stdevlist.Add(Math.Sqrt(squared.Sum() / (squared.Count() - 1)));
                else
                    stdevlist.Add(0.0);

            }
            return stdevlist;
        }

        #endregion


        /// <summary>
        /// <c>ConvertAlgorithmName</c> - pomocná metoda pro převod dvojteček a mezer v názvech algoritmů z back-endu na podtržítka.
        /// </summary>
        private string ConvertAlgorithmName(string Text)
        {
            return Text.Replace(":", "_").Replace(" ", "_");
        }


        /// <summary>
        /// <c>Dispose</c> - metoda převzatá z ASP.NET MVC.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
