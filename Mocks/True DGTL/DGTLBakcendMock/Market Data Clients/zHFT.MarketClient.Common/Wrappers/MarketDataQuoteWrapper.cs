using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;

namespace zHFT.MarketClient.Common.Wrappers
{
    public class MarketDataQuoteWrapper : MarketDataWrapper
    {
        #region Constructors

        public MarketDataQuoteWrapper(Security pSecurity, IConfiguration pConfig)
            : base(pSecurity, pConfig)
        {

        }

        #endregion

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.MARKET_DATA_QUOTES;
        }
    }
}
