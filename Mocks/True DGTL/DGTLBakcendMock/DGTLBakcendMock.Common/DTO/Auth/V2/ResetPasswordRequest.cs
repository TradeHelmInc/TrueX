using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ResetPasswordRequest : PasswordResetMessageV2
    {
        #region Protected Attributes

        public string MessageName { get; set; }

        public string Uuid { get; set; }

        public string TempSecret { get; set; }

        public string NewSecret { get; set; }

        #endregion
    }
}
