using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientOrderCancelReq : WebSocketMessageV2
    {

        #region Public Attributes


        public string Uuid { get; set; }

        public long FirmId { get; set; }

        public string UserId { get; set; }

        public long OrderId { get; set; }

        public string ClientOrderId { get; set; }

        public string CancelReason { get; set; }

        #endregion
    }
}
