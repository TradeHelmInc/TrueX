using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderCancelReq : WebSocketMessage
    {

        #region Public Attributes

        public string UserId { get; set; }

        public string ClOrderId { get; set; }

        public string OrigClOrderId { get; set; }

        public string InstrumentId { get; set; }

        public byte Side { get; set; }

        [JsonIgnore]
        public char cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

        public string JsonWebToken { get; set; }

        #endregion
    }
}
