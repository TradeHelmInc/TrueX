using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderAck : WebSocketMessage
    {
        #region Public Attributes

        public string OrderId { get; set; }

        public string UserId { get; set; }

        public string ClOrderId { get; set; }

        public string InstrumentId { get; set; }

        public string Status { get; set; }

        public decimal? Price { get; set; }

        public decimal LeftQty { get; set; }

        public int Timestamp { get; set; }

        public string OrderRejectReason { get; set; }

        #endregion
    }
}
