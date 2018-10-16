using System.Collections.Generic;

namespace MSPGeneratorWeb.ViewModel
{

    /// <summary>
    ///  Třída zapouzdřuje výsledky ze simulace pro View.
    /// </summary>
    /// <remarks>
    /// Všechny algoritmy použité při simulaci jsou položkou na seznamu.
    /// </remarks>
    public class SimulationResViewModel
    {
        public List<int> AlgId { get; set; } // seznam identifikátorů algoritmů
        public List<string> AlgName { get; set; } // seznam jmen použitých algoritmů

        public List<string> Senates { get; set; }

        //List<List<int>> jsou vysledky simulace za jeden algortimus
        // vnejsi "index" odpovida algoritmu
        // vnitrni "index" odpovida iteraci daneho algoritmu
        // nejvnitrnejsi odpovida senatu Data[vnejsi][vnitrni[nejvnitrnejsi]
        public List<List<List<int>>> Data { get; set; }
        
        //seznam maxim pro jednotlive algortimy a senaty
        //vnejsi index: algoritmus, vnitrni index: senat
        public List<List<int>>  Max { get; set; }

        public List<List<int>> Min { get; set; }

        public List<List<double>> Avg { get; set; }

        public List<List<double>> Stdev { get; set; }

        // seznam "odchylek" pro jednotlive iterace
        // vnejsi index: algoritmus, vnitrni index: iterace
        public List<List<int>> MinMaxDiff { set; get; }

        /// <summary>
        /// Konstruktor pro alokaci vnitřních seznamů
        /// </summary>
        public SimulationResViewModel()
        {
            AlgId = new List<int>(); // alokuj si seznam pro id algoritmu
            AlgName = new List<string>(); // alokuj si seznam pro jmena algoritmu
            Data = new List<List<List<int>>>(); // alokuj si seznam
            Senates = new List<string>(); // alokuj si seznam pro jména senátů


            Max = new List<List<int>>();
            Min = new List<List<int>>();
            Avg = new List<List<double>>();
            Stdev = new List<List<double>>();

            MinMaxDiff = new List<List<int>>(); // "odchylky" přes jednotlivé iterace každého algoritmu
        }

    }

}
