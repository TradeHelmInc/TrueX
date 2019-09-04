using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList.V2
{
    public class ClientMarketStateBatch
    {
        public string Msg { get; set; }

        public ClientMarketState[] messages { get; set; }
    }
}
