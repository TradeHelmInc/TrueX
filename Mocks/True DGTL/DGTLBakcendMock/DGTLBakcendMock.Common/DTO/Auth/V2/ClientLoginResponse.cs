using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ClientLoginResponse : WebSocketMessageV2
    {

        #region Public Static Consts

        public static char _STATUS_OK = '1';
        public static char _STATUS_FAILED = '0';

        #endregion

        #region Public Methods

        public string UserId { get; set; }

        public string JsonWebToken { get; set; }

        public string UUID { get; set; }

        //public bool Status { get; set; }

        private byte success;
        public byte Success
        {
            get { return success; }
            set
            {
                success = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cSuccess { get { return Convert.ToChar(Success); } set { Success = Convert.ToByte(value); } }

        public long Time { get; set; }

        public string Message { get; set; }


        #endregion
    }
}
