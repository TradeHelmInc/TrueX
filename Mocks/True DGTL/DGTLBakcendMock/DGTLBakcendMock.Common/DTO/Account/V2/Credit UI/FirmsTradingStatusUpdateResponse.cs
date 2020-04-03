using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class FirmsTradingStatusUpdateResponse : WebSocketMessageV2
    {
        #region Pubic Attributes

        public string JsonWebToken { get; set; }

        public string Uuid { get; set; }

        //public ClientFirmRecord Firm { get; set; }

        public FirmsCreditRecord Firm { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public long Time { get; set; }

        #endregion
    }
}
