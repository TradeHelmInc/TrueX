using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class FirmsListRequest : WebSocketMessageV2
    {
        #region Public Attributes

        public string JsonWebToken { get; set; }

        public string Uuid { get; set; }

        public int PageNo { get; set; }

        public int PageRecords { get; set; }

        public long Time { get; set; }

        //public string SettlementAgentId { get; set; }

        #endregion
    }
}
