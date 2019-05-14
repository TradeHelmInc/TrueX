using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderExecutionReport : WebSocketMessage
    {
        #region Public static Consts

        public static string _ORD_STATUS_NEW = "new";

        public static string _ORD_sTATUS_REJECTED = "rejected";

        public static string _ORD_STATUS_CANCELED = "cancelled";

        public static string _ORD_STATUS_FILLED = "full_fill";

        public static string _ORD_sTATUS_PARTIALLY_FILLED = "partial_fill";

        public static char _SIDE_BUY = 'B';

        public static char _SIDE_SELL = 'S';


        #endregion

        #region Public Attributes

        public string OrderId { get; set; }

        public string UserId { get; set; }

        public string ClOrderId { get; set; }

        public string InstrumentId { get; set; }

        private byte side;
        public byte Side
        {
            get { return side; }
            set { side = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

        public string Status { get; set; }

        public decimal Quantity { get; set; }

        public decimal? Price { get; set; }

        public decimal LeftQty { get; set; }

        public long Timestamp { get; set; }


        #endregion
    }
}
