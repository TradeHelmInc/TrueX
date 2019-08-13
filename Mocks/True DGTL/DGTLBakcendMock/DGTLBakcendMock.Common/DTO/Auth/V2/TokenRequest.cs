using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class TokenRequest : WebSocketMessageV2
    {
        public string SourceIP { get; set; }
    }
}
