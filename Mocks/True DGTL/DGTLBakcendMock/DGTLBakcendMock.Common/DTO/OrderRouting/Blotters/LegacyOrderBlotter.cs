using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.Blotters
{
    public class LegacyOrderBlotter
    {
        #region Public Attributes

        public string Account { get; set; }

        public string AgentId { get; set; }

        public decimal AvgPrice { get; set; }

        public string ClOrderId { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public decimal ExecNotional { get; set; }

        public decimal Fees { get; set; }

        public double LeavesQty { get; set; }

        public double FillQty { get; set; }

        public double? LimitPrice { get; set; }

        public string Msg { get; set; }

        public string OrderId { get; set; }

        public double OrderQty { get; set; }

        public int OrderType { get; set; }

        public string RejectReason { get; set; }

        public int Sender { get; set; }

        private byte side;
        public byte Side
        {
            get { return side; }
            set { side = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

        private byte status;
        public byte Status
        {
            get { return status; }
            set { status = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char cStatus { get { return Convert.ToChar(Status); } set { Status = Convert.ToByte(value); } }

        public string Symbol { get; set; }

        public long Time { get; set; }

        #endregion
    }
}
