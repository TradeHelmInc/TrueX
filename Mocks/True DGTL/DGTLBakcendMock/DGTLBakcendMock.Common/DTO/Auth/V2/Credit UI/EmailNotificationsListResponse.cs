using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2.Credit_UI
{
    public class EmailNotificationsListResponse : WebSocketMessageV2
    {
        #region Public Attributes

        public string JsonWebToken { get; set; }

        public string UUID { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public string SettlementFirmId { get; set; }

        public Mail[] Emails { get; set; }

        public long Time { get; set; }


        #endregion
    }
}
