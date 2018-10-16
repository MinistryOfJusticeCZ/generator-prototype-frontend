using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MSPGeneratorWeb.Models;
using MSPGeneratorWeb.HostedSimulationService;
using System.ComponentModel.DataAnnotations;

namespace MSPGeneratorWeb.ViewModel
{
    public class SimulationViewModel
    {
        //[Key]
        //public string User { get; set; }
        public List<WebSenateModels> Senates { get; set; }
        public int Cases { get; set; }
        //[Key(Algos.AlgorithmIDField)]
        public List<AlgorithmInfo> Algos { get; set; }
        public int Iters { get; set; }
        public bool IsEquiv { get; } // pro zjištění ekvivalence senátů ("relevance" výpisu odchylky)


        /// <summary>
        /// Konstruktor (alokace vnitřních seznamů, přiřazení vstupů, nastavení "relevance" vypisované odchylky)
        /// </summary>
        public SimulationViewModel(List<WebSenateModels> senates, int n_cases, List<AlgorithmInfo> algos, int k_iters)
        {
            Senates = new List<WebSenateModels>(); // alokuj si seznam pro senáty
            Algos = new List<AlgorithmInfo>(); // alokuj si seznam pro algoritmy

            // převzetí hodnot ze vstupů:
            Senates = senates;
            Cases = n_cases;
            Algos = algos;
            Iters = k_iters;

            // porovnání senátů a nastavení vlastnosti IsEquiv (mají všechny senáty stejné zatížení i počet případů?)
            IsEquiv = true; // počáteční optimismus
            double load = 0.0;
            int acases = 0;
            for (int i = 0; i < Senates.Count; i++) // konverze dvojteček na mezery v názvech algoritmů
            {
                if (i == 0) // pro první položku inicializujeme hodnoty
                {
                    load = Senates[0].Load;
                    acases = Senates[0].Acases;
                }
                else
                {
                    if (load != Senates[i].Load || acases != Senates[i].Acases)
                    {
                        IsEquiv = false;
                        break; // konec cyklu
                    }
                }

            }
        }
    }
}