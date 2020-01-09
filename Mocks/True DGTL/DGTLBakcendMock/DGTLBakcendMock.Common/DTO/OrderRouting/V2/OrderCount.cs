using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientOrderCount : WebSocketMessageV2
    {
        #region Public Attributes

        public string Uuid { get; set; }

        public string UserId { get; set; }

        public int Count { get; set; }

        public string TimeStamp { get; set; }

        #endregion
    }
}
