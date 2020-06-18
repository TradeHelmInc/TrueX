using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientSubscribeForSettlement : WebSocketMessageV2
    {

        #region Public Static Consts

        public static char _SUBTYPE_ALL = 'A';
        public static char _SUBTYPE_SUMMARY = 'S';
        public static char _SUBTYPE_DETAIL = 'D';

        #endregion


        #region Public Attributes

        public string Uuid { get; set; }

        public string FirmId { get; set; }

        public string UserId { get; set; }

        public string AccountId { get; set; }

        public string InstrumentId { get; set; }

        public string SettlmentDate { get; set; }

        private byte subType;
        public byte SubType
        {
            get { return subType; }
            set { subType = Convert.ToByte(value); }
        }

        [JsonIgnore]
        public char cSubType { get { return Convert.ToChar(SubType); } set { SubType = Convert.ToByte(value); } }


        #endregion
    }
}
