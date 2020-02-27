using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ResetPasswordRequest : WebSocketMessage
    {
        #region Protected Attributes

        public string UUID { get; set; }

        public string TempSecret { get; set; }

        //public string OldPassword { get; set; }

        public string NewSecret { get; set; }

        //public string NewPassword { get; set; }

        public string UserId { get; set; }

        public string JsonWebToken { get; set; }

        #endregion
    }
}
