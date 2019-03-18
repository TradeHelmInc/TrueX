using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth
{
    public class WebSocketLoginMessage : WebSocketMessage
    {
        public string UUID { get; set; }

        public string UserId { get; set; }

        public string Password { get; set; }
    }
}
