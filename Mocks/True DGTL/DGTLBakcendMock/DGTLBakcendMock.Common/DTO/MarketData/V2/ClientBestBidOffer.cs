using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData.V2
{
    public class ClientBestBidOffer : WebSocketMessageV2
    {
        #region Public  Attributes

        public string UUID { get; set; }

        public long InstrumentId { get; set; }

        public decimal? Bid { get; set; }

        public decimal? BidSize { get; set; }

        public decimal? Ask { get; set; }

        public decimal? AskSize { get; set; }

        public decimal? MidPoint { get; set; }

        #endregion
    }
}
