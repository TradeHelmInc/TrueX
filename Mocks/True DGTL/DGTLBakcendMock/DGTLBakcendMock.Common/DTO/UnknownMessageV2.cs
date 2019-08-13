using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO
{
    public class UnknownMessageV2 : WebSocketMessageV2
    {
        public string Resp { get; set; }

        public string Reason { get; set; }
    }
}
