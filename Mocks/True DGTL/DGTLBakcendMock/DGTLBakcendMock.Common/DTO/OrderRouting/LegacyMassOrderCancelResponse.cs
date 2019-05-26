using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyMassOrderCancelResponse : WebSocketMessage
    {
        public string UUID { get; set; }

        public string RejectReason { get; set; }

        public string OrderId { get; set; }
    }
}
