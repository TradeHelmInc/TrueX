using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData
{
    public class LastSale : WebSocketMessage
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public decimal? FirstPrice { get; set; }

        public decimal? LastPrice { get; set; }

        public decimal? LastShares { get; set; }

        public long? LastTime { get; set; }

        public decimal? Change { get; set; }

        public decimal? DiffPrevDay { get; set; }

        public decimal Volume { get; set; }//In this case, if there is no volume, volume is 0.

        public decimal? High { get; set; }

        public decimal? Low { get; set; }

        public decimal? Open { get; set; }

        public DateTime? GetLastTime()
        {
            if (LastTime.HasValue)
            {
                DateTime date = new DateTime(1970, 1, 1);
                date = date.AddMilliseconds(LastTime.Value);
                return date;
            }
            else
                return null;
        
        
        }

        #endregion
    }
}
