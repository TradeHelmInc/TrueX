using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Websockets.Events;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.Wrappers
{
    public class BitmexMarketDataOrderBookEntryWrapper : Wrapper
    {
        #region Public Attributes

        public string action { get; set; }

        public OrderBookEntry OrderBookEntry { get; set; }

        public bool IsBid { get; set; }

        #endregion

        #region Constructor

        public BitmexMarketDataOrderBookEntryWrapper(string pAction, OrderBookEntry pOrderBookEntry, bool isBid)
        {
            action = pAction;

            OrderBookEntry = pOrderBookEntry;

            IsBid = isBid;
        }


        #endregion

        #region Private Methods


        private MDUpdateAction GetMDUpdateAction()
        {
            if (action == "insert")
                return MDUpdateAction.New;
            if (action == "partial")
                return MDUpdateAction.New;
            else if (action == "update")
                return MDUpdateAction.Change;
            if (action == "delete")
                return MDUpdateAction.Delete;
            else
                return MDUpdateAction.New;
        }

        #endregion

        #region Wrapper Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            MarketDataOrderBookEntryFields mdField = (MarketDataOrderBookEntryFields)field;

            if (OrderBookEntry == null || action ==null)
                return MarketDataOrderBookEntryFields.NULL;

            if (mdField == MarketDataOrderBookEntryFields.MDUpdateAction)
                return GetMDUpdateAction();
            else if (mdField == MarketDataOrderBookEntryFields.Symbol)
                return OrderBookEntry.symbol;
            else if (mdField == MarketDataOrderBookEntryFields.MDEntrySize)
                return OrderBookEntry.size;
            else if (mdField == MarketDataOrderBookEntryFields.MDEntryPx)
                return OrderBookEntry.price;
            else if (mdField == MarketDataOrderBookEntryFields.MDEntryType)
                return IsBid ? MDEntryType.Bid : MDEntryType.Ask;
            else
                return MarketDataOrderBookEntryFields.NULL;
        }

        public override Actions GetAction()
        {
            return Actions.MARKET_DATA_ORDER_BOOK_ENTRY;
        }

        #endregion
    }
}
