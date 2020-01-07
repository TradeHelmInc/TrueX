using DGTLBackendMock.Common.DTO.OrderRouting.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.Blotters
{
    public class GetOrdersBlotterResponse
    {
        public string Msg { get; set; }

        public bool Success { get; set; }

        public string Uuid { get; set; }

        public ClientOrderRecord[] data { get; set; }  
      
    }
}
