using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientMarketActivityBatch : WebSocketMessageV2
    {

        public ClientMarketActivityBatch(ClientMarketActivity[] pMessages)
        {
            Msg = "ClientMarketActivityBatch";
            messages = pMessages;
        }

        public ClientMarketActivity[] messages { get; set; }
    }
}
