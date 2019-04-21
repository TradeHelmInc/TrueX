using DGTLBackendMock.Common.DTO.OrderRouting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace DGTLBackendMock.Common.Wrappers
{
    public class NewOrderSingleWrapper : Wrapper
    {
        #region Protected Attributes

        protected LegacyOrderReq LegacyOrderReq { get; set; }

        protected string OutgoingSymbol { get; set; }

        #endregion


        #region Constructors

        public NewOrderSingleWrapper(LegacyOrderReq pLegactyOrderReq, string pOutgoingSymbol)
        {
            LegacyOrderReq = pLegactyOrderReq;

            OutgoingSymbol = pOutgoingSymbol;
        
        }

        #endregion

        #region Private Methods


        private Side GetSide()
        {
            if (LegacyOrderReq.cSide == LegacyOrderReq._SIDE_BUY)
                return Side.Buy;
            else if (LegacyOrderReq.cSide == LegacyOrderReq._SIDE_SELL)
                return Side.Sell;
            else
                throw new Exception(string.Format("Invalid side for new order: {0}", LegacyOrderReq.cSide));
        
        
        }

        #endregion

        #region Abstract Methods

        public override object GetField(zHFT.Main.Common.Enums.Fields field)
        {
            OrderFields oField = (OrderFields)field;

            if (LegacyOrderReq == null)
                return OrderFields.NULL;

            if (oField == OrderFields.ClOrdID)
                return LegacyOrderReq.ClOrderId;
            if (oField == OrderFields.OrigClOrdID)
                return LegacyOrderReq;
            else if (oField == OrderFields.SettlType)
                return LegacyOrderReq;
            else if (oField == OrderFields.SettlDate)
                return LegacyOrderReq;
            else if (oField == OrderFields.Symbol)
                return OutgoingSymbol;
            else if (oField == OrderFields.SecurityType)
                return SecurityType.CC;
            else if (oField == OrderFields.Currency)
                return Currency.USD;
            else if (oField == OrderFields.Exchange)
                return "Test";
            else if (oField == OrderFields.OrdType)
                return OrdType.Limit;
            else if (oField == OrderFields.PriceType)
                return PriceType.FixedAmount;
            else if (oField == OrderFields.Price)
                return LegacyOrderReq.Price.HasValue ? (decimal?)LegacyOrderReq.Price.Value : null;
            else if (oField == OrderFields.StopPx)
                return OrderFields.NULL;
            else if (oField == OrderFields.ExpireDate)
                return OrderFields.NULL;
            else if (oField == OrderFields.ExpireTime)
                return OrderFields.NULL;
            else if (oField == OrderFields.Side)
                return GetSide();
            else if (oField == OrderFields.OrderQty)
                return LegacyOrderReq.Quantity;
            else if (oField == OrderFields.CashOrderQty)
                return OrderFields.NULL;
            else if (oField == OrderFields.OrderPercent)
                return OrderFields.NULL;
            else if (oField == OrderFields.TimeInForce)
                return TimeInForce.Day;//Always Day for the moment
            else if (oField == OrderFields.MinQty)
                return OrderFields.NULL;
            else if (oField == OrderFields.Account)
                return LegacyOrderReq.AccountId;
            else if (oField == OrderFields.DecimalPrecission)
                return OrderFields.NULL;

            return OrderFields.NULL;
        }

        public override zHFT.Main.Common.Enums.Actions GetAction()
        {
            return Actions.NEW_ORDER;
        }

        #endregion
    }
}
