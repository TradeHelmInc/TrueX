using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.BusinessEntities.Market_Data
{
    public class Trade
    {
        #region Public Attributes

        public long Timestamp { get; set; }

        public string Symbol { get; set; }

        public decimal Size { get; set; }

        public decimal Price { get; set; }

        public string TradeId { get; set; }

        public bool MyTrade { get; set; }

        public bool LastTrade { get; set; }

        #endregion
    }
}
