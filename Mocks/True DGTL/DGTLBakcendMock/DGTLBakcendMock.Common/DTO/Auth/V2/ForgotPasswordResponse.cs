using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ForgotPasswordResponse : WebSocketMessage
    {
        #region Public Attributes

        public string Uuid { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        #endregion
    }
}
