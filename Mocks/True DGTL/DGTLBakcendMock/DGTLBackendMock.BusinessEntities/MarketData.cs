using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.BusinessEntities
{
    public class MarketData
    {
        #region Public Attributes

        public decimal? BestBidPrice { get; set; }

        public decimal? BestBidSize { get; set; }

        public decimal? BestAskPrice { get; set; }

        public decimal? BestAskSize { get; set; }

        public decimal? OpeningPrice { get; set; }

        public decimal? ClosingPrice { get; set; }

        public decimal? TradingSessionHighPrice { get; set; }

        public decimal? TradingSessionLowPrice { get; set; }

        public decimal? NominalVolume { get; set; }

        public decimal? SettlementPrice { get; set; }

        public decimal? FIXRate { get; set; }//?

        public DateTime? LastTradeDateTime { get; set; }

        public decimal? MDTradeSize { get; set; }

        public decimal? Trade { get; set; }

        public decimal? NetChgPrevDay { get; set; }

        #endregion
    }
}
