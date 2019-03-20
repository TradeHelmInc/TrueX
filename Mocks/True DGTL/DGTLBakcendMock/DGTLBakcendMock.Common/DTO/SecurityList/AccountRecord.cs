using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList
{
    public class AccountRecord : WebSocketMessage
    {
        public string AccountKey { get; set; }

        public string UniqueId { get; set; }

        public string RouteId { get; set; }

        public string EPNickName { get; set; }

        public string CFNickName { get; set; }

        public decimal CreditLimit { get; set; }

        public decimal MaxNotional { get; set; }

        public bool Active { get; set; }
    }
}
