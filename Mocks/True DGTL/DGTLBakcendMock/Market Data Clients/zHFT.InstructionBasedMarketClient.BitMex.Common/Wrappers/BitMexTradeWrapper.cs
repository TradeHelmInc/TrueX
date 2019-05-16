using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.Wrappers
{
    public class BitMexTradeWrapper : Wrapper
    {
        #region Protected Attribute

        public Trade Trade { get; set; }

        public bool LastTrade { get; set; }


        #endregion

        #region Constructors


        public BitMexTradeWrapper(Trade pTrade, bool pLastTrade)
        {

            Trade = pTrade;

            LastTrade = pLastTrade;
        }


        #endregion

        #region Private Methods

        public Side GetSide()
        {
            if (Trade.side == "Buy")
                return Side.Buy;
            else if (Trade.side == "Sell")
                return Side.Sell;
            else
                return Side.Unknown;
        }

        #endregion

        #region Wrapper Methods


        public override object GetField(Main.Common.Enums.Fields field)
        {
            MarketDataFields mdField = (MarketDataFields)field;

            if (Trade == null )
                return MarketDataFields.NULL;

            if (mdField == MarketDataFields.Symbol)
                return Trade.symbol;
            else if (mdField == MarketDataFields.Trade)
                return Trade.price;
            else if (mdField == MarketDataFields.MDTradeSize)
                return Trade.size;
            else if (mdField == MarketDataFields.Timestamp)
            {
                TimeSpan elapsed = Trade.timestamp - new DateTime(1970, 1, 1);
                return Convert.ToInt64(elapsed.TotalSeconds) ;
            }
            else if (mdField == MarketDataFields.TradeId)
                return Trade.trdMatchID;
            else if (mdField == MarketDataFields.Side)
                return GetSide();
            else if (mdField == MarketDataFields.MyTrade)
                return Trade.trdMatchID != "00000000-0000-0000-0000-000000000000";
            else if (mdField == MarketDataFields.LastTrade)
                return LastTrade;
            else
                return MarketDataOrderBookEntryFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.MARKET_DATA_HISTORICAL_TRADE;
        }

        #endregion
    }
}
