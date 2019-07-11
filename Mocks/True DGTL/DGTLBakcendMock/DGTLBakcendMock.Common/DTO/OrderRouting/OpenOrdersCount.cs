using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class OpenOrdersCount : WebSocketMessage
    {
        #region Public Attributes

        public long Time { get; set; }

        public string UserId { get; set; }

        public string Symbol { get; set; }

        public int Count { get; set; }


        #endregion
    }
}
