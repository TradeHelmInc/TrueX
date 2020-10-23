using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Temp.Positions
{
    public class TradeDTO
    {

        #region Public Static Attributes

        public static string _TRADE_BUY = "BUY";

        public static string _TRADE_SELL = "SELL";

        #endregion

        #region Public Attributes

        public DateTime Date { get; set; }

        public string Symbol { get; set; }

        public double ExecutionPrice { get; set; }

        public double ExecutionSize { get; set; }

        public string Side { get; set; }

        #endregion

        #region Public Methods

        public double GetSignedExecutionSize()
        {

            return Side == _TRADE_BUY ? ExecutionSize : -1 * ExecutionSize;
        }

        public double GetSignedExecutionSize(double tradeSize)
        {

            return Side == _TRADE_BUY ? Math.Abs(tradeSize) : -1 * Math.Abs(tradeSize);
        }


        #endregion
    }
}
