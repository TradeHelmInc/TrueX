using DGTLBackendMock.Common.DTO.Temp.Positions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.Loaders.csv
{
    public class ExecutionsLoader
    {
        #region Protected Static Consts

        private static string _BUY_LABEL = "Buy";

        #endregion

        #region Protected Static Method

        protected static DateTime GetDate(string strDate)
        {

            try
            {
                return DateTime.ParseExact(strDate, "MM-dd-yy - hh:mm:ss tt", null);

            }
            catch (Exception)
            {
                return DateTime.ParseExact(strDate, "MM-dd-yy - h:mm:ss tt", null);
            }
        }

        #endregion

        #region Public Static Method

        public static List<TradeDTO> GetTrades(string csvFile)
        {
            List<TradeDTO> trades = new List<TradeDTO>();

            using (var reader = new StreamReader(csvFile))
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


                    TradeDTO trade = new TradeDTO()
                    {
                        Date = GetDate(values[0].Replace("\"", "")),
                        Symbol = values[2].Replace("\"", ""),
                        Side = values[1].Replace("\"", "") == _BUY_LABEL ? TradeDTO._TRADE_BUY : TradeDTO._TRADE_SELL,
                        ExecutionSize = Convert.ToDouble(values[4].Replace("\"", "")),
                        ExecutionPrice = Convert.ToDouble(values[5].Replace("\"", ""))


                    };

                    trades.Add(trade);
                }
            }

            return trades;
        }

        #endregion
    }
}
