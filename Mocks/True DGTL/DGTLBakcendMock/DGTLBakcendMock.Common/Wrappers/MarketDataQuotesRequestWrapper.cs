using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace DGTLBackendMock.Common.Wrappers
{
    public class MarketDataQuotesRequestWrapper : MarketDataRequestWrapper
    {
        #region Constructors

        public MarketDataQuotesRequestWrapper(Security pSecurity, SubscriptionRequestType pSubscriptionRequestType):base(pSecurity,pSubscriptionRequestType)
        {
           
        }

        public MarketDataQuotesRequestWrapper(int pMdReqId, Security pSecurity, SubscriptionRequestType pSubscriptionRequestType):base(pMdReqId,pSecurity,pSubscriptionRequestType)
        {
          
        }

        #endregion

        #region Wrapper Methods

        public override Actions GetAction()
        {
            return Actions.MARKET_DATA_QUOTES_REQUEST;
        }

        #endregion
    }
}
