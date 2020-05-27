using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientAccountPositionRecordBatch
    {
        public string Msg { get; set; }

        public ClientAccountPositionRecord[] messages { get; set; }
    }
}
