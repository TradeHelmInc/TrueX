using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientOrderAck : WebSocketMessageV2
    {
        #region Public Attributes

        public string UUID { get; set; }

        public byte ExchangeId { get; set; }

        public string ClientOrderId { get; set; }

        public string OrderId { get; set; }

        public long TransactionTime { get; set; }

        public string UserId { get; set; }

        #endregion
    }
}
