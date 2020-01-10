using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList.V2
{
    public class ClientInstrumentState : WebSocketMessageV2
    {
        #region Public Static Consts

        public static char _STATE_HALT = 'H';

        public static char _STATE_OPEN = 'O';

        public static char _STATE_CLOSE = 'C';

        public static char _STATE_INACTIVE = 'I';

        public static char _STATE_UNKNOWN = 'U';

        public static char _REASON_CODE_2 = '2';

        public static char _REASON_CODE_3 = '3';

        public static string _OLD_STATUS_ACTIVE = "84";
        public static string _OLD_STATUS_HALTED = "H";
        public static string _OLD_STATUS_COSED = "C";
        public static string _OLD_STATUS_INACTIVE = "I";

        #endregion


        #region Protected Attributes

        public string ExchangeId { get; set; }

        public string InstrumentId { get; set; }

        private byte state;
        public byte State
        {
            get { return state; }
            set
            {
                state = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cState { get { return Convert.ToChar(State); } set { State = Convert.ToByte(value); } }


        private byte reasonCode;
        public byte ReasonCode
        {
            get { return reasonCode; }
            set
            {
                reasonCode = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cReasonCode { get { return Convert.ToChar(ReasonCode); } set { ReasonCode = Convert.ToByte(value); } }

        public double? TriggerPrice { get; set; }

        #endregion

        #region Public Static Methods

        public static char GetSecurityStatus(string prevStatus)
        {
            if (prevStatus == _OLD_STATUS_ACTIVE)
                return _STATE_OPEN;
            else if (prevStatus == _OLD_STATUS_COSED)
                return _STATE_CLOSE;
            else if (prevStatus == _OLD_STATUS_HALTED)
                return _STATE_HALT;
            else if (prevStatus == _OLD_STATUS_INACTIVE)
                return _STATE_INACTIVE;
            else
                return _STATE_UNKNOWN;
        }

        #endregion
    }
}
