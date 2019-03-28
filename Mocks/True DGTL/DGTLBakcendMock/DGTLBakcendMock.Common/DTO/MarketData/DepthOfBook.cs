using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData
{
    public class DepthOfBook : WebSocketMessage
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public long DepthTime { get; set; }

        public string Action { get; set; }//Action A -> Add, C->Change R-Remove

        public string BidOrAsk { get; set; }//B -> Bid, A -> Ask

        public int Index { get; set; }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        #endregion
    }
}
