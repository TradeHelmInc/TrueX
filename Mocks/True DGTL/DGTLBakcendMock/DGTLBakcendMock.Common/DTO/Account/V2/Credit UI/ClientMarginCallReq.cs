using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class ClientMarginCallReq : WebSocketMessageV2
    {
        #region Public Attributes

        public string UserId { get; set; }

        public string Uuid { get; set; }

        public string AccountId { get; set; }

        #endregion
    }
}
