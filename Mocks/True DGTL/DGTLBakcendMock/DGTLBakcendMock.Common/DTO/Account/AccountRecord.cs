using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account
{
    public class AccountRecord : WebSocketMessage
    {
        #region Public Attributes

        public string AccountKey { get; set; }

        public string UniqueId { get; set; }

        public string ClearingFirmId { get; set; }

        public string CFirmName { get; set; }

        public string CFSortName { get; set; }

        public string AccountId { get; set; }

        public string EPFirmId { get; set; }

        public string RouteId { get; set; }

        public string EPNickName { get; set; }

        public string CFNickName { get; set; }

        public double CreditLimit { get; set; }

        public decimal MaxNotional { get; set; }

        public bool Active { get; set; }

        #endregion
    }
}
