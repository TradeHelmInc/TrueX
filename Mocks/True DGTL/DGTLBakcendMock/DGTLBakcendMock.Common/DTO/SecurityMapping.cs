using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO
{
    public class SecurityMapping
    {
        #region Public Attributs

        public string IncomingSymbol { get; set; }

        public string OutgoingSymbol { get; set; }

        public bool SubscribedLS { get; set; }

        public bool SubscribedLQ { get; set; }

        public bool PendingLSResponse { get; set; }

        public bool PendingLQResponse { get; set; }

        public zHFT.Main.BusinessEntities.Market_Data.MarketData PublishedMarketData { get; set; }


        #endregion
    }
}
