using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class ClientFirmRecord
    {
        #region Public Attributes

        public long Id { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public CreditLimit[] CreditLimit { get; set; }

        public ClientAccountRecord[] Accounts { get; set; }

        #endregion
    }
}
