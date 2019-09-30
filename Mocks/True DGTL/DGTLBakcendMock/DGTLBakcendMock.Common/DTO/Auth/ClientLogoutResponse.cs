using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth
{
    public class ClientLogoutResponse : WebSocketMessage
    {
        public int Time { get; set; }

        public string UserId { get; set; }

        public bool ReLogin { get; set; }
    }
}
