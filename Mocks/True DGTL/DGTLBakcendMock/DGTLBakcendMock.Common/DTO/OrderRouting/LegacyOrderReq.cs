using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderReq : WebSocketMessage
    {
        #region Public Attributes

        public string OrderId { get; set; }

        public string UserId { get; set; }

        public string ClOrderId { get; set; }

        public string AccountId { get; set; }

        public string InstrumentId { get; set; }

        public decimal? Price { get; set; }

        public string Side { get; set; }

        public decimal Quantity { get; set; }

        public string TimeInForce { get; set; }

        public string OrderType { get; set; }

        #endregion
    }
}
