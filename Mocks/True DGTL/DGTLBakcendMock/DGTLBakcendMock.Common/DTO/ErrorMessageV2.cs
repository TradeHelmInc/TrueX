using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO
{
    public class ErrorMessageV2 : WebSocketMessageV2
    {
        public string Error { get; set; }
    }
}
