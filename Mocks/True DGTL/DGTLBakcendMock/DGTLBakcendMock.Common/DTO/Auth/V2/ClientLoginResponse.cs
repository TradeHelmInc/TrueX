using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ClientLoginResponse : WebSocketMessageV2
    {

        #region Public Methods

        public long UserId { get; set; }

        public string JsonWebToken { get; set; }

        public string UUID { get; set; }

        //public bool Status { get; set; }

        public bool Success { get; set; }

        public long Time { get; set; }

        public string Message { get; set; }


        #endregion
    }
}
