using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.DTO
{
    public class Quote
    {
        #region Public Attributes

        public DateTime timestamp { get; set; }

        public string symbol { get; set; }

        public double? bidSize { get; set; }

        public double? bidPrice { get; set; }

        public double? askPrice { get; set; }

        public double? askSize { get; set; }

        #endregion
    }
}
