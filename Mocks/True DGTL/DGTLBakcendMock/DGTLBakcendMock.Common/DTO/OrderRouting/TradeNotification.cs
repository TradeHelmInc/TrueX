using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class TradeNotification : WebSocketMessage
    {
        #region Public Attributes

        public long Time { get; set; }

        public string UserId { get; set; }

        public string Symbol { get; set; }

        public double Price { get; set; }

        public double Size { get; set; }


        #endregion
    }
}
