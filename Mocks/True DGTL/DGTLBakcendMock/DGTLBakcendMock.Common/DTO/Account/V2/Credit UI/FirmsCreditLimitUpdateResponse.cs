using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class FirmsCreditLimitUpdateResponse : WebSocketMessageV2
    {

        #region Public Attributes

        public string JsonWebToken { get; set; }

        public string Uuid { get; set; }

        public long FirmId { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public long Time { get; set; }

        public ClientFirmRecord Firm { get; set; }


        #endregion
    }
}
