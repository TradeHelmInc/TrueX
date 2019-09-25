using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class TokenResponse : WebSocketMessageV2
    {
        #region Public Static Consts

        public static char _STATUS_OK = '1';
        public static char _STATUS_FAILED = '0';

        #endregion

        #region Public Attributes

        public string Token { get; set; }

        //public string JsonWebToken { get; set; }

        public string Uuid { get; set; }

        public bool Success { get; set; }

        public long Time { get; set; }

        public string Message { get; set; }


        #endregion

    }
}
