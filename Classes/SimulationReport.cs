using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//ABY FUNGOVALY nasledujici usings, je nutne pridat pres nuget  DocumentFormat.OpenXml
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using X14 = DocumentFormat.OpenXml.Office2010.Excel;
using X15 = DocumentFormat.OpenXml.Office2013.Excel;
using System.Globalization;
using System.IO;
//FRONTEND
namespace MSPGeneratorWeb.HostedSimulationService //stejny jmenny prostor jako je service
{
    /// <summary>
    /// Třída pro vytváření reportů v xlsx
    /// </summary>
    /// <remarks>
    /// Je použito OpenXml
    /// </remarks>
    class SimulationReport
    {
        SimulationParams sparams;
        List<SimulationResult> sresult;
        List<AlgorithmInfo> alginfo;

        /// <summary>
        /// Odstraneni dvojtecek a mezer z jmena algoritmu
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        /// <remarks>
        /// Excel nechce delat sheety s dvojteckou ve jmene
        /// </remarks>
        private string ConvertAlgorithmName(string Text)
        {
            return Text.Replace(":", "_").Replace(" ", "_");
        }

        /// <summary>
        /// Nastaveni parametru sloupcu
        /// </summary>
        /// <param name="StartColumnIndex"></param>
        /// <param name="EndColumnIndex"></param>
        /// <param name="ColumnWidth"></param>
        /// <returns></returns>
        private Column CreateColumnData(UInt32 StartColumnIndex, UInt32 EndColumnIndex, double ColumnWidth)
        {
            Column column;
            column = new Column();
            column.Min = StartColumnIndex;
            column.Max = EndColumnIndex;
            column.Width = ColumnWidth;
            column.CustomWidth = true;
            return column;
        }

        /// <summary>
        /// Metoda pro převod indexu na Excelovské jméno, tj. 1->A, 2->B
        /// </summary>
        /// <param name="columnNumber"></param>
        /// <returns></returns>
        private string GetExcelColumnName(uint columnNumber)
        {
            uint dividend = columnNumber;
            string columnName = String.Empty;
            uint modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;//predpoklad A-Z 26 znaku A=65
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (uint)((dividend - modulo) / 26);
            }

            return columnName;
        }

        /// <summary>
        /// Konstruktor pro prejmuti parametru simulace
        /// </summary>
        /// <param name="ainfo"></param>
        /// <param name="spar"></param>
        /// <param name="sres"></param>
        public SimulationReport(List<AlgorithmInfo> ainfo, SimulationParams spar, List<SimulationResult> sres)
        {
            sparams = spar;
            sresult = sres;
            alginfo = ainfo;

        }

        /// <summary>
        /// Metoda vytváří report do souboru
        /// </summary>
        /// <param name="file">Jméno souboru</param>
        public void CreateReportToFile(string file)
        {
            using (var fs = File.Create(file))
            {
                Create(fs);
            }
        }

        /// <summary>
        /// Metoda vytvoří report do paměti.
        /// </summary>
        /// <returns>Memory stream s reportem</returns>
        public MemoryStream CreateReportOnTheFly()
        {
            MemoryStream ms = new MemoryStream();
            Create(ms);
            return ms;
        }

