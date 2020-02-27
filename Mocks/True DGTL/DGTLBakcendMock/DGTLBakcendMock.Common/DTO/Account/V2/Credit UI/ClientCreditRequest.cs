using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class ClientCreditRequest : WebSocketMessageV2
    {
        #region Public Attributes

        public string Uuid { get; set; }

        public string SettlementAgentId { get; set; }

        public string FirmId { get; set; }

        public string AccountId { get; set; }

        public string InstrumentId { get; set; }

        private byte side;
        public byte Side
        {
            get { return side; }
            set { side = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }


        public long Quantity { get; set; }

        #endregion
    }
}
