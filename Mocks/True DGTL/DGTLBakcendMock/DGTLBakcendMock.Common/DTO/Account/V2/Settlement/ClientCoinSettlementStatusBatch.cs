using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Settlement
{
    public class ClientCoinSettlementStatusBatch
    {
        public string Msg { get; set; }

        public ClientCoinSettlementStatus[] messages { get; set; }
    }
}
