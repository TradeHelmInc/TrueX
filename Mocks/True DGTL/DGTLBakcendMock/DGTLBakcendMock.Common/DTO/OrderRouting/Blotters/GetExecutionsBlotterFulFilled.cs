using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.Blotters
{
    public class GetExecutionsBlotterFulFilled
    {
        public string Msg { get; set; }

        public LegacyTradeBlotter[] data { get; set; }  
    }
}
