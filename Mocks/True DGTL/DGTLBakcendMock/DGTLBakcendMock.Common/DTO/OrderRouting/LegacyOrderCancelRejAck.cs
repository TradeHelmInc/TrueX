using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderCancelRejAck : LegacyOrderExecutionReport
    {
        #region Public Attributes

        public string OrderRejectReason { get; set; }

        #endregion
    }
}
