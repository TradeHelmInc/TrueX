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

        public static char _STATE_INACTIVE = 'H';

        public static char _STATE_UNKNOWN = 'U';

        public static char _REASON_CODE_2 = '2';

        public static char _REASON_CODE_3 = '3';

        #endregion


        #region Protected Attributes

        public int ExchangeId { get; set; }

        public int InstrumentId { get; set; }

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
    }
}
