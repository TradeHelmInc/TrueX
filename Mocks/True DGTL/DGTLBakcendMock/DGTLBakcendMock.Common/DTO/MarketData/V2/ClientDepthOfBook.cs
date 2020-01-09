using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData.V2
{
    public class ClientDepthOfBook : WebSocketMessageV2
    {
        #region Public Static Conts

        #region Actions

        //public static char _ACTION_SNAPSHOT = '0';

        public static char _ACTION_INSERT = 'A';

        public static char _ACTION_UPDATE = 'U';

        public static char _ACTION_DELETE = 'D';

        #endregion

        #region Bid Or Ask

        public static char _BID_ENTRY = 'B';

        public static char _OFFER_ENTRY = 'O';

        #endregion

        #endregion


        #region Public Attributes

        public string Uuid { get; set; }

        public string InstrumentId { get; set; }

        private byte side;
        public byte Side
        {
            get { return side; }
            set
            {
                side = Convert.ToByte(value);

            }
        }//B -> Bid, O -> Offer

        public char cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        private byte action;
        public byte Action
        {
            get { return action; }
            set { action = Convert.ToByte(value); }
        }//Action A -> Add, U->Update D-Delete

        public char cAction { get { return Convert.ToChar(Action); } set { Action = Convert.ToByte(value); } }

        public long Timestamp { get; set; }

        #endregion

        #region Public Static Mehtods

        public static char TranslateOldAction(char oldAction)
        {
            if (oldAction == DepthOfBook._ACTION_CHANGE)
                return ClientDepthOfBook._ACTION_UPDATE;
            else if (oldAction == DepthOfBook._ACTION_INSERT)
                return ClientDepthOfBook._ACTION_INSERT;
            else if (oldAction == DepthOfBook._ACTION_REMOVE)
                return ClientDepthOfBook._ACTION_DELETE;
            else
                throw new Exception(string.Format("Unknown old depth of book action {0}", oldAction));
        
        
        }


        public static char TranslateOldSide(char oldSide)
        {
            if (oldSide == DepthOfBook._ASK_ENTRY)
                return ClientDepthOfBook._OFFER_ENTRY;
            else if (oldSide == DepthOfBook._BID_ENTRY)
                return ClientDepthOfBook._BID_ENTRY;
         
            else
                throw new Exception(string.Format("Unknown old depth of book side {0}", oldSide));


        }

        #endregion
    }
}
