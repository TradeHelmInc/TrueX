using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class FirmsCreditLimitUpdateResponse : WebSocketMessageV2
    {

        #region Public Static Consts

        public static char _SUCCESS_FALSE = '0';
        public static char _SUCCESS_TRUE = '1';

        #endregion

        #region Public Attributes

        public string JsonWebToken { get; set; }

        public string UUID { get; set; }

        public long FirmId { get; set; }

        private byte success;
        public byte Success
        {
            get { return success; }
            set { success = Convert.ToByte(value); }
        }

        [JsonIgnore]
        public char cSuccess { get { return Convert.ToChar(Success); } set { Success = Convert.ToByte(value); } }

        public string Message { get; set; }

        public long Time { get; set; }

        public ClientFirmRecord Firm { get; set; }


        #endregion
    }
}
