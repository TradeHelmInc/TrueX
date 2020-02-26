using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ResetPasswordResponse : WebSocketMessage
    {
        public string UUID { get; set; }

        public string UserId { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
    }
}
