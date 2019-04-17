using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;

namespace DGTLBackendMock.Common.DTO
{
    public class SecurityMapping
    {
        #region Constructor

        public SecurityMapping()
        {
            OrderBookEntriesToPublish = new Queue<OrderBookEntry>();
        }

        #endregion

        #region Public Attributs

        public string IncomingSymbol { get; set; }

        public string OutgoingSymbol { get; set; }

        public bool SubscribedLS { get; set; }

        public bool SubscribedLQ { get; set; }

        public bool SubscribedFD { get; set; }

        public bool SubscribedFP { get; set; }

        public bool SubscribedLD { get; set; }

        public bool PendingLSResponse { get; set; }

        public bool PendingLQResponse { get; set; }

        public bool PendingFDResponse { get; set; }

        public bool PendingFPResponse { get; set; }

        public bool PendingLDResponse { get; set; }

        public zHFT.Main.BusinessEntities.Market_Data.MarketData PublishedMarketData { get; set; }

        public Queue<OrderBookEntry> OrderBookEntriesToPublish { get; set; }

        #endregion

        #region Public Methods

        public bool SubscribedMarketData()
        {
            return SubscribedLS || SubscribedLQ || SubscribedFP || SubscribedFD || SubscribedLD;
        
        }

        #endregion
    }
}
