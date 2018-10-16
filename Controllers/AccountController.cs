using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using MSPGeneratorWeb.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MSPGeneratorWeb.Controllers
{
    /// <summary>
    /// <c>AccountController</c> - třída pro správu uživatelských účtů a práci s nimi přes web.
    /// </summary>
    /// <remarks><para>Metody jsou vygenerované z ASP.NET MVC, avšak některé "webové" metody byly upraveny a některé odstraněny.</para><para>Třída má 2 vlastnosti: <c>_signInManager</c> a <c>_userManager</c>.</para></remarks>
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        /// <summary>
        /// <c>AccountController</c> - konstruktor (defaultní).
        /// </summary>
        public AccountController()
        {
        }


        /// <summary>
        /// <c>AccountController</c> - konstruktor (se 2 vstupy) od ASP.NET MVC.
        /// </summary>
        /// <ramarks>Přiřazuje vstupy do vlastností.</ramarks>
        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }


        /// <summary>
        /// <c>SignInManager</c> - metoda pro nastavení/zjištění vlastnosti <c>_signInManager</c>.
        /// </summary>
        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        /// <summary>
        /// <c>UserManager</c> - metoda pro nastavení/zjištění vlastnosti <c>_userManager</c>.
        /// </summary>
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        #region LogIn
        /// <summary>
        /// <c>Login</c> (GET) - "webová" metoda pro vykreslení přihlašovacího formuláře (URL: /Account/Login; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: AccountViewModels.cs, třída LoginViewModel.</para><para>Views: Account/Login.cshtml.</para><para>Pozn.: vstup metody je URL, které chtěl zobrazit nepřihlášený uživatel.</para></remarks>
        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// <c>Login</c> (POST) - "webová" metoda pro zpracování přihlašovacího formuláře (URL: /Account/Login; HTTP POST).
        /// </summary>
        /// <remarks><para>Vstupem jsou data z formuláře pro přihlášení uživatele a URL, kam má být přesměrováno.</para><para>Pokud nastane chyba, znovu vykreslí formulář (model a view jsou stejné jako u GET-varianty této metody).</para></remarks>
        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Chybné přihlašovací údaje.");
                    return View(model);
            }
        }
        #endregion

        #region Register
        /// <summary>
        /// <c>Register</c> (GET) - "webová" metoda pro vykreslení formuláře na vytvoření nového uživatele typu "user" (URL: /Account/Register; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: AccountViewModels.cs, třída RegisterViewModel.</para><para>Views: Account/Register.cshtml + _RegisterPartial.cshtml.</para><para>Pozn.: modifikováno z původního ASP.NET MVC, aby akci mohl provádět jen uživatel s rolí "admin" a aby se pouze vytvářel účet (a nepřihlašuje rovnou toho nového uživatele).</para></remarks>
        //
        // GET: /Account/Register
        [Authorize]
        public ActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// <c>Register</c> (POST) - "webová" metoda pro zpracování formuláře na vytvoření nového uživatele (URL: /Account/Register; HTTP POST).
        /// </summary>
        /// <remarks><para>Vstupem jsou data z formuláře pro vytvoření nového uživatele. Jsou-li správná, vytvoří uživatele a přesměruje na přehled uživatelů. Jsou-li data chybná, tak znovu vykreslí formulář (model a view jsou stejné jako u GET-varianty této metody).</para></remarks>
        //
        // POST: /Account/Register
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                Regex rv = new Regex("[0-9]"); // test, zda heslo obsahuje nějakou číslici
                if (!rv.IsMatch(model.Password))
                {
                    // přidání českého chybového hlášení k položce formuláře (http://www.tutorialsteacher.com/mvc/htmlhelper-validationsummary):
                    ModelState.AddModelError("Password", "Heslo musí obsahovat alespoň jednu číslici.");
                    return View(model); // chyba => zpět na formulář
                }
                var user = new ApplicationUser { UserName = model.UserName, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password); // vytvoří uživatele
                
                if (result.Succeeded)
                {
                    var userId = UserManager.FindByName(model.UserName);
                    UserManager.AddToRole(userId.Id, "user");

                    // nového uživatele nebudeme přihlašovat!
                    // await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

                    TempData.Add("Message", $"Nový uživatel '{model.UserName}' byl vytvořen.");

                    return RedirectToAction("Users", "Account");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }


        /// <summary>
        /// <c>ExistsUserName</c> - ověření existence uživatelského jména.
        /// </summary>
        /// <remarks><para>Pomocná metoda pro kontrolu unikátnosti username na klientovi (při registraci nového uživ. adminem).</para><para>Tuto metodu si "volá formulář" z /Account/Register.</para><para>Používá formát JSON pro výměnu dat mezi HTTP serverem a HTTP klientem.</para></remarks>
        // 
        [HttpPost]
        public JsonResult ExistsUserName(string UserName)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            ApplicationUser user = db.Users.FirstOrDefault(x => x.UserName == UserName); // exists user with given UserName?
            return Json(user == null);
        }

        /// <summary>
        /// <c>ExistsEmail</c> - ověření existence e-mailové adresy.
        /// </summary
        /// <remarks><para>Pomocná metoda pro kontrolu unikátnosti e-mailu na klientovi (při registraci nového uživ. adminem).</para><para> Původně bylo plánováno, že zapomenuté heslo se bude posílat uživateli e-mailem, ale nakonec zapomenuté heslo řeší administrátor jeho přenastavením.</para><para>Tuto metodu si "volá formulář" z /Account/Register.</para><para>Používá formát JSON pro výměnu dat mezi HTTP serverem a HTTP klientem.</para></remarks>
        //
        [HttpPost]
        public JsonResult ExistsEmail(string Email)
        {
            // var user = Membership.FindUsersByEmail(Email);
            ApplicationDbContext db = new ApplicationDbContext();
            ApplicationUser user = db.Users.FirstOrDefault(x => x.Email == Email);
            return Json(user == null);
        }
        #endregion

        #region Users
        /// <summary>
        /// <c>Users</c> (GET) - "webová" metoda pro výpis seznamu uživatelů s odkazy na nějaké akce (URL: /Account/Users; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: AccountViewModels.cs, třída UsersViewModel.</para><para>Views: Account/Users.cshtml + _UsersPartial.cshtml.</para><para>Pozn.: nově přidaná metoda, dostupná pouze uživateli s rolí "admin" (řeší si to View).</para></remarks>
        // GET: /Account/Users
        [Authorize] // (Roles = "admin")]
        public ActionResult Users()
        {
            ApplicationDbContext context = new ApplicationDbContext();
            var usersWithRoles = (from user in context.Users
                                  select new
                                  {
                                      UserId = user.Id,
                                      Username = user.UserName,
                                      Email = user.Email,
                                      RoleNames = (from userRole in user.Roles
                                                   join role in context.Roles on userRole.RoleId
                                                   equals role.Id
                                                   select role.Name).ToList()
                                  }).OrderBy(u => u.Username).ToList().Select(p => new UsersViewModel()
                                  {
                                      UserId = p.UserId,
                                      UserName = p.Username,
                                      Email = p.Email,
                                      Role = string.Join(",", p.RoleNames)
                                  });

            ViewBag.Error = TempData["error"]; // chyby od nastavení hesla někomu (ResetPassword)
            ViewBag.Message = TempData["Message"];
            ViewBag.ActiveUser = User.Identity.Name; // username přihlášeného uživatele
            return View(usersWithRoles);
        }
        #endregion

        #region ResetPassword        
        /// <summary>
        /// <c>ResetPassword</c> (GET) - "webová" metoda pro přenastavení hesla vybraného uživatele (vstupem je username; URL: /Account/ResetPassword; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: AccountViewModels.cs, třída ResetPasswordViewModel.</para><para>Views: Account/ResetPassword.cshtml + _ResetPasswordPartial.cshtml.</para><para>Pozn.: upravená metoda, dostupná pouze uživateli s rolí "admin" (řeší si to View).</para><para>Hesla lze měnit pouze uživatelům s rolí "user" nebo sám sobě (nikoli jiným administrátorům).</para><para>A) Změna hesla uživatele s rolí "user": po úspěšné změně přesměruje na seznam uživatelů (/Account/Users), při chybě znovu zobrazí formulář.</para><para>B) Změna hesla uživatele s rolí "admin", který je právě přihlášen: přesměruje na Manage/ChangePassword.</para><para>C) Změna hesla uživateli s rolí "admin", který není právě přihlášen: zakázána.</para></remarks>
        //
        // GET: /Account/ResetPassword?username=xxx
        [Authorize]
        public ActionResult ResetPassword(string username) // máme username poslané pomocí GET
        {
            if (username == null) // neplatné username!
            {
                TempData.Add("error", "Pokud chcete někomu nastavit nové heslo, tak musíte vybrat existujícího uživatele.");
                return RedirectToAction("Users", "Account");
            }

            ResetPasswordViewModel RPVM = new ResetPasswordViewModel();
            try
            {
                ApplicationUser user = UserManager.FindByName(username);
                if (user.UserName == User.Identity.Name) // jedná se o právě přihlášeného uživatele (admina)
                {
                    return RedirectToAction("ChangePassword", "Manage"); // sám sobě smí heslo jen změnit, nikoli nastavit bez znalosti starého hesla
                } 
                else if (!UserManager.IsInRole(user.Id, "user")) // nejedná se o uživatele v roli "user"!
                {
                    TempData.Add("error", "Heslo lze nastavit pouze uživateli v roli 'user', nikoli jinému administrátorovi.");
                    return RedirectToAction("Users", "Account");
                }

                RPVM.UserName = username;
                return View(RPVM);
            }
            catch // asi bylo neplatné username!
            {
                TempData.Add("error", $"Uživateli '{username}' nelze nastavit nové heslo (uživatel neexistuje).");
                return RedirectToAction("Users", "Account");
            }
        }

        /// <summary>
        /// <c>ResetPassword</c> (POST) - "webová" metoda pro zpracování formuláře na změnu hesla uživatele s rolí "user" (URL: /Account/ResetPassword; HTTP POST).
        /// </summary>
        /// <remarks><para>Vstupem jsou data z formuláře pro změnu hesla.</para><para>Při úspěchu přesměruje na přehled uživatelů (/Account/Users) a při některých chybách taky - např. neexistující username. Při chybně zadaném heslu znovu vykreslí formulář (model a view jsou stejné jako u GET-varianty této metody).</para></remarks>
        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);  // chyba => zpět na formulář
            }
            else
            {   // kontrola formátu hesla - obsahuje číslici?
                Regex rv = new Regex("[0-9]"); // test, zda heslo obsahuje nějakou číslici
                if (!rv.IsMatch(model.Password))
                {
                    // přidání českého chybového hlášení k položce formuláře (http://www.tutorialsteacher.com/mvc/htmlhelper-validationsummary):
                    ModelState.AddModelError("Password", "Heslo musí obsahovat alespoň jednu číslici.");
                    return View(model); // chyba => zpět na formulář
                }
            }
            ApplicationUser user = UserManager.FindByName(model.UserName);
            if (user == null)
            {
                TempData.Add("error", "Chyba: neplatné uživatelské jméno. Najděte řádek uživatele a klikněte na odkaz vpravo.");
                return RedirectToAction("Users", "Account");
            }
            if (!UserManager.IsInRole(user.Id, "user")) // nejedná se o uživatele v roli "user"!
            {
                TempData.Add("error", "Heslo lze nastavit pouze uživateli v roli 'user', nikoli jinému administrátorovi.");
                return RedirectToAction("Users", "Account");
            }

            if (user.PasswordHash != null) // musíme odstranit staré heslo
            {
                UserManager.RemovePassword(user.Id);
            }

            UserManager.AddPassword(user.Id, model.Password); // nastavení nového hesla
            TempData.Add("Message", "Heslo pro uživatele '" + model.UserName + "' bylo úspěšně nastaveno.");
            return RedirectToAction("Users", "Account");
        }
        #endregion

        #region Delete
        /// <summary>
        /// <c>Delete</c> (GET) - "webová" metoda, zobrazuje tlačítko pro potvrzení odstranění vybraného uživatele (vstupem je username; URL: /Account/Delete; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: AccountViewModels.cs, třída DeleteViewModel.</para><para>Views: Account/Delete.cshtml.</para><para>Pozn.: nově přidaná metoda, dostupná pouze uživateli s rolí "admin" (řeší si to View).</para><para>Kontroluje dále: existuje uživatel? Je to uživatel s rolí "user"?</para></remarks>
        //
        // GET: /Account/Delete?username=xxx
        [Authorize]
        public ActionResult Delete(string username) // máme username poslané pomocí GET
        {
            if (username == null) // neplatné username!
            {
                TempData.Add("error", "Před smazáním musíte vybrat existujícího uživatele.");
                return RedirectToAction("Users", "Account");
            }

            DeleteViewModel DVM = new DeleteViewModel();
            try
            {
                ApplicationUser user = UserManager.FindByName(username);
                if (user.UserName == User.Identity.Name) // jedná se o právě přihlášeného uživatele (admina)
                {
                    TempData.Add("error", "Nemůžete smazat svůj uživatelský účet.");
                    return RedirectToAction("Users", "Account"); // sám sebe nesmí mazat
                }
                else if (!UserManager.IsInRole(user.Id, "user")) // nejedná se o uživatele v roli "user"!
                {
                    TempData.Add("error", "Lze mazat pouze uživateli v roli 'user', nikoli jiné administrátory.");
                    return RedirectToAction("Users", "Account");
                }

             //   DVM.UserId = user.Id;
                DVM.UserName = username;
                return View(DVM); // formulář pro potvrzení smazání uživatele
            }
            catch // asi bylo neplatné username!
            {
                TempData.Add("error", "Uživatele '" + username + "' nelze smazat (uživatel neexistuje).");
                return RedirectToAction("Users", "Account");
            }
        }

        /// <summary>
        /// <c>Delete</c> (POST) - "webová" metoda pro odstranění vybraného uživatele z databáze (URL: /Account/Delete; HTTP POST).
        /// </summary>
        /// <remarks><para>Vstupem je username z formuláře pro potvrzení odstranění uživatele.</para><para>Po pokusu o odstranění uživatele přesměruje na přehled uživatelů (/Account/Users). Kdyby nastala chyba ve formuláři (nemělo by nastat), tak znovu vykreslí formulář (s jedním potvrzovacím tlačítkem; model a view jsou stejné jako u GET-varianty této metody).</para></remarks>
        //
        // POST: /Account/Delete  (POST: username)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(DeleteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                ApplicationUser user = UserManager.FindByName(model.UserName);
                UserManager.Delete(user); // odstranění uživatele
                TempData.Add("Message", "Uživatel '" + model.UserName + "' byl smazán.");
                return RedirectToAction("Users", "Account");
            }
            catch
            {
                TempData.Add("error", "Uživatele '" + model.UserName + "' se nepodařilo smazat.");
                return RedirectToAction("Users", "Account");
            }

            /*
            WebSenateModels webSenateModels = db.WebSenateModels.Find(id);
            db.WebSenateModels.Remove(webSenateModels);
            db.SaveChanges();
            return RedirectToAction("Index");
            */
        }

        #endregion


        #region LogOff
        /// <summary>
        /// <c>LogOff</c> (POST) - "webová" metoda pro odhlášení uživatele z aplikace (URL: /Account/LogOff; HTTP POST).
        /// </summary>
        /// <remarks><para>Odhlásí uživatele a přesměruje na přihlašovací formulář.</para><para>Nepoužívá žádný model ani view.</para></remarks>
        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        #endregion


        /// <summary>
        /// <c>Dispose</c> - metoda převzatá z ASP.NET MVC.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }


        #region Helpers
        /// <summary>
        /// <c>XsrfKey</c> - property převzatá z ASP.NET MVC.
        /// </summary>
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        /// <summary>
        /// <c>AuthenticationManager</c> - property převzatá z ASP.NET MVC.
        /// </summary>
        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        /// <summary>
        /// <c>AddErrors</c> - metoda převzatá z ASP.NET MVC.
        /// </summary>
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        /// <summary>
        /// <c>Dispose</c> - metoda převzatá z ASP.NET MVC.
        /// </summary>
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// <c>ChallengeResult</c> - třída převzatá z ASP.NET MVC.
        /// </summary>
        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}