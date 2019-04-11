using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData
{
    public class DepthOfBook : WebSocketMessage
    {
        #region Public Static Conts

        #region Actions

        public static string _ACTION_SNAPSHOT = "0";

        public static string _ACTION_INSERT = "A";

        public static string _ACTION_CHANGE = "C";

        public static string _ACTION_REMOVE = "R";

        #endregion

        #region Bid Or Ask

        public static string _BID_ENTRY = "B";

        public static string _ASK_ENTRY = "A";

        #endregion

        #endregion


        #region Public Attributes

        public string Symbol { get; set; }

        public long DepthTime { get; set; }

        public string Action { get; set; }//Action A -> Add, C->Change R-Remove, 0-> Initial Snapshot

        public string BidOrAsk { get; set; }//B -> Bid, A -> Ask

        public int Index { get; set; }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        #endregion

        #region Public Methods

        public bool IsBid()
        {
            return BidOrAsk == _BID_ENTRY;
        }

        #endregion
    }
}
