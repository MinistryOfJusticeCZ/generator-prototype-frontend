using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MSPGeneratorWeb.Models
{
    /// <summary>
    /// Třída pro práci se senáty ve webovém rozhraní.
    /// </summary>
    public class WebSenateModels
    {
        [Key]
        public int Id { get;  set; }
        [AllowHtml]
        [StringLength(100)]
        [Required(ErrorMessage = "{0} je povinný.")]
        [MinLength(5, ErrorMessage = "{0} musí mít alespoň {1} znaků.")]
        [MaxLength(100, ErrorMessage = "{0} nesmí být delší než {1} znaků.")]
        [Display(Name = "Název")]
        public string SenateName { get;  set; }
        [Display(Name = "používat při simulaci")]
        public bool Enabled { get;  set; }
        [Required(ErrorMessage = "{0} je povinný údaj.")]
        [Display(Name = "Zatížení")]
        // [DataAnnotationsExtensions.Integer(ErrorMessage = "Please enter a valid number.")]
        [Range(0, 100, ErrorMessage = "{0} musí mít hodnotu mezi {1} a {2}.")]
        public double Load { get; set; }
        [Required(ErrorMessage = "{0} je povinný údaj.")]
        [Display(Name = "Počet aktuálně řešených případů")]
        // [RegularExpression("[0-9]+", ErrorMessage = "{0} musí obsahovat číslo.")]
        [Range(0, 10000, ErrorMessage = "{0} musí být v rozsahu {1} až {2}.")]
        public int Acases { get;  set; }
        
        public ApplicationUser SenateCreator { get; private set; }

        // metody:
        // WebSenateModels
        public void SenateUserSet(ApplicationUser uzivatel)
        {
            SenateCreator = uzivatel;
        }
            

    }

}