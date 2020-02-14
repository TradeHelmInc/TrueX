using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ForgotPasswordResponse : PasswordResetMessageV2
    {
        #region Public Attributes

        public string MessageName { get; set; }

        public string Uuid { get; set; }

        public string Message { get; set; }

        #endregion
    }
}
