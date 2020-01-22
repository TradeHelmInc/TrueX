using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientMyOrders : WebSocketMessageV2
    {
        public static char _SIDE_BUY = 'B';
        public static char _SIDE_SELL = 'S';

        #region Public Attributes

        public string Msg { get; set; }

        public string Uuid { get; set; }

        public string OrderId { get; set; }

        public string InstrumentId { get; set; }

        public double? Price { get; set; }

        public double Quantity { get; set; }

        private byte? side;
        public byte? Side
        {
            get { return side; }
            set { side = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char? cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

        public double LeavesQty { get; set; }

        public double CumQty { get; set; }

        private byte? status;
        public byte? Status
        {
            get { return status; }
            set { status = Convert.ToByte(value); }
        }

        [JsonIgnore]
        public char? cStatus { get { return Convert.ToChar(Status); } set { Status = Convert.ToByte(value); } }
        //O-> Open, C-> Cancelled,R-> Rejected, F-> Filled, E-> Expired

        public string Timestamp { get; set; }

        public int Time { get; set; }

        public string UserId { get; set; }

        public int Sender { get; set; }

        #endregion
    }
}
