using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderRecord : WebSocketMessage
    {

        #region Public static Consts

        public static char _STATUS_OPEN = 'O';

        public static char _STATUS_REJECTED = 'R';

        public static char _STATUS_CANCELED = 'C';

        public static char _STATUS_FILLED = 'F';

        public static char _STATUS_PARTIALLY_FILLED = 'P';

        public static char _STATUS_EXPIRED = 'E';

        #endregion

        #region Public Attributes

        public string OrderId { get; set; }

        public string UserId { get; set; }

        public string ClientOrderId { get; set; }

        public string InstrumentId { get; set; }

        public int Time { get; set; }

        public long UpdateTime { get; set; }

        private byte side;
        public byte Side
        {
            get { return side; }
            set { side = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

        public double OrdQty { get; set; }

        public double? Price { get; set; }

        public double LvsQty { get; set; }

        public double FillQty { get; set; }

        private byte status;
        public byte Status
        {
            get { return status; }
            set { status = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char cStatus { get { return Convert.ToChar(Status); } set { Status = Convert.ToByte(value); } }

        #endregion

        #region Public Static Methods

        public static char GetStatus(OrdStatus status)
        {
            if (status == OrdStatus.AcceptedForBidding || status == OrdStatus.Calculated || status == OrdStatus.New
                 || status == OrdStatus.PendingCancel || status == OrdStatus.PendingNew || status == OrdStatus.PendingReplace
                 || status == OrdStatus.Replaced)
            {
                return _STATUS_OPEN;
            }
            else if (status == OrdStatus.Canceled)
                return _STATUS_CANCELED;
            else if (status == OrdStatus.Rejected || status == OrdStatus.Suspended)
                return _STATUS_REJECTED;
            else if (status == OrdStatus.Filled)
                return _STATUS_FILLED;
            else if (status == OrdStatus.PartiallyFilled)
                return _STATUS_PARTIALLY_FILLED;
            else if (status == OrdStatus.Expired || status == OrdStatus.DoneForDay)
                return _STATUS_EXPIRED;
            else throw new Exception(string.Format("Unkwnown order status: {0}", status.ToString()));
        
        }

        #endregion
    }
}
