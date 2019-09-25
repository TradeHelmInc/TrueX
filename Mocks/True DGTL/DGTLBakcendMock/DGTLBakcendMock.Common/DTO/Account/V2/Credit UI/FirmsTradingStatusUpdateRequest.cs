using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class FirmsTradingStatusUpdateRequest : WebSocketMessageV2
    {
        #region Private Static Consts

        public static char _STATUS_TRADING = 'T';

        public static char _STATUS_SUSPENDED = 'S';

        #endregion

        #region Pubic Attributes

        public string JsonWebToken { get; set; }

        public string Uuid { get; set; }

        public long FirmId { get; set; }

        private byte tradingStatus;
        public byte TradingStatus
        {
            get { return tradingStatus; }
            set
            {
                tradingStatus = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cTradingStatus { get { return Convert.ToChar(TradingStatus); } set { TradingStatus = Convert.ToByte(value); } }

        public long Time { get; set; }

        #endregion
    }
}
