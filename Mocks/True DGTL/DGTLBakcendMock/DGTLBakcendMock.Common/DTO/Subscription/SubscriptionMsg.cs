using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Subscription
{
    public class SubscriptionMsg : WebSocketMessage
    {
        public int Sender { get; set; }

        public string SeqNum { get; set; }

        public int Expected { get; set; }

        public string Service { get; set; }

        public string Symbol { get; set; }
    }
}
