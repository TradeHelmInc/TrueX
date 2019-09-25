using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class FirmsCreditLimitRecord : WebSocketMessageV2
    {
        #region Public Attributes

        public string Uuid { get; set; }

        public ClientFirmRecord Firm { get; set; }

        public long Time { get; set; }

        #endregion
    }
}
