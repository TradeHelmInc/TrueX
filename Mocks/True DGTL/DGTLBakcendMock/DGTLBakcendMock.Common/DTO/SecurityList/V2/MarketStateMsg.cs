using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList.V2
{
    public class MarketStateMsg : WebSocketMessageV2
    {
        #region Private Static Consts

        public static char _DEFAULT_EXCHANGE_ID='a';

        #endregion
        #region Public Attributes

        private byte exchangeId;
        public byte ExchangeId
        {
            get { return exchangeId; }
            set
            {
                exchangeId = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cExchangeId { get { return Convert.ToChar(ExchangeId); } set { ExchangeId = Convert.ToByte(value); } }


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
        //Market State



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
        //Market State


        public long StateTime { get; set; }

        #endregion



    }
}
