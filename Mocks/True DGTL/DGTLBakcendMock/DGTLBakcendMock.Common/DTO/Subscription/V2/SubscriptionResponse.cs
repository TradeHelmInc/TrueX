using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Subscription.V2
{
    public class SubscriptionResponse : WebSocketMessageV2
    {
        #region Public Attributes

        public string UUID { get; set; }

        public string Service { get; set; }

        public string ServiceKey { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        #endregion
    }
}
