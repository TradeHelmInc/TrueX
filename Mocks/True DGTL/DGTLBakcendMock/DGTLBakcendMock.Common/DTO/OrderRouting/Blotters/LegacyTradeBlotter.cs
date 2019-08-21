using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.Blotters
{
    public class LegacyTradeBlotter
    {
        #region Public Attributes

        public string Account { get; set; }

        public string AgentId { get; set; }

        public string Symbol { get; set; }

        public double ExecPrice { get; set; }

        public double ExecQty { get; set; }

        public long ExecutionTime { get; set; }

        public string Msg { get; set; }

        public double Notional { get; set; }

        public string OrderId { get; set; }

        public int Sender { get; set; }

        //public long Time { get; set; }

        public string TradeId { get; set; }


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


        #endregion
    }
}
