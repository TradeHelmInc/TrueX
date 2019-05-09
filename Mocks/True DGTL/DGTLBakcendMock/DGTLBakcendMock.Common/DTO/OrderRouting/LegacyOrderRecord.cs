using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderRecord : WebSocketMessage
    {

        #region Public static Consts

        public static string _ORD_STATUS_NEW = "O";

        public static string _ORD_sTATUS_REJECTED = "R";

        public static string _ORD_STATUS_CANCELED = "C";

        public static string _ORD_STATUS_FILLED = "F";

        public static string _ORD_sTATUS_PARTIALLY_FILLED = "P";

        public static string _ORD_STATUS_EXPIRED = "E";

        #endregion

        #region Public Attributes

        public string OrderId { get; set; }

        public string UserId { get; set; }

        public string ClientOrderId { get; set; }

        public string InstrumentId { get; set; }

        public string Side { get; set; }

        public double OrdQty { get; set; }

        public double? Price { get; set; }

        public double LvsQty { get; set; }

        public double FillQty { get; set; }

        public string Status { get; set; }

        #endregion
    }
}
