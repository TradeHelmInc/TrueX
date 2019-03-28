using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth
{
    public class ClientHeartbeatResponse : WebSocketMessage
    {
        public string UserId { get; set; }

        public int SeqNum { get; set; }

        public string JsonWebToken { get; set; }
    }
}
