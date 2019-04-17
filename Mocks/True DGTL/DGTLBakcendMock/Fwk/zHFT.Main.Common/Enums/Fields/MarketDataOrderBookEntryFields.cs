using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public class MarketDataOrderBookEntryFields : Fields
    {
        #region Public Attributes

        public static readonly MarketDataOrderBookEntryFields Symbol = new MarketDataOrderBookEntryFields(2);

        public static readonly MarketDataOrderBookEntryFields MDUpdateAction = new MarketDataOrderBookEntryFields(3);

        public static readonly MarketDataOrderBookEntryFields MDEntrySize = new MarketDataOrderBookEntryFields(4);

        public static readonly MarketDataOrderBookEntryFields MDEntryPx = new MarketDataOrderBookEntryFields(5);

        public static readonly MarketDataOrderBookEntryFields MDEntryType = new MarketDataOrderBookEntryFields(6);

        #endregion

        #region Constructor

        protected MarketDataOrderBookEntryFields(int pInternalValue)
            : base(pInternalValue)
        {

        }

        #endregion
    }
}
