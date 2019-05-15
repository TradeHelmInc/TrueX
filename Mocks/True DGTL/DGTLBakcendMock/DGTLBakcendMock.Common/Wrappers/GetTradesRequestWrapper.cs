using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace DGTLBackendMock.Common.Wrappers
{
    public class GetTradesRequestWrapper : Wrapper
    {
        #region Public Attributes

        protected string Symbol { get; set; }

        protected int MdReqId { get; set; }

        protected SubscriptionRequestType SubscriptionRequestType { get; set; }


        #endregion

        #region Public Constructors

        public GetTradesRequestWrapper(string pSymbol, SubscriptionRequestType pSubscriptionRequestType)
        {

            Symbol = pSymbol;
            SubscriptionRequestType = pSubscriptionRequestType;
        
        }

        public GetTradesRequestWrapper()
        {

            Symbol = null;

        }

        #endregion

        public override object GetField(zHFT.Main.Common.Enums.Fields field)
        {
            MarketDataRequestField mdrField = (MarketDataRequestField)field;

        

            if (mdrField == MarketDataRequestField.Symbol)
                return Symbol;
            if (mdrField == MarketDataRequestField.Exchange)
                return MarketDataRequestField.NULL;
            if (mdrField == MarketDataRequestField.SecurityType)
                return SecurityType.CC;
            if (mdrField == MarketDataRequestField.Currency)
                return MarketDataRequestField.NULL;
            if (mdrField == MarketDataRequestField.MDReqId)
                return MdReqId;
            if (mdrField == MarketDataRequestField.SubscriptionRequestType)
                return SubscriptionRequestType;
            else
                return MarketDataRequestField.NULL;
        }

        public override zHFT.Main.Common.Enums.Actions GetAction()
        {
            return Actions.MARKET_DATA_TRADE_LIST_REQUEST;
        }
    }
}
