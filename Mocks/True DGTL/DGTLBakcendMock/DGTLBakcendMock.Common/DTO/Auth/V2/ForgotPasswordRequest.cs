﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ForgotPasswordRequest : WebSocketMessage
    {
        #region Protected Attributes

        public string Uuid { get; set; }

        //public string UserId { get; set; }

        public string User { get; set; }

        public string Email { get; set; }

        public string RequestingUser { get; set; }

        public string RequestPattern { get; set; }

        public int Time { get; set; }

        public string JsonWebToken { get; set; }

        #endregion
    }
}
