using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Subscription.V2
{
    public class Subscribe : WebSocketMessageV2
    {

        #region Public Static Consts

        public static char _ACTION_SUBSCRIBE = 'S';
        public static char _ACTION_UNSUBSCRIBE = 'U';

        #endregion

        #region Public Methods

        public string Uuid { get; set; }

        public string JsonWebToken { get; set; }

        private byte action;
        public byte Action
        {
            get { return action; }
            set
            {
                action = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cAction { get { return Convert.ToChar(Action); } set { Action = Convert.ToByte(value); } }
        //Action

        public string Service { get; set; }

        public string ServiceKey { get; set; }

        #endregion
    }
}
