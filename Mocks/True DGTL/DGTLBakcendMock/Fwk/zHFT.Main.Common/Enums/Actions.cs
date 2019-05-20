using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public enum Actions
    {
        MARKET_DATA,
        MARKET_DATA_ERROR,
        MARKET_DATA_ORDER_BOOK_ENTRY,
        MARKET_DATA_REQUEST,
        MARKET_DATA_TRADES_REQUEST,
        MARKET_DATA_TRADE_LIST_REQUEST,
        MARKET_DATA_HISTORICAL_TRADE,
        MARKET_DATA_TRADES,
        MARKET_DATA_QUOTES_REQUEST,
        MARKET_DATA_QUOTES,
        MARKET_DATA_ORDERBOOK_REQUEST,
        ORDER_BOOK,
        OFFER,
        EXECUTION_REPORT,
        EXECUTION_REPORT_INITIAL_LIST,
        NEW_POSITION,
        SECURITY,
        NEW_POSITION_CANCELED,
        NEW_ORDER,
        UPDATE_ORDER,
        CANCEL_ORDER,
        CANCEL_ALL_ORDERS,
        CANCEL_POSITION,
        CANCEL_ALL_POSITIONS,
        SECURITY_LIST,
        SECURITY_LIST_REQUEST,
        ORDER_CANCEL_REJECT,
        BUSINESS_MESSAGE_REJECT,
        ORDER_LIST
    }
}
