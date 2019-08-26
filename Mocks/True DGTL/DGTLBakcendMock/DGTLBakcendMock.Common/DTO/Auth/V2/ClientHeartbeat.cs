using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ClientHeartbeat : WebSocketMessageV2
    {
        #region Public Attributes

        public string JsonWebToken { get; set; }

        public long Time { get; set; }

        #endregion
    }
}
