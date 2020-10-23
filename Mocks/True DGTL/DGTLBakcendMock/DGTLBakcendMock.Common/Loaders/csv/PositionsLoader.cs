using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Temp.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.Loaders.csv
{
    public class PositionsLoader
    {
        #region Privvate Static Constants

        protected static string _ROW_FIRM = " ->";

        protected static string _EMPTY_CELL = "-";

        #endregion

        #region Public Static Methods

        public static PositionsCSVDTO GetPositions(string positionsCSV)
        {
            Dictionary<string, List<ClientPosition>> firmPositions = new Dictionary<string, List<ClientPosition>>();
            Dictionary<string, double> todayDSPs = new Dictionary<string, double>();
            Dictionary<string, double> prevDSPs = new Dictionary<string, double>();

            bool firmsRow = false;
            string currentFirm = null;

            using (var reader = new StreamReader(positionsCSV))
            {
                List<string> listA = new List<string>();
                int i = 0;
                while (!reader.EndOfStream)
                {

                    var line = reader.ReadLine();
                    var values = line.Split(',');


                    if (i == 0)
                    {
                        i++;
                        continue;
                    }

                    if (!firmsRow)
                    {

                        if (line.StartsWith(_ROW_FIRM))
                        {
                            currentFirm = line.Split(',')[0].Replace(_ROW_FIRM, "").Trim();

                            List<ClientPosition> positions = new List<ClientPosition>();

                            if (!firmPositions.ContainsKey(currentFirm))
                            {
                                firmsRow = true;
                                firmPositions.Add(currentFirm, positions);
                            }
                            else
                                throw new Exception(string.Format("Repetated firm positions secion: {0}", currentFirm));

                        }
                        else
                        {
                            if (currentFirm != null)
                            {
                                string symbol = values[2];
                                //read positions row and add it to the firmPositions dict
                                ClientPosition position = new ClientPosition()
                                {
                                    Symbol = symbol,
                                    Contracts = values[5] != _EMPTY_CELL ? Convert.ToInt32(values[5]) : 0,
                                    MarginFunded = true,
                                    Msg = "ClientPosition",
                                    Price = values[7] != _EMPTY_CELL ? Convert.ToDouble(values[7]) : 0
                                };

                                if (!todayDSPs.ContainsKey(symbol) && values[8] != _EMPTY_CELL)
                                    todayDSPs.Add(symbol, Convert.ToDouble(values[8]));

                                if (!prevDSPs.ContainsKey(symbol) && values[6] != _EMPTY_CELL)
                                    prevDSPs.Add(symbol, Convert.ToDouble(values[6]));

                                firmPositions[currentFirm].Add(position);
                            }
                        }
                    }
                    else
                        firmsRow = false;
                }
            }

            return new PositionsCSVDTO() { FirmPositions = firmPositions, PrevDSPs = prevDSPs, TodayDSPs = todayDSPs };
        }

        #endregion
    }
}
