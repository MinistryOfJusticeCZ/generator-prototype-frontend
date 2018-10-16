using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MSPGeneratorWeb.Models
{
    /// <summary>
    /// <c>UsersViewModel</c> - model (třída) pro akci /Account/Users
    /// </summary>
    public class UsersViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string ActiveUserName { get; set; }
    }

    /// <summary>
    /// <c>LoginViewModel</c> - model (třída) pro akci /Account/Login
    /// </summary>
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Přihlašovací jméno")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Heslo")]
        public string Password { get; set; }

        [Display(Name = "zapamatovat uživatelské jméno?")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// <c>RegisterViewModel</c> - model (třída) pro akci /Account/Register
    /// </summary>
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Přihlašovací jméno")]
        [Remote(action: "ExistsUserName", controller: "Account", HttpMethod = "POST", ErrorMessage = "Toto uživatelské jméno již existuje. Zadejte jiné (např. přidejte číslici).")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "E-mailová adresa")]
        [Remote(action: "ExistsEmail", controller: "Account", HttpMethod = "POST", ErrorMessage = "Tento e-mail je již používaný nějakým uživatelem. Zadejte jiný.")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "{0} musí být nejméně {2} znaků dlouhé.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Heslo")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Heslo znovu")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Hesla se neshodují.")]
        public string ConfirmPassword { get; set; }
    }


    /// <summary>
    /// <c>ResetPasswordViewModel</c> - model (třída) pro akci /Account/ResetPassword
    /// </summary>
    public class ResetPasswordViewModel
    {
        /*
        [Required]
        [EmailAddress]
        [Display(Name = "E-mail")]
        public string Email { get; set; }
        */

        [Required]
        [StringLength(100, ErrorMessage = "{0} musí obsahovat nejméně {2} znaků (z toho jednu číslici).", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nové heslo")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nové heslo znovu")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Hesla se neshodují.")]
        public string ConfirmPassword { get; set; }

        public string UserName { get; set; }
    }


    /// <summary>
    /// <c>DeleteViewModel</c> - model (třída) pro akci /Account/Delete
    /// </summary>
    public class DeleteViewModel
    {
        [Required]
        public string UserName { get; set; }
        //public string UserId { get; set; }
    }
}
