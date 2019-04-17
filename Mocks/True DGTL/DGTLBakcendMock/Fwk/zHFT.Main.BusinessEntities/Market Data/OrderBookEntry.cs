using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Market_Data
{
    public class OrderBookEntry
    {
        #region Public Attributes

        public MDEntryType MDEntryType { get; set; }

        public MDUpdateAction MDUpdateAction { get; set; }

        public string Symbol { get; set; }

        public decimal MDEntrySize { get; set; }

        public decimal MDEntryPx { get; set; }

        #endregion

    }
}
