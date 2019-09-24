using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class OrderCount : WebSocketMessageV2
    {
        #region Public Attributes

        public string UUID { get; set; }

        public string UserId { get; set; }

        public int Count { get; set; }

        public long TimeStamp { get; set; }

        #endregion
    }
}
