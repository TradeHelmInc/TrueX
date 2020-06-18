using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientFiatSettlementStatus : WebSocketMessageV2
    {
        #region Public Static Consts

        public static char _FUNDING_STATUS_F = 'F';
        public static char _FUNDING_STATUS_N = 'N';

        public static string _DELIVERY_CURRENCY = "USD";
        

        #endregion

        #region Public Attributes

        public string Uuid { get; set; }

        public string SettlementId { get; set; }

        public string FirmId { get; set; }

        public string UserId { get; set; }

        public string AccountId { get; set; }

        public string InstrumentId { get; set; }

        public string SettlementDate { get; set; }

        public string FiatDeliveryCurrency { get; set; }

        public double? FiatDeliveryAmount { get; set; }

        public double? FiatReceivedAmount { get; set; }

        public string FiatDeliveryWalletAddress { get; set; }

        private byte fundingStatus;
        public byte FundingStatus
        {
            get { return fundingStatus; }
            set { fundingStatus = Convert.ToByte(value); }
        }

        [JsonIgnore]
        public char cFundingStatus { get { return Convert.ToChar(FundingStatus); } set { FundingStatus = Convert.ToByte(value); } }


        public string CoinDeliverAddress { get; set; }

        public int CoinSenderCount { get; set; }

        #endregion
    }
}
