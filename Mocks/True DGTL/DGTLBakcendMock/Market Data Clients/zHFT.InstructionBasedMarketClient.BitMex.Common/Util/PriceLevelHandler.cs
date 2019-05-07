using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.Util
{
    public class PriceLevelHandler
    {
        #region Public Attributes

        public Dictionary<string, Dictionary<long, decimal>> PriceLevels { get; set; }


        #endregion

        #region Constructors

        public PriceLevelHandler()
        {
            PriceLevels = new Dictionary<string, Dictionary<long, decimal>>();
        }

        #endregion

        #region Public Methods


        public Dictionary<long, decimal> GetPriceLevelDict(string symbol)
        {
            if (!PriceLevels.ContainsKey(symbol))
                PriceLevels.Add(symbol, new Dictionary<long, decimal>());

            return PriceLevels[symbol];
        }

        #endregion
    }
}
