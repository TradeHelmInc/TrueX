using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientTradeRecord : WebSocketMessageV2
    {
        #region Public Static Consts

        public static char _STATUS_OPEN= 'O';
        public static char _STATUS_CANCELLED = 'C';

        public static char _SIDE_BUY = 'B';
        public static char _SIDE_SELL = 'S';

        #endregion


        #region Public Attributes

        public string Uuid { get; set; }

        public long? FirmId { get; set; }

        public string UserId { get; set; }

        public string OrderId { get; set; }

        public string TradeId { get; set; }

        public string ClientOrderId { get; set; }

        public string Symbol { get; set; }

        public string InstrumentId { get; set; }

        private byte? side;
        public byte? Side
        {
            get { return side; }
            set { side = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char? cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

        public double TradePrice { get; set; }

        public double TradeQty { get; set; }

        public double Notional { get; set; }

        public double ExchangeFees { get; set; }

        private byte status;
        public byte Status
        {
            get { return status; }
            set { status = Convert.ToByte(value); }
        }

        [JsonIgnore]
        public char cStatus { get { return Convert.ToChar(Status); } set { Status = Convert.ToByte(value); } }

        public string CreatedAt { get; set; }

        public string UpdatedAt { get; set; }

        public string AccountId { get; set; }


        #endregion
    }
}
