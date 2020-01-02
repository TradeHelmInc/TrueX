using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class FirmsCreditLimitRecord : WebSocketMessageV2
    {
        #region Public Attributes

        public string FirmId { get; set; }

        //public string Uuid { get; set; }

        //public string Name { get; set; }

        //public string ShortName { get; set; }

        public double AvailableCredit { get; set; }

        public double UsedCredit { get; set; }

        public double PotentialExposure { get; set; }

        public double MaxNotional { get; set; }

        public double MaxQuantity { get; set; }

        //public bool CurrencyRootId { get; set; }

        private byte tradingStatus;
        public byte TradingStatus
        {
            get { return tradingStatus; }
            set { tradingStatus = Convert.ToByte(value); }
        }//Trading -> T , Suspended -> S

        [JsonIgnore]
        public char cTradingStatus { get { return Convert.ToChar(TradingStatus); } set { TradingStatus = Convert.ToByte(value); } }

        public string[] Accounts { get; set; }

        //public ClientFirmRecord Firm { get; set; }

        public long Time { get; set; }

        #endregion
    }
}
