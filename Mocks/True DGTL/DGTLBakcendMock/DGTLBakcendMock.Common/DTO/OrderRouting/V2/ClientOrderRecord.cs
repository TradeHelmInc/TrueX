using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientOrderRecord : WebSocketMessageV2
    {
        #region Public Static Consts

        public static char _SIDE_BUY = 'B';
        public static char _SIDE_SELL = 'S';

        public static char _STATUS_OPEN = 'O';
        public static char _STATUS_CANCELLED = 'C';
        public static char _STATUS_REJECTED = 'R';
        public static char _STATUS_FILLED = 'F';
        public static char _STATUS_EXPIRED = 'E';

        #endregion

        #region Public Attributes

         public string UUID { get; set; }

         public long FirmId{ get; set; }

         public string UserId { get; set; }

         public long OrderId { get; set; }

         public string ClientOrderId { get; set; }

         public long InstrumentId { get; set; }

         private byte side;
         public byte Side
         {
             get { return side; }
             set { side = Convert.ToByte(value); }
         }//Side B -> Buy, S->Sell

         [JsonIgnore]
         public char cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

         public double? Price { get; set; }

         public double Quantity { get; set; }

         public double LeavesQty { get; set; }

         public double CumQty { get; set; }

         public double? AveragePrice { get; set; }

         public double? Notional{ get; set; }

         public decimal? ExchageFees{ get; set; }


         private byte status;
         public byte Status
         {
             get { return status; }
             set { status = Convert.ToByte(value); }
         }//Side B -> Buy, S->Sell

         [JsonIgnore]
         public char cStatus { get { return Convert.ToChar(Status); } set { Status = Convert.ToByte(value); } }

         public long CreateTimeStamp { get; set; }

         public long TimeStamp { get; set; }


         public string Message { get; set; }

        #endregion
    }
}
