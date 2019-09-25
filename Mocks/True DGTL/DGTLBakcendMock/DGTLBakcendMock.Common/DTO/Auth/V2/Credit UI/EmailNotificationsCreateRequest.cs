using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2.Credit_UI
{
    public class EmailNotificationsCreateRequest : WebSocketMessageV2
    {
        #region Public Attributes

        public string JsonWebToken { get; set; }

        public string Uuid { get; set; }

        public string SettlementFirmId { get; set; }

        public long Time { get; set; }

        public string Email { get; set; }

        #endregion
    }
}
