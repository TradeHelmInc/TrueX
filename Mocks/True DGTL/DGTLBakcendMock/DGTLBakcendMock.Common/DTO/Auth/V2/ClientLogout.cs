using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ClientLogout : WebSocketMessageV2
    {
        public string Uuid { get; set; }

        public string UserId { get; set; }

        public string Token { get; set; }
    }
}
