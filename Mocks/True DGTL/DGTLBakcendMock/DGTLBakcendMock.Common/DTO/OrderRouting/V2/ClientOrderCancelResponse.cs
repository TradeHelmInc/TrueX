using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientOrderCancelResponse : WebSocketMessageV2
    {

        #region Public Attributes


        public string Uuid { get; set; }

        public long FirmId { get; set; }

        public string UserId { get; set; }

        public string OrderId { get; set; }

        public string ClientOrderId { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public string TimeStamp { get; set; }

        #endregion
    }
}
