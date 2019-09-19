using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class CreditLimit
    {
        #region Public Static Consts

        public static char _TRADING_STATUS_FALSE = '0';
        public static char _TRADING_STATUS_TRUE = '1';


        #endregion

        #region Public Attriutes
        
        public string FirmCreditId { get; set; }

        public string CurrencyRootId { get; set; }

        public double Total { get; set; }

        public double Usage { get; set; }

        public decimal PotentialExposure { get; set; }

        public decimal MaxTradeSize { get; set; }

        public decimal MaxQtySize { get; set; }

        private byte tradingStatus;
        public byte TradingStatus
        {
            get { return tradingStatus; }
            set { tradingStatus = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char cTradingStatus { get { return Convert.ToChar(TradingStatus); } set { TradingStatus = Convert.ToByte(value); } }



        #endregion
    }
}
