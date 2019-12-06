using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientMarketActivity : WebSocketMessageV2
    {

        public static char _SIDE_BUY = 'B';
        public static char _SIDE_SELL = 'S';

        #region Public Attributes

        public string Msg { get; set; }

        public string Uuid { get; set; }

        public long InstrumentId { get; set; }

        public double LastPrice { get; set; }

        public double LastSize { get; set; }

        private byte? myside;
        public byte? MySide
        {
            get { return myside; }
            set { myside = Convert.ToByte(value); }
        }//Side B -> Buy, S->Sell

        [JsonIgnore]
        public char? cMySide { get { return Convert.ToChar(MySide); } set { MySide = Convert.ToByte(value); } } 

        public long TradeId { get; set; }

        public long TimeStamp { get; set; }


        #endregion
    }
}
