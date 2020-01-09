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

        public string Uuid { get; set; }

        public string InstrumentId { get; set; }

        public decimal? Bid { get; set; }

        public decimal? BidSize { get; set; }

        public decimal? Offer { get; set; }

        public decimal? OfferSize { get; set; }

        public decimal? MidPrice{ get; set; }

        #endregion
    }
}
