using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth
{
    public class WebSocketHeartbeatMessage : WebSocketMessage
    {
        public string UUID { get; set; }

        public string JWToken { get; set; }

        public string PingPong { get; set; }
    }
}
