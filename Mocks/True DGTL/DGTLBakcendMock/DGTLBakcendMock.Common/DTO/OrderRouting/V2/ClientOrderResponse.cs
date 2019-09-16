using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientOrderResponse : WebSocketMessageV2
    {
        #region Public Attributes

        public string UUID { get; set; }

        public string UserId { get; set; }

        public long InstrumentId { get; set; }

        public long OrderId { get; set; }

        public string ClientOrderId { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public long Timestamp { get; set; }

        #endregion
    }
}
