using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account
{
    public class CreditRecordUpdate : WebSocketMessage
    {
        #region Public Attributes

        public string FirmId { get; set; }

        public string RouteId { get; set; }

        public double CreditUsed { get; set; }

        #endregion
    }
}
