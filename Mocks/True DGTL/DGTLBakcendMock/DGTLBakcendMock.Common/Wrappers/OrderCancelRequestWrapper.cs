using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace DGTLBackendMock.Common.Wrappers
{
    public class OrderCancelRequestWrapper : Wrapper
    {
        #region Public Attributes

        public string OrigClOrderId { get; set; }

        public string ClOrderId { get; set; }

        public string Symbol { get; set; }

        #endregion

        #region Constructors

        public OrderCancelRequestWrapper(string pOrigClOrderId, string pClOrderId, string pSymbol)
        {

            OrigClOrderId = pOrigClOrderId;

            ClOrderId = pClOrderId;

            Symbol = pSymbol;
        
        }

        #endregion

        #region Wrapper Methods

        public override object GetField(zHFT.Main.Common.Enums.Fields field)
        {
            OrderFields oField = (OrderFields)field;

            if (oField == OrderFields.ClOrdID)
                return ClOrderId;
            if (oField == OrderFields.OrigClOrdID)
                return OrigClOrderId;
            else if (oField == OrderFields.OrderId)
                return OrderFields.NULL;
            else if (oField == OrderFields.Symbol)
                return Symbol;
            else
                return OrderFields.NULL;
        }

        public override zHFT.Main.Common.Enums.Actions GetAction()
        {
            return Actions.CANCEL_ORDER;
        }

        #endregion
    }
}
