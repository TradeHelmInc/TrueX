using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace DGTLBackendMock.Common.DTO.MarketData
{
    public class LegacyTradeHistory : WebSocketMessage
    {

        #region Public Consts

        public static char _SIDE_BUY = 'B';
        public static char _SIDE_SELL = 'S';
        public static char _SIDE_NONE = 'N';

        #endregion

        #region Public Attributes

        public string TradeId { get; set; }

        public string Symbol { get; set; }

        public double TradePrice { get; set; }

        public double TradeQuantity { get; set; }

        public bool MyTrade { get { return Convert.ToChar(myside) != _SIDE_NONE; } set { } }

        private byte myside;
        public byte MySide
        {
            get { return myside; }
            set { myside = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell, N->None

        [JsonIgnore]
        public char cMySide { get { return Convert.ToChar(MySide); } set { MySide = Convert.ToByte(value); } }

        public long TradeTimeStamp { get; set; }

        #endregion

        #region Public Static Methods


        public static char GetMySide(Side side)
        {

            if (side == Side.Buy)
                return LegacyTradeHistory._SIDE_BUY;
            if (side == Side.Sell)
                return LegacyTradeHistory._SIDE_SELL;
            else
                return LegacyTradeHistory._SIDE_NONE;

        }

        #endregion
    }
}
