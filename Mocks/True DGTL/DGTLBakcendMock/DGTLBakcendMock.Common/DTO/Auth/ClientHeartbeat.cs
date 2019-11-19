using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth
{
    public class ClientHeartbeat : WebSocketMessage
    {
        public int Time { get; set; }

        public string UUID { get; set; }

        public string UserId { get; set; }

        public int SeqNum { get; set; }
    }
}
