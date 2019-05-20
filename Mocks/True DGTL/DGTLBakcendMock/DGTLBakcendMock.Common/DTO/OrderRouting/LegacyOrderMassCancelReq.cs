using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderMassCancelReq: WebSocketMessage
    {
        #region Public Attributes

        public string UUID { get; set; }

        public string Symbol { get; set; }

        public string UserId { get; set; }

        public string JsonWebToken { get; set; }

        #endregion
    }
}
