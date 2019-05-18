using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Bitmex.BusinessEntities;

namespace zHFT.OrderRouters.Bitmex.Common.Wrappers
{
    public class OrderCancelRejectWrapper : Wrapper
    {

        #region Public Attributes

        protected string OrderId { get; set; }

        protected string OrigClOrderId { get; set; }

        protected string ClOrderId { get; set; }

        protected OrdStatus OrdStatus { get; set; }

        protected Side Side { get; set; }

        protected DateTime TransactTime { get; set; }

        protected CxlRejReason CxlRejReason { get; set; }

        protected string Text { get; set; }

        protected string  Symbol { get; set; }

        protected decimal? Price { get; set; }

        protected decimal LeftQty{ get; set; }

        #endregion

        #region Constructors

        public OrderCancelRejectWrapper(string pOrderId, string pOrigClOrderId, string pClOrderId,
                                        OrdStatus pOrdStatus, Side pSide,decimal? pPrice, decimal pLeftQty, 
                                        DateTime pTransactTime, 
                                        CxlRejReason pCxlRejReason,
                                        string pText,string pSymbol)
        {
            OrderId = pOrderId;

            OrigClOrderId = pOrigClOrderId;

            ClOrderId = pClOrderId;

            OrdStatus = pOrdStatus;

            Side = pSide;

            Price = pPrice;

            LeftQty = pLeftQty;

            TransactTime = pTransactTime;

            CxlRejReason = pCxlRejReason;

            Text = pText;

            Symbol = pSymbol;
            
        }

        #endregion

        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            OrderCancelRejectField xrField = (OrderCancelRejectField)field;

            if (xrField == OrderCancelRejectField.ClOrdID)
                return ClOrderId;
            if (xrField == OrderCancelRejectField.CxlRejReason)
                return CxlRejReason;
            else if (xrField == OrderCancelRejectField.CxlRejResponseTo)
                return CxlRejResponseTo.OrderCancelRequest;
            else if (xrField == OrderCancelRejectField.OrderID)
                return OrderId;
            else if (xrField == OrderCancelRejectField.OrdStatus)
                return OrdStatus;
            else if (xrField == OrderCancelRejectField.Side)
                return Side;
            else if (xrField == OrderCancelRejectField.Price)
                return Price;
            else if (xrField == OrderCancelRejectField.LeftQty)
                return LeftQty;
            else if (xrField == OrderCancelRejectField.OrigClOrdID)
                return OrigClOrderId;
            else if (xrField == OrderCancelRejectField.Text)
                return Text;
            else if (xrField == OrderCancelRejectField.Symbol)
                return Symbol;
            else
                return ExecutionReportFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.ORDER_CANCEL_REJECT;
        }

        #endregion
    }
}
