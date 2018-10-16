using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using MSPGeneratorWeb.Models;
using System.Web.Security;

namespace MSPGeneratorWeb.Controllers
{
    /// <summary>
    /// <c>ManageController</c> - třída pro správu uživatelského účtu.
    /// </summary>
    /// <remarks><para>Metody jsou vygenerované z ASP.NET MVC, avšak některé "webové" metody byly upraveny a některé odstraněny.</para><para>Třída má 2 vlastnosti: <c>_signInManager</c> a <c>_userManager</c>.</para></remarks>
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        /// <summary>
        /// <c>ManageController</c> - konstruktor (defaultní).
        /// </summary>
        public ManageController()
        {
        }

        /// <summary>
        /// <c>ManageController</c> - konstruktor (se 2 vstupy) od ASP.NET MVC.
        /// </summary>
        /// <ramarks>Přiřazuje vstupy do vlastností.</ramarks>
        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
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

        /// <summary>
        /// <c>Index</c> (GET) - "webová" metoda pro zobrazení informací o uživatelském účtu přihlášeného uživatele (URL: /Manage/Index; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: ManageViewModels.cs, třída IndexViewModel (převzato z ASP.NET MVC, ale nejsou používány všechny položky).</para><para>Views: Manage/Index.cshtml.</para><para>Pozn.: přístupné jen přihlášenému uživateli.</para></remarks>
        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Heslo bylo změněno."
                : message == ManageMessageId.SetPasswordSuccess ? "Heslo bylo nastaveno."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Dvoufaktorová autentifikace byla úspěšně nastavena."
                : message == ManageMessageId.Error ? "Nastala chyba."
                : message == ManageMessageId.AddPhoneSuccess ? "Vaše telefonní číslo bylo přidáno."
                : message == ManageMessageId.RemovePhoneSuccess ? "Vaše telefonní číslo bylo smazáno."
                : "";

            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
            };
            // e-mailová adresa:
            // MembershipUser u = Membership.GetUser(User.Identity.Name);
            // ViewBag.Email = u.Email;
            // view:
            return View(model);
        }


        /// <summary>
        /// <c>ChangePassword</c> (GET) - "webová" metoda pro změnu hesla sám sobě (URL: /Manage/ChangePassword; HTTP GET).
        /// </summary>
        /// <remarks><para>Models: ManageViewModels.cs, třída ChangePasswordViewModel.</para><para>Views: Manage/ChangePassword.cshtml.</para><para>Pozn.: přístupné jen přihlášenému uživateli.</para></remarks>
        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// <c>ChangePassword</c> (POST) - "webová" metoda pro zpracování formuláře na změnu hesla (URL: /Manage/ChangePassword; HTTP POST).
        /// </summary>
        /// <remarks><para>Vstupem jsou data z formuláře pro změnu hesla (přihlášeného uživatele).</para><para>Pokud nastane chyba, znovu vykreslí formulář (model a view jsou stejné jako u GET-varianty této metody).</para><para>Po úspěšné změně hesla v databázi přesměruje na informace o uživatelově účtu (/Manage/Index).</para></remarks>
        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }


        /// <summary>
        /// <c>Dispose</c> - metoda převzatá z ASP.NET MVC.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

    
#region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}