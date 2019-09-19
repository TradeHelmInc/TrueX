using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class FirmsCreditLimitUpdateRequest : WebSocketMessageV2
    {

        #region Public Static Consts

        public static char _TRADING_STATUS_TRADING = 'T';
        public static char _TRADING_STATUS_SUSPENDED = 'S';


        #endregion

        #region Pubic Attributes

        public string JsonWebToken { get; set; }

        public string UUID { get; set; }

        public long FirmId{ get; set; }

        private byte tradingStatus;
        public byte TradingStatus
        {
            get { return tradingStatus; }
            set { tradingStatus = Convert.ToByte(value); }
        }//

        [JsonIgnore]
        public char cTradingStatus { get { return Convert.ToChar(TradingStatus); } set { TradingStatus = Convert.ToByte(value); } }


        public double CreditLimitTotal { get; set; }

        public double CreditLimitBalance { get; set; }

        public double CreditLimitUsage { get; set; }

        public decimal CreditLimitMaxTradeSize { get; set; }

        #endregion
    }
}
