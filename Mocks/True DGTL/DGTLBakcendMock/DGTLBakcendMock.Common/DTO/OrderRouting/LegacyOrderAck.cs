using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderAck : LegacyOrderExecutionReport
    {
        public decimal Quantity { get; set; }

        public string AccountId { get; set; }
    
    }
}
