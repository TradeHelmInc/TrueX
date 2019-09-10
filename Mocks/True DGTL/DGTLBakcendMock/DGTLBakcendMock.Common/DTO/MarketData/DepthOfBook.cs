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

        public static char _ACTION_INSERT = 'A';

        public static char _ACTION_CHANGE = 'C';

        public static char _ACTION_REMOVE = 'R';

        #endregion

        #region Bid Or Ask

        public static char _BID_ENTRY = 'B';

        public static char _ASK_ENTRY = 'A';

        #endregion

        #endregion


        #region Public Attributes

        public string Symbol { get; set; }

        public long DepthTime { get; set; }


        private byte action;
        public byte Action 
        {
            get { return action; }
            set { action = Convert.ToByte(value); } 
        }//Action A -> Add, C->Change R-Remove, 0-> Initial Snapshot

        public char cAction { get { return Convert.ToChar(Action); } set { Action = Convert.ToByte(value); } }

        private byte bidOrAsk;
        public byte BidOrAsk 
        {
            get { return bidOrAsk; }
            set {
                bidOrAsk = Convert.ToByte(value);
            
            }
        }//B -> Bid, A -> Ask

        public char cBidOrAsk { get { return Convert.ToChar(BidOrAsk); } set { BidOrAsk = Convert.ToByte(value); } }

        public int Index { get; set; }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        #endregion

        #region Public Methods

        public bool IsBid()
        {
            return Convert.ToChar( BidOrAsk) == _BID_ENTRY;
        }

        #endregion
    }
}
