using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList
{
    public class SecurityStatus : WebSocketMessage
    {

        #region Public Static Consts

        public static char _SEC_STATUS_TRADING = 'T';
        public static char _SEC_STATUS_HALTING = 'H';

        #endregion

        #region Public Attributes

        public string Symbol { get; set; }

        private byte status;
        public byte Status
        {
            get { return status; }
            set
            {
                status = Convert.ToByte(value);

            }
        }//T: Trading, H: Halt

        [JsonIgnore]
        public char cStatus { get { return Convert.ToChar(Status); } set { Status = Convert.ToByte(value); } }


        public decimal? ReferencePrice { get; set; }

        public bool IsOrdersPostingEnabled { get; set; }

        #endregion

    }
}
