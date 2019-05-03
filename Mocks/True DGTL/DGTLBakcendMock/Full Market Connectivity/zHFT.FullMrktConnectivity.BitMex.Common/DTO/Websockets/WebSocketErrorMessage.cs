using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets
{
    public class WebSocketErrorMessage : WebSocketSubscriptionEvent
    {
        #region Public Attributes

        public int status { get; set; }

        public string error { get; set; }

        public WebSocketSubscriptionRequest request { get; set; }

        public string Symbol { get; set; }


        #endregion

        #region Public Methods

        

        #endregion
    }
}
