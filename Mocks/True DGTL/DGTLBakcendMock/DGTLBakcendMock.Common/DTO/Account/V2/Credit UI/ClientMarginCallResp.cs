using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class ClientMarginCallResp : WebSocketMessageV2
    {
        #region Public Attributes

        public string UserId { get; set; }

        public string Uuid { get; set; }

        public string AccountId { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public string SignetTDPWallet { get; set; }

        public double? Amount { get; set; }

        public long MarginRegulationTime { get; set; }

        #endregion
    }
}
