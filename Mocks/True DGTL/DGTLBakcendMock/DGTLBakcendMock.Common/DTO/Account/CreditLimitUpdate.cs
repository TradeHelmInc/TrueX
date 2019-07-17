using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account
{
    public class CreditLimitUpdate : WebSocketMessage
    {
        public long Time { get; set; }

        public string FirmId { get; set; }

        public string RouteId { get; set; }

        public double CreditLimit { get; set; }

        public decimal MaxNotional { get; set; }

        public bool Active { get; set; }

    }
}