        /// <summary>
        /// Vytvoří repost do vstupního streamu
        /// </summary>
        /// <param name="file"></param>
        protected void Create(Stream file)
        {
            using (SpreadsheetDocument spreadSheet = SpreadsheetDocument.Create(file, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookpart = spreadSheet.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();
                WorkbookStylesPart wbsp = workbookpart.AddNewPart<WorkbookStylesPart>();
                wbsp.Stylesheet = GenerateStyleSheet();

                Sheets sheets = spreadSheet.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
                uint id = 1;
                foreach (var result in sresult)//pro kazdy algoritmus delej sheet
                {
                    WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                    Worksheet worksheet = new Worksheet();
                    Columns columns = new Columns();
                    for (uint i = 1; i <= sparams.Senates.Count + 2; i++)
                    {
                        columns.Append(CreateColumnData(i, i, 20));
                    }
                    worksheet.Append(columns);
                    //KUB 6.3. pridano
                    var alg_name = ConvertAlgorithmName((from a in alginfo where a.AlgorithmID == result.UsedAlgorithm.ToString() select a.AlgorithmName).First());
             
                    Sheet sheet = new Sheet()
                    {
                        Id = spreadSheet.WorkbookPart.GetIdOfPart(worksheetPart),
                        SheetId = id,
                        Name = alg_name /*ConvertAlgorithmName(alginfo[result.UsedAlgorithm].AlgorithmName)// chyba 6.3. KUB */
                    };
                    id++;
                    sheets.Append(sheet);

                    SheetData sheetData = new SheetData();

                    SimulationParamsExport(alginfo, sparams, result, sheetData);//tiskni informace o simulaci
                    uint last_row_index = SenateInfoExport(sparams, sheetData, 5);//zacni od pateho radku s vypisem info o senatech
                    last_row_index++;
                    last_row_index = SimulationResultsExport(sparams, result, sheetData, last_row_index);//tiskni vysledky simulace
                    last_row_index++;
                    last_row_index = MaximumExport(result, sheetData, last_row_index);//tiskni maximum    
                    last_row_index++;
                    last_row_index = MinimumExport(result, sheetData, last_row_index);//tiskni minimum
                    last_row_index++;
                    last_row_index = AvgExport(result, sheetData, last_row_index);//tiskni prumer
                    last_row_index++;
                    last_row_index = StdevExport(result, sheetData, last_row_index);//tiskni smerodatnou odchylku
                    worksheet.Append(sheetData);
                    worksheetPart.Worksheet = worksheet;
                }
                workbookpart.Workbook.Save();
            }

        }

        /* Ostatní metody jsou pouze metody pro generování výsledků do souboru.
         * Každá z metod zapisuje příslušné části reportu.
         * Zápis se  provádí na úrovni buněk a řádků
         * 
         */
        private uint StdevExport(SimulationResult result, SheetData sheetData, uint last_row_index)
        {

            Row avg_senate_row = new Row() { RowIndex = last_row_index, Spans = new ListValue<StringValue>() };
            Cell title_cell = new Cell()
            {
                CellReference = "A" + last_row_index,
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)1U,
                CellValue = new CellValue("STDEV")
            };

            avg_senate_row.Append(title_cell);
            uint senate_column_index = 2;
            foreach (var item in GetStdev(result.Data))
            {
                string cellname = GetExcelColumnName(senate_column_index);
                var data_cell = new Cell()
                {
                    CellReference = cellname + last_row_index, //zacne od B
                    DataType = CellValues.Number,
                    StyleIndex = (UInt32Value)7U,//dve des mista-styl7
                    CellValue = new CellValue(Convert.ToString(item, CultureInfo.InvariantCulture))
                };
                avg_senate_row.Append(data_cell);
                senate_column_index++;
            }
            sheetData.Append(avg_senate_row);
            return last_row_index;
        }
        private uint AvgExport(SimulationResult result, SheetData sheetData, uint last_row_index)
        {

            Row avg_senate_row = new Row() { RowIndex = last_row_index, Spans = new ListValue<StringValue>() };
            Cell title_cell = new Cell()
            {
                CellReference = "A" + last_row_index,
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)1U,
                CellValue = new CellValue("PRŮMĚR")
            };
            avg_senate_row.Append(title_cell);
            uint senate_column_index = 2;
            foreach (var item in GetAvg(result.Data))
            {
                string cellname = GetExcelColumnName(senate_column_index);
                var data_cell = new Cell()
                {
                    CellReference = cellname + last_row_index, //zacne od B
                    DataType = CellValues.Number,
                    StyleIndex = (UInt32Value)7U,
                    CellValue = new CellValue(Convert.ToString(item, CultureInfo.InvariantCulture))
                };
                avg_senate_row.Append(data_cell);
                senate_column_index++;
            }
            sheetData.Append(avg_senate_row);
            return last_row_index;
        }
        private uint MinimumExport(SimulationResult result, SheetData sheetData, uint last_row_index)
        {

            Row min_senate_row = new Row() { RowIndex = last_row_index, Spans = new ListValue<StringValue>() };
            Cell title_cell = new Cell()
            {
                CellReference = "A" + last_row_index,
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)1U,
                CellValue = new CellValue("MINIMUM")
            };
            min_senate_row.Append(title_cell);
            uint senate_column_index = 2;
            foreach (var item in GetMinimum(result.Data))
            {
                string cellname = GetExcelColumnName(senate_column_index);
                var data_cell = new Cell()
                {
                    CellReference = cellname + last_row_index, //zacne od B
                    DataType = CellValues.Number,
                    StyleIndex = (UInt32Value)5U,
                    CellValue = new CellValue(Convert.ToString(item))
                };
                min_senate_row.Append(data_cell);
                senate_column_index++;
            }
            sheetData.Append(min_senate_row);
            return last_row_index;
        }
        private uint MaximumExport(SimulationResult result, SheetData sheetData, uint last_row_index)
        {

            Row max_senate_row = new Row() { RowIndex = last_row_index, Spans = new ListValue<StringValue>() };
            Cell title_cell = new Cell()
            {
                CellReference = "A" + last_row_index,
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)1U,
                CellValue = new CellValue("MAXIMUM")
            };
            max_senate_row.Append(title_cell);
            uint senate_column_index = 2;
            foreach (var item in GetMaximum(result.Data))
            {
                string cellname = GetExcelColumnName(senate_column_index);
                var data_cell = new Cell()
                {
                    CellReference = cellname + last_row_index, //zacne od B
                    DataType = CellValues.Number,
                    StyleIndex = (UInt32Value)5U,
                    CellValue = new CellValue(Convert.ToString(item))
                };
                max_senate_row.Append(data_cell);
                senate_column_index++;
            }
            sheetData.Append(max_senate_row);
            return last_row_index;
        }

        private uint SimulationResultsExport(SimulationParams spar, SimulationResult result, SheetData sheetData, uint last_row_index)
        {
            Row title_senate_row = new Row() { RowIndex = last_row_index, Spans = new ListValue<StringValue>() };

            Cell title_cell = new Cell()
            {
                CellReference = "A" + last_row_index,
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)4U,
                CellValue = new CellValue("Iterace")
            };
            title_senate_row.Append(title_cell);
            uint senate_column_index = 2;
            foreach (var s in spar.Senates)
            {
                string cellname = GetExcelColumnName(senate_column_index);
                title_cell = new Cell()
                {
                    CellReference = cellname + last_row_index, //zacne od B
                    DataType = CellValues.String,
                    StyleIndex = (UInt32Value)4U,
                    CellValue = new CellValue(s.ID)
                };

                senate_column_index++;
                title_senate_row.Append(title_cell);
            }
            sheetData.Append(title_senate_row);
            last_row_index++;//posun se na dalsi radek
            int iter_counter = 1;
            foreach (var iter_data in result.Data)//pro kazdou iteraci
            {
                Row data_senate_row = new Row() { RowIndex = last_row_index, Spans = new ListValue<StringValue>() };

                Cell data_cell = new Cell()
                {
                    CellReference = "A" + last_row_index,
                    DataType = CellValues.Number,
                    StyleIndex = (UInt32Value)6U,
                    CellValue = new CellValue(Convert.ToString(iter_counter++))
                };
                data_senate_row.Append(data_cell);
                senate_column_index = 2;

                foreach (var item in iter_data)
                {
                    string cellname = GetExcelColumnName(senate_column_index);
                    data_cell = new Cell()
                    {
                        CellReference = cellname + last_row_index, //zacne od B
                        DataType = CellValues.Number,
                        StyleIndex = (UInt32Value)6U,
                        CellValue = new CellValue(Convert.ToString(item))
                    };

                    senate_column_index++;
                    data_senate_row.Append(data_cell);
                }

                sheetData.Append(data_senate_row);
                last_row_index++;
            }

            return last_row_index;
        }

        private uint SenateInfoExport(SimulationParams spar, SheetData sheetData, uint last_row_index)
        {

            uint index_senate_row = last_row_index;
            Row title_senate_row = new Row() { RowIndex = index_senate_row, Spans = new ListValue<StringValue>() };
            Cell title_cell = new Cell()
            {
                CellReference = "A" + index_senate_row.ToString(),
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)4U,
                CellValue = new CellValue("Senát ID")
            };
            title_senate_row.Append(title_cell);
            title_cell = new Cell()
            {
                CellReference = "B" + index_senate_row.ToString(),
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)4U,
                CellValue = new CellValue("Zatížení v %")
            };
            title_senate_row.Append(title_cell);

            title_cell = new Cell()
            {
                CellReference = "C" + index_senate_row.ToString(),
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)4U,
                CellValue = new CellValue("Aktivní Př.")
            };
            title_senate_row.Append(title_cell);

            title_cell = new Cell()
            {
                CellReference = "D" + index_senate_row.ToString(),
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)4U,
                CellValue = new CellValue("Povolen")
            };
            title_senate_row.Append(title_cell);
            sheetData.Append(title_senate_row);
            index_senate_row++;
            foreach (var senate in spar.Senates)
            {
                Row senate_row = new Row() { RowIndex = index_senate_row, Spans = new ListValue<StringValue>() };
                Cell cell = new Cell()
                {
                    CellReference = "A" + index_senate_row.ToString(),
                    DataType = CellValues.String,
                    StyleIndex = (UInt32Value)6U,
                    CellValue = new CellValue(senate.ID)
                };
                senate_row.Append(cell);

                cell = new Cell()
                {
                    CellReference = "B" + index_senate_row.ToString(),
                    DataType = CellValues.Number,
                    StyleIndex = (UInt32Value)6U,
                    CellValue = new CellValue(Convert.ToString(senate.Load))
                };
                senate_row.Append(cell);

                cell = new Cell()
                {
                    CellReference = "C" + index_senate_row.ToString(),
                    DataType = CellValues.Number,
                    StyleIndex = (UInt32Value)6U,
                    CellValue = new CellValue(Convert.ToString(senate.ActiveCases))
                };
                senate_row.Append(cell);

                cell = new Cell()
                {
                    CellReference = "D" + index_senate_row.ToString(),
                    DataType = CellValues.String,
                    StyleIndex = (UInt32Value)6U,
                    CellValue = new CellValue(((senate.Enabled) ? "ANO" : "NE"))
                };
                senate_row.Append(cell);

                sheetData.Append(senate_row);
                index_senate_row++;
            }
            return index_senate_row;


        }

        private void SimulationParamsExport(List<AlgorithmInfo> ainfo, SimulationParams spar, SimulationResult result, SheetData sheetData)
        {

            Row title_row = new Row() { RowIndex = 1U, Spans = new ListValue<StringValue>() };
            //KUB 6.3. pridano
            var alg_name = ConvertAlgorithmName((from a in alginfo where a.AlgorithmID == result.UsedAlgorithm.ToString() select a.AlgorithmName).First());
      
            Cell cell = new Cell()
            {
                CellReference = "A1",
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)1U,
                CellValue = new CellValue(alg_name/*info[result.UsedAlgorithm].AlgorithmName.Replace(":", " ") // KUB 6.3. vyjmuto a opraveno*/)
            };
            title_row.Append(cell);

            cell = new Cell()
            {
                CellReference = "C1",
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)5U,
                CellValue = new CellValue("Uživatel:" + spar.User.ID)
            };
            title_row.Append(cell);

            cell = new Cell()
            {
                CellReference = "D1",
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)5U,
                CellValue = new CellValue(DateTime.Now.ToString())
            };
            title_row.Append(cell);


            sheetData.Append(title_row);

            Row param_row = new Row() { RowIndex = 3U, Spans = new ListValue<StringValue>() };
            cell = new Cell()
            {
                CellReference = "A3",
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)1U,
                CellValue = new CellValue("Parametry simulace:")
            };
            param_row.Append(cell);
            cell = new Cell()
            {
                CellReference = "C3",
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)5U,
                CellValue = new CellValue("Počet senátů: " + spar.Senates.Count)
            };
            param_row.Append(cell);

            cell = new Cell()
            {
                CellReference = "E3",
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)5U,
                CellValue = new CellValue("Počet případů: " + spar.CasesToDistribution)
            };
            param_row.Append(cell);

            cell = new Cell()
            {
                CellReference = "G3",
                DataType = CellValues.String,
                StyleIndex = (UInt32Value)5U,
                CellValue = new CellValue("Počet iterací: " + spar.IterationsCount)
            };
            param_row.Append(cell);

            sheetData.Append(param_row);

        }


        /*
         * Iterátory pro práci s daty.
         * Jsou použity v při reportování výše
         */
        IEnumerable<double> GetStdev(List<List<int>> data)//kazde pole v seznamu predstavuje distribuci pripadu
        {
            int senatescount = data.First().Count();//vem delku prvniho pole, vsechny pole maji stejnou delku
            var avglist = GetAvg(data).ToList<double>();
            for (int i = 0; i < senatescount; i++)//pres vsechny senaty
            {
                var squared = from y in (from x in data select x[i]) select Math.Pow(y - avglist[i], 2);//iterator pres druhou mocninu 
                if (squared.Count() > 1)
                    yield return Math.Sqrt(squared.Sum() / (squared.Count() - 1));
                else
                    yield return 0.0;

            }
        }

        IEnumerable<double> GetAvg(List<List<int>> data)//kazde pole v seznamu predstavuje distribuci pripadu
        {
            int senatescount = data.First().Count();//vem delku prvniho pole, vsechny pole maji stejnou delku
            return (from iter in (from i in Enumerable.Range(0, senatescount) select from x in data select x[i]) select iter.Average());//LINQ vyraz pro vytvoreni seznamu pres prumery
        }

        IEnumerable<double> GetMaximum(List<List<int>> data)//kazde pole v seznamu predstavuje distribuci pripadu
        {
            int senatescount = data.First().Count();//vem delku prvniho pole, vsechny pole maji stejnou delku

            for (int i = 0; i < senatescount; i++)//pres vsechny senaty
            {
                var senatedata = from x in data select x[i];//obsahuje vzdy iterator-neco jako seznam-dat pro i ty senat
                yield return senatedata.Max();//nalezni  maximum nad danym iteratorem
            }
        }

        IEnumerable<double> GetMinimum(List<List<int>> data)//kazde pole v seznamu predstavuje distribuci pripadu
        {
            int senatescount = data.First().Count();//vem delku prvniho pole, vsechny pole maji stejnou delku

            for (int i = 0; i < senatescount; i++)//pres vsechny senaty
            {
                var senatedata = from x in data select x[i];//obsahuje vzdy iterator-neco jako seznam-dat pro i ty senat
                yield return senatedata.Min();//nalezni  minimum nad danym iteratorem
            }
        }

        /// <summary>
        /// Nastaveni stylu
        /// </summary>
        /// <returns>Styly</returns>
        private Stylesheet GenerateStyleSheet()
        {
            return new Stylesheet(
                new Fonts(
                    new Font(                                                               // Index 0 - The default font.
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }
                        ),
                    new Font(                                                               // Index 1 - The bold font.
                        new Bold(),
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 2 - The Italic font.
                        new Italic(),
                        new FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 2 - The Times Roman font. with 16 size
                        new FontSize() { Val = 16 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Times New Roman" })
                ),
                new Fills(
                    new Fill(                                                           // Index 0 - The default fill.
                        new PatternFill() { PatternType = PatternValues.None }),
                    new Fill(                                                           // Index 1 - The default fill of gray 125 (required)
                        new PatternFill() { PatternType = PatternValues.Gray125 }),
                    new Fill(                                                           // Index 2 - The yellow fill.
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "118d91" } }
                        ) { PatternType = PatternValues.Solid })
                ),
                new Borders(
                    new Border(                                                         // Index 0 - The default border.
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),
                    new Border(                                                         // Index 1 - Applies a Left, Right, Top, Bottom border to a cell
                        new LeftBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new RightBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                ),
                new CellFormats(
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 0 },                          // Index 0 - The default cell style.  If a cell does not have a style index applied it will use this style combination instead
                    new CellFormat(new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                        ) { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true, ApplyAlignment = true },       // Index 1 - Bold 
                    new CellFormat() { FontId = 2, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 2 - Italic
                    new CellFormat() { FontId = 3, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 3 - Times Roman
                    new CellFormat(new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                        ) { FontId = 1, FillId = 2, BorderId = 0, ApplyFill = true, ApplyAlignment = true },       // Index 4 -  Fill-ramecek tabulky
                    new CellFormat(                                                                   // Index 5 - Alignment
                        new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    ) { FontId = 0, FillId = 0, BorderId = 0, ApplyAlignment = true },
                    new CellFormat(new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }) { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true, ApplyAlignment = true },     // Index 6 - Border
                    new CellFormat(                                                                   // Index 7 - Alignment+2des mista
                        new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    ) { NumberFormatId = (UInt32Value)2U, FontId = 0, FillId = 0, BorderId = 0, ApplyAlignment = true }
                )
            ); // return
        }
    }
}
