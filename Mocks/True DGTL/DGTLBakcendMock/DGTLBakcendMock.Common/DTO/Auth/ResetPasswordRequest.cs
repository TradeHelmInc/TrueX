using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth
{
    public class ResetPasswordRequest : WebSocketMessage
    {
        #region Pulic Attributes

        public string UUID { get; set; }

        public string UserId { get; set; }

        public string OldPassword { get; set; }

        public string NewPassword { get; set; }

        public string JsonWebToken { get; set; }

        #endregion
    }
}
