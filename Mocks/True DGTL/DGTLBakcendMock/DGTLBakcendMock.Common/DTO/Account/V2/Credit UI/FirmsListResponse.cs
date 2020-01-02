using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class FirmsListResponse : WebSocketMessageV2
    {
        #region Public Attributes

        //public string JsonWebToken { get; set; }

        public string SettlementAgentId { get; set; }

        public string Uuid { get; set; }

        public int PageNo { get; set; }

        public int PageRecords { get; set; }

        public bool Success { get; set; }
   
        public string Msg { get; set; }

        //public ClientFirmRecord[] Firms { get; set; }

        public FirmsCreditLimitRecord[] Firms { get; set; }

        public long Time { get; set; }

        #endregion
    }
}
