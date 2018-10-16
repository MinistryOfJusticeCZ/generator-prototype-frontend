using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using MSPGeneratorWeb.ViewModel;
using System.Web.Hosting;

namespace MSPGeneratorWeb.Controllers
{
    /// <summary>
    /// <c>HomeController</c> - třída pro obecnou práci s webem (titulní stránka, kontakt, changelog).
    /// </summary>
    /// <remarks>Třída má jednu vlastnost: fileChangeLog (cesta a jméno souboru, v němž je uložen ChangeLog).</remarks>
    public class HomeController : Controller
    {
        private string fileChangeLog;

        /// <summary>
        /// <c>HomeController</c> - konstruktor, nastaví cestu k souboru pro ChangeLog (vlastnost "fileChangeLog").
        /// </summary>
        public HomeController()
        {
            // cesta k souboru, kde je ChangeLog (kód v HTML):
            fileChangeLog = HostingEnvironment.MapPath(@"~/App_Data/") + "changelog.dat";
        }


        /// <summary>
        /// <c>Index</c>  (GET) - "webová" metoda pro úvodní stránku webu (URL: /Home nebo /Home/Index; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: žádný.</para><para>Views: Home/Index.cshtml.</para><para>Titulní stránka webu pro přihlášené nebo nepřihlášené uživatele.</para></remarks>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// <c>Contact</c>  (GET) - "webová" metoda pro stránku s kontaktem na administrátora GNP (URL: /Home/Contact; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: žádný.</para><para>Views: Home/Contact.cshtml.</para><para>Stránka s informacemi o administrátorovi GNP (kontakty).</para><para>Přístupná přihlášeným i nepřihlášeným uživatelům.</para></remarks>
        [AllowAnonymous]
        public ActionResult Contact()
        {
            return View();
        }


        /// <summary>
        /// <c>ReadChangeLog</c>  - pomocná metoda pro načtení obsahu souboru, kde je ChangeLog.
        /// </summary>
        /// <returns>Vrací řetězec = kód HTML ze souboru, v němž je ChangeLog. Při chybě je řetězec prázdný.</returns>
        // pomocná metoda, není viditelná z webu
        private string ReadChangeLog()
        {
            string obsah = ""; // příprava dat do editoru HTML

            if (System.IO.File.Exists(fileChangeLog))
            {
                try
                {
                    using (var tr = System.IO.File.OpenText(fileChangeLog))
                    {
                        obsah = tr.ReadToEnd();
                    }
                }
                catch (Exception e)
                {
                    obsah = "!!! Problém na serveru: obsah changelogu nelze načíst (data jsou právě editována nebo nejsou nastavena potřebná práva na serveru) !!!";
                }
            }

            return obsah;
        }

        /// <summary>
        /// <c>ChangeLog</c>  (GET) - "webová" metoda pro zobrazení ChangeLogu (URL: /Home/ChangeLog; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: žádný.</para><para>Views: Home/ChangeLog.cshtml.</para><para>Zobrazí obsah ChangeLogu a pokud je přihlášen administrátor, tak i odkaz na editaci ChangeLogu.</para><para>Přístupná pouze přihlášeným uživatelům.</para></remarks>
        // GET: /Home/ChangeLog
        [Authorize]
        public ActionResult ChangeLog()
        {
            ViewBag.obsahHtml = ReadChangeLog();

            return View(); // všichni vidí přehled novinek, admin tam má tlačítko pro editaci (viz View)
        }

        /// <summary>
        /// <c>ChangeLogEdit</c>  (GET) - "webová" metoda pro zobrazení ChangeLogu (URL: /Home/ChangeLogEdit; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: HomeViewModels.cs, třída ChangelogViewModel.</para><para>Views: Home/ChangeLogEdit.cshtml.</para><para>Zobrazí formulář pro editaci ChangeLogu (používá editor TinyMCE).</para><para>Přístupná pouze přihlášeným uživatelům s rolí "admin" (řeší to View).</para></remarks>
        //
        // GET: /Home/ChangeLogEdit
        [Authorize] //(Roles = "admin")] // roli ohlídá View
        public ActionResult ChangeLogEdit()
        {
            // Pozn.: jen adminové mohou editovat ChangeLog (všichni ten jeden)
            ChangelogViewModel CVM = new ChangelogViewModel();
            CVM.ContentHtml = ReadChangeLog();

            return View(CVM);
        }

        /// <summary>
        /// <c>ChangeLogEdit</c> (POST) - "webová" metoda pro zpracování přihlašovacího formuláře (URL: /Home/ChangeLogEdit; HTTP POST).
        /// </summary>
        /// <remarks><para>Vstupem jsou data z formuláře pro editaci ChangeLogu. Jedná se o jeden řetězec, který se nekontroluje (obsahuje kód HTML) a uloží se do souboru ChangeLogu.</para><para>Dostupná jen uživatelům s rolí "admin".</para></remarks>
        //
        // POST: /Home/ChangeLogEdit
        [HttpPost]
        [Authorize(Roles = "admin")] // pro jistotu
        [ValidateInput(false)] // data z editoru TinyMCE obsahují kód HTML => nebudeme validovat!
        [ValidateAntiForgeryToken]
        public ActionResult ChangeLogEdit(ChangelogViewModel htmlText)
        {
            try
            {
                using (var fd = System.IO.File.CreateText(@fileChangeLog))
                {
                    fd.Write(htmlText.ContentHtml); // uložíme data z TinyMCE do souboru na server
                }
                TempData["code"] = "1";
                TempData["message"] = "Data byla uložena.";
            }
            catch (Exception e)
            {
                TempData["code"] = "2";
                TempData["message"] = "Data se bohužel nepodařilo uložit. Možné příčiny: data edituje jiný administrátor (vyčkejte několik minut) nebo je problém s právy zápisu do souboru (kontaktujte správce serveru).";
            }
            return RedirectToAction("ChangeLog"); // přesměrujeme
        }
    }
}