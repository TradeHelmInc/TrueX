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

        public string UUID { get; set; }

        public long InstrumentId { get; set; }

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


        #endregion
    }
}
