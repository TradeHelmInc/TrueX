using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientSettlementStatus : WebSocketMessageV2
    {

        #region Static Attributes

        public static string _SETTL_DIRECTION_RECEIVE = "Receive";

        public static string _SETTL_DIRECTION_SEND = "Send";

        public static string _STATUS_WAITING = "Waiting for TXID";

        public static string _STATUS_CONFIRMING = "Confirming";

        public static string _STATUS_CONFIRMED = "Confirmed";

        public static string _STATUS_PENDING_CONFIRMATION = "Pending Confirmation";

        public static string _STATUS_DELIVERED = "Delivered";

        public static string _FIAT_DELIVERY_CURRENCY = "USD";

        public static string _CRYPTO_DELIVERY_CURRENCY = "XBT";

        #endregion

        #region Public Attributes

        public string Uuid { get; set; }

        public string FirmId { get; set; }

        public string UserId { get; set; }

        public string AccountId { get; set; }

        public string InstrumentId { get; set; }

        public string SettlementDate { get; set; }

        public string SettlementDirection { get; set; }

        public string FiatDeliveryCurrency { get; set; }

        public double FiatDeliveryAmount { get; set; }

        public string FiatDeliveryWalletAddress { get; set; }

        public double? SendAmount { get; set; }

        public string SendCurrency { get; set; }

        public string SendAddress { get; set; }

        public string SendTxId { get; set; }

        public string SendStatus { get; set; }

        public double? ReceiveAmount { get; set; }

        public string ReceiveCurrency { get; set; }

        public string ReceiveAddress { get; set; }

        public string ReceiveTxId { get; set; }

        public string ReceiveStatus { get; set; }



        #endregion
    }
}
