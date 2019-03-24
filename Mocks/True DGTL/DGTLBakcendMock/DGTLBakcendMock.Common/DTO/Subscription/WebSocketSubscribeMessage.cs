using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Subscription
{
    public class WebSocketSubscribeMessage : WebSocketMessage
    {
        public string SubscriptionType { get; set; }

        public string UserId { get; set; }

        public string JsonWebToken { get; set; }

        public string Service { get; set; }

        public string ServiceKey { get; set; }
    }
}
