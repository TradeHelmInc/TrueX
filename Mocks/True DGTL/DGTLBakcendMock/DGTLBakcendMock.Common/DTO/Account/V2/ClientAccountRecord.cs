using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientAccountRecord : WebSocketMessageV2
    {
        #region Private Static Consts


        public static char _STATUS_ACTIVE = 'A';
        public static char _STATUS_ON_BOARDING = 'O';
        public static char _STATUS_DELETED = 'D';

        public static char _DEFAULT_USER_TYPE = 'a';

        public static char _CTI_PROP = 'P';

        public static char _CTI_MEMB = 'M';

        public static char _CTI_OTHER = 'O';

        #endregion


        #region Public Attributes

        public int Sender { get; set; }

        public int Time { get; set; }

        public string AccountId { get; set; }

        public string FirmId { get; set; }

        public string SettlementFirmId { get; set; }

        public string AccountName { get; set; }

        public string AccountAlias { get; set; }

        public int AccountType { get; set; }

        public string RegistrationType { get; set; }

        public string AccountNumber { get; set; }

        public string WalletAddress { get; set; }

        public bool IsSuspense { get; set; }

        public bool UsDomicile { get; set; }

        public string Currency { get; set; }

        public string Lei { get; set; }

        public int UpdatedAt { get; set; }

        public int CreatedAt { get; set; }

        public string LastUpdatedBy { get; set; }
     

        private byte status;
        public byte Status
        {
            get { return status; }
            set
            {
                status = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cStatus { get { return Convert.ToChar(Status); } set { Status = Convert.ToByte(value); } }



        private byte userType;
        public byte UserType
        {
            get { return userType; }
            set
            {
                userType = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cUserType { get { return Convert.ToChar(UserType); } set { UserType = Convert.ToByte(value); } }


        private byte cti;
        public byte Cti
        {
            get { return cti; }
            set
            {
                cti = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cCti { get { return Convert.ToChar(Cti); } set { Cti = Convert.ToByte(value); } }


        #endregion
    }
}
