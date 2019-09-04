using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList.V2
{
    public class ClientInstrumentBatch 
    {
        public string Msg { get; set; }

        public ClientInstrument[] messages { get; set; }
    }
}
