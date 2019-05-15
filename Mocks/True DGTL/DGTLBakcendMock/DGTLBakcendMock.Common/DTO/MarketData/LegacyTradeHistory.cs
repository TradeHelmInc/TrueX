using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData
{
    public class LegacyTradeHistory : WebSocketMessage
    {
        public string TradeId { get; set; }

        public string Symbol { get; set; }

        public double TradePrice { get; set; }

        public double TradeQuantity { get; set; }

        public bool myTrade { get; set; }

        public long TradeTimeStamp { get; set; }
    }
}
