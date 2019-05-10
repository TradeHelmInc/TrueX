using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderRejAck : LegacyOrderExecutionReport
    {
        public string OrderRejectReason { get; set; }
    }
}
