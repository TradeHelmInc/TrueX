using DGTLBackendMock.Common.DTO.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData
{
    public class Quote : WebSocketMessage
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public decimal? Bid { get; set; }

        public decimal? BidSize { get; set; }

        public decimal? Ask { get; set; }

        public decimal? AskSize { get; set; }

        public decimal? MidPoint { get; set; }

        #endregion

        #region Public Methods

        protected decimal TruncateDecimal(decimal value, int precision)
        {
            decimal step = (decimal)Math.Pow(10, precision);
            decimal tmp = Math.Truncate(step * value);
            return tmp / step;
        }

        public void RefreshMidPoint(decimal MinPriceIncrement)
        {
            if (Ask.HasValue && Bid.HasValue)
            {
                decimal midPoint = (Ask.Value + Bid.Value) / 2;

                string strMinPriceIncrement = MinPriceIncrement.ToString();

                int countDecimalsMinPriceIncr = 0;
                if (strMinPriceIncrement.Split(new string[] { ".", "," }, StringSplitOptions.RemoveEmptyEntries).Length > 1)
                    countDecimalsMinPriceIncr = strMinPriceIncrement.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries)[1].Length;

                string strMidPoint = midPoint.ToString();
                if (strMidPoint.Split(new string[] { ".", "," }, StringSplitOptions.RemoveEmptyEntries).Length > 1)
                {
                    if (strMidPoint.Split(new string[] { ".", "," }, StringSplitOptions.RemoveEmptyEntries)[1].Length > countDecimalsMinPriceIncr)
                    {
                        char nPlusOneDec = strMidPoint.Split(new string[] { ".", "," }, StringSplitOptions.RemoveEmptyEntries)[1][countDecimalsMinPriceIncr];

                        if (nPlusOneDec == '5')
                            MidPoint = TruncateDecimal(midPoint, countDecimalsMinPriceIncr + 1);
                        else
                            MidPoint = Convert.ToDecimal(Math.Round(midPoint, countDecimalsMinPriceIncr));
                    }
                    else
                        MidPoint = Convert.ToDecimal(Math.Round(midPoint, countDecimalsMinPriceIncr));
                }
                else
                    MidPoint = midPoint;
            }
            else
                MidPoint = null;
        
        }

        #endregion
    }
}
