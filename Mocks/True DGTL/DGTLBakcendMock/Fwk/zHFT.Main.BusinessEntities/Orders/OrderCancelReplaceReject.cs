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

        public string OrigClOrdId { get; set; }

        public string ClOrdId { get; set; }

        public CxlRejReason CxlRejReason { get; set; }

        public CxlRejResponseTo CxlRejResponseTo { get; set; }

        public string OrderId { get; set; }

        public string Text { get; set; }

        public string Symbol { get; set; }

        public OrdStatus OrdStatus { get; set; }

        public Side Side { get; set; }

        public decimal? Price { get; set; }

        public decimal LeftQty { get; set; }

        public long Timestamp { get; set; }


        #endregion
    }
}
