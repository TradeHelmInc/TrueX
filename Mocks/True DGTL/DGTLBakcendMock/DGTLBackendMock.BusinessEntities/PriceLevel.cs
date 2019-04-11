using DGTLBackendMock.BusinessEntities.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.BusinessEntities
{
    public class PriceLevel
    {
        #region Public Attributes

        public OrderBookEntryType OrderBookEntryType { get; set; }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        #endregion

        #region Public Methods

        public bool IsBidOrAsk(bool isBid)
        {
            if (isBid)
                return OrderBookEntryType == OrderBookEntryType.Bid;
            else
                return OrderBookEntryType == OrderBookEntryType.Ask;
        
        }

        #endregion
    }
}
