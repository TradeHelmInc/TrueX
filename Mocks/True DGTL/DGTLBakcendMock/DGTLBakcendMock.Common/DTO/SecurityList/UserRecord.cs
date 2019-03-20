using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList
{
    public class UserRecord : WebSocketMessage
    {
        public string UserKey { get; set; }

        public string UserId { get; set; }

        public string FirmId { get; set; }

        public string DeskId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string NickName { get; set; }

        public string UserType { get; set; }

        public string AccountType { get; set; }

        public string Status { get; set; }

        public string Email { get; set; }

        public string GatewayType { get; set; }

    }
}
