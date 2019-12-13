using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData.V2
{
    public class ClientDepthOfBookBatch
    {
        public string Msg { get; set; }

        public ClientDepthOfBook[] messages { get; set; }
    }
}
