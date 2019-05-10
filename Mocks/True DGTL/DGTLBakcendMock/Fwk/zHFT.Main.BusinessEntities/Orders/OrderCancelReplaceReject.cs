using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Orders
{
    public class OrderCancelReplaceReject
    {
        #region Public Attributes

        public string ClOrdId { get; set; }

        public CxlRejReason CxlRejReason { get; set; }

        public CxlRejResponseTo CxlRejResponseTo { get; set; }

        public string OrderId { get; set; }

        public OrdStatus OrdStatus { get; set; }

        public string OrigClOrdId { get; set; }

        public string Text { get; set; }

        public string Symbol { get; set; }


        #endregion
    }
}
