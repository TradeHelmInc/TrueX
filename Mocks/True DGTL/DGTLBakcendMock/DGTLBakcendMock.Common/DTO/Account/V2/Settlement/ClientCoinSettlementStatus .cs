using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientCoinSettlementStatus : WebSocketMessageV2
    {

        #region Public Static Consts

        public static string _DELIVERY_CRYPTO_CURR = "XBT";

        public static char _DIRECTION_SEND = '1';

        public static char _DIRECTION_RECEIVE = '2';

        public static char _STATUS_WAITING = '1';

        public static char _STATUS_CONFIRMING = '2';

        public static char _STATUS_CONFIRMED = '3';

        public static char _STATUS_PENDING_CONFIRMATION = '4';

        public static char _STATUS_DELIVERED = '5';

        #endregion

        #region Public Attributes

        public string SettlementId { get; set; }

        public string SettlementSubId { get; set; }

        public string FirmId { get; set; }

        public string UserId { get; set; }

        public string AccountId { get; set; }

        public string InstrumentId { get; set; }

        public string SettlementDate { get; set; }

        private byte settlDirection;
        public byte SettlementDirection
        {
            get { return settlDirection; }
            set { settlDirection = Convert.ToByte(value); }
        }

        [JsonIgnore]
        public char cSettlementDirection { get { return Convert.ToChar(SettlementDirection); } set { SettlementDirection = Convert.ToByte(value); } }

        public double? SendAmount { get; set; }

        public string SendCurrency { get; set; }

        public string SendAddress { get; set; }

        public string SendTxId { get; set; }

        private byte sendStatus;
        public byte SendStatus
        {
            get { return sendStatus; }
            set { sendStatus = Convert.ToByte(value); }
        }

        [JsonIgnore]
        public char cSendStatus { get { return Convert.ToChar(SendStatus); } set { SendStatus = Convert.ToByte(value); } }


        public double? ReceiveAmount { get; set; }

        public string ReceiveCurrency { get; set; }

        public string ReceiveAddress { get; set; }

        public string ReceiveTxId { get; set; }


        private byte recvStatus;
        public byte ReceiveStatus
        {
            get { return recvStatus; }
            set { recvStatus = Convert.ToByte(value); }
        }

        [JsonIgnore]
        public char cReceiveStatus { get { return Convert.ToChar(ReceiveStatus); } set { ReceiveStatus = Convert.ToByte(value); } }


        #endregion
    }
}
