using DGTLBackendMock.Common.DTO.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData
{
    public class Quote : WebSocketMessage
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public decimal? Bid { get; set; }

        public decimal? BidSize { get; set; }

        public decimal? Ask { get; set; }

        public decimal? AskSize { get; set; }

        #endregion
    }
}
