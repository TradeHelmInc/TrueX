using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace DGTLBackendMock.Common.Wrappers
{
    public class MarketDataTradesRequestWrapper : MarketDataRequestWrapper
    {

        #region Constructors

        public MarketDataTradesRequestWrapper(Security pSecurity, SubscriptionRequestType pSubscriptionRequestType):base(pSecurity,pSubscriptionRequestType)
        {
           
        }

        public MarketDataTradesRequestWrapper(int pMdReqId, Security pSecurity, SubscriptionRequestType pSubscriptionRequestType):base(pMdReqId,pSecurity,pSubscriptionRequestType)
        {
          
        }

        #endregion

        #region Wrapper Methods


        public override Actions GetAction()
        {
            return Actions.MARKET_DATA_TRADES_REQUEST;
        }

        #endregion
    }
}
