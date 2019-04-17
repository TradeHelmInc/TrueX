using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderReq : WebSocketMessage
    {

        #region Public Static Conts

        public static char _ORD_TYPE_LIMIT = '1';
        public static char _ORD_TYPE_MARKET = '2';

        public static char _SIDE_BUY = 'B';
        public static char _SIDE_SELL = 'S';

        public static char _TIF_DAY = '0';

        #endregion

        #region Public Attributes

        public string JsonWebToken { get; set; }

        public string OrderId { get; set; }

        public string UserId { get; set; }

        public string ClOrderId { get; set; }

        public string AccountId { get; set; }

        public string InstrumentId { get; set; }

        public decimal? Price { get; set; }

        private byte side;
        public byte Side
        {
            get { return side; }
            set { side = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

        public decimal Quantity { get; set; }

        private byte timeInForce;
        public byte TimeInForce
        {
            get { return timeInForce; }
            set { timeInForce = Convert.ToByte(value); }
        }//O -> DAY

        [JsonIgnore]
        public char cTimeInForce { get { return Convert.ToChar(TimeInForce); } set { TimeInForce = Convert.ToByte(value); } }

        private byte orderType;
        public byte OrderType
        {
            get { return orderType; }
            set { orderType = Convert.ToByte(value); }
        }//1-> Limit, 2-> Market?

        [JsonIgnore]
        public char cOrderType { get { return Convert.ToChar(OrderType); } set { OrderType = Convert.ToByte(value); } }


        #endregion
    }
}
