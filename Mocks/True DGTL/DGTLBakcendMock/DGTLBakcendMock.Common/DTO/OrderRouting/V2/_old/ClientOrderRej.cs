using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientOrderRej : WebSocketMessageV2
    {
        #region Public Attributes

        public string UUID { get; set; }

        public byte ExchangeId { get; set; }

        public string ClientOrderId { get; set; }

        private byte rejectCode;
        public byte RejectCode
        {
            get { return rejectCode; }
            set { rejectCode = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char cRejectCode { get { return Convert.ToChar(RejectCode); } set { RejectCode = Convert.ToByte(value); } }

        public long TransactionTimes { get; set; }

        #endregion
    }
}
