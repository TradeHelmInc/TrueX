using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.Blotters
{
    public class GetOrdersBlotterFulFilled
    {
        public string Msg { get; set; }

        public LegacyOrderBlotter[] data { get; set; }  
      
    }
}
