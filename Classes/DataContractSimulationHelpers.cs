using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace MSPGeneratorWeb.HostedSimulationService
{
    /*
     * Helpers jsou zde pro jednodušší práci s objekty na straně klienta.
     * Umožňují na klientské straně vytvářet potřebné objekty pomocí konstruktorů.
     * pro každou službu jsou Helpers samostatné, neboť každá služba má svůj vlastní jmený prostor z důvodů oddělitelnosti služeb.
    */

    /// <summary>
    /// Pomocna trida pro kontrolu praci na strane klienta.
    /// </summary>
    
    public partial class UserID  //hack na konstruktor
    {
        public UserID(string id)
        {
            ID = id;
        }
    }

    /// <summary>
    /// Pomocná třída pro práci se senáty na straně klienta. Rozšiřuje původní třídu.
    /// </summary>
    /// <remarks>Třída není nezbytná, nicméně umožňuje provádět některé kontroly již na straně klienta, nikoliv až na serveru.</remarks>
    public partial class Senate
    {
        /// <summary>
        /// Kontruktor umožňující kontrolu údajů na straně klienta.
        /// </summary>
        /// <param name="id">ID senátu</param>
        /// <param name="enabled">Senát povolen/zakázán.</param>
        /// <param name="load">Zatížení senátu.</param>
        /// <param name="acases">Počet případů v řešení.</param>
        public Senate(string id, bool enabled, double load, int acases)
        {
            if (load < 0 || load > 100)
                throw new ArgumentException("Load must be in interval <0,100>.");

            if (acases < 0)
                throw new ArgumentException("Active cases must be in interval <0,infinity>.");
            this.Load = load;
            this.ID = id;
            this.Enabled = enabled;
            this.ActiveCases = acases;
        }
        /// <summary>
        /// Kontruktor umožňující kontrolu údajů na straně klienta. Počet případů v řešení je 0.
        /// </summary>
        /// <param name="id">ID senátu</param>
        /// <param name="enabled">Senát povolen/zakázán.</param>
        /// <param name="load">Zatížení senátu.</param>
        public Senate(string id, bool enabled, double load) : this(id, enabled, load, 0) { } //0 pripadu v reseni
        /// <summary>
        /// Kontruktor umožňující kontrolu údajů na straně klienta. Počet případů v řešení je 0 a zatížení senátu je 100%.
        /// </summary>
        /// <param name="id">ID senátu</param>
        /// <param name="enabled">Senát povolen/zakázán.</param>
        public Senate(string id, bool enabled) : this(id, enabled, 100.0) { }//pouzije se 100% zatizeni, 0 pripadu v reseni
        /// <summary>
        /// Kontruktor umožňující kontrolu údajů na straně klienta. Počet případů v řešení je 0 a zatížení senátu je 100% a senát je povolen
        /// </summary>
        /// <param name="id">ID senátu</param>
        public Senate(string id) : this(id, true, 100.0, 0) { }// pouzije se povoleny senat,100% zatizeni a 0 pripadu v reseni

    }
  
     
}
