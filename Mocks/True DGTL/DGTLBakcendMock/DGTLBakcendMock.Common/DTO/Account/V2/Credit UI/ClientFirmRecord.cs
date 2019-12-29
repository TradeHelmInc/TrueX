using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class ClientFirmRecord
    {
        #region Private Consts

        private static char _TRADING_STATUS_TRADING = 'T';
        private static char _TRADING_STATUS_SUSPENDED = 'S';

        #endregion


        #region Public Attributes

        public long FirmId { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        //public CreditLimit CreditLimit { get; set; }

        public double AvailableCredit { get; set; }

        public double UsedCredit { get; set; }

        public double PotentialExposure { get; set; }

        public double MaxNotional { get; set; }

        public double MaxQuantity { get; set; }

        public bool CurrencyRootId { get; set; }

        private byte tradingStatus;
        public byte TradingStatus
        {
            get { return tradingStatus; }
            set { tradingStatus = Convert.ToByte(value); }
        }//Trading -> T , Suspended -> S

        [JsonIgnore]
        public char cTradingStatus { get { return Convert.ToChar(TradingStatus); } set { TradingStatus = Convert.ToByte(value); } }

        public string[] Accounts { get; set; }

        //public ClientAccountRecord[] Accounts { get; set; }

        #endregion
    }
}
