using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderCancelAck : LegacyOrderExecutionReport
    {
        public string OrigClOrderId { get; set; }

        public string CancelReason { get; set; }

        public string UUID { get; set; }
    }
}
