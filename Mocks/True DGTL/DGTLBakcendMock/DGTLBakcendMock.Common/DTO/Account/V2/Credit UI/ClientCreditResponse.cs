using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class ClientCreditResponse : WebSocketMessageV2
    {
        #region Public Attributes

        public string Uuid { get; set; }

        public string FirmId { get; set; }

        public string UserId { get; set; }

      
        public long ExposureChange { get; set; }


        public bool CreditAvailable { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        #endregion
    }
}
