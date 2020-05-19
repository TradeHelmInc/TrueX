using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientGetAccountPositions: WebSocketMessageV2
    {
        #region Public Attributes

        public string UserId { get; set; }

        public string Uuid { get; set; }

        public string SettlementAgentId { get; set; }

        public string FirmId { get; set; }

        public string AccountId { get; set; }

        #endregion
    }
}
