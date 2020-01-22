using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList.V2
{
    public class ClientUserRecord : WebSocketMessageV2
    {
        #region Public Attributes

        public string UserId { get; set; }

        public long FirmId { get; set; }

        public string GroupId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool IsAdmin { get; set; }

        private byte connectionType;
        public byte ConnectionType
        {
            get { return connectionType; }
            set
            {
                connectionType = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cConnectionType { get { return Convert.ToChar(ConnectionType); } set { ConnectionType = Convert.ToByte(value); } }


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


        public string Email { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string PostalCode { get; set; }

        public string DefaultAccount { get; set; }

     
        #endregion
    }
}
