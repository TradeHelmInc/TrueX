using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.OrderRouting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;

namespace DGTLBackendMock.Common.Util
{
    public class AttributeConverter
    {
        #region Public Static Methods


        public static char GetBidOrAsk(MDEntryType MDEntryType)
        {
            if (MDEntryType == MDEntryType.Bid)
                return DepthOfBook._BID_ENTRY;
            else if (MDEntryType == MDEntryType.Ask)
                return DepthOfBook._ASK_ENTRY;
            else
                throw new Exception(string.Format("Unknown MDEntryType {0}", MDEntryType));
        }

        public static char GetAction(MDUpdateAction MDUpdateAction)
        {

            if (MDUpdateAction == MDUpdateAction.New)
            {
                return DepthOfBook._ACTION_INSERT;
            }
            else if (MDUpdateAction == MDUpdateAction.Change)
            {
                return DepthOfBook._ACTION_CHANGE;
            }
            else if (MDUpdateAction == MDUpdateAction.Delete)
            {
                return DepthOfBook._ACTION_REMOVE;
            }
            else
                throw new Exception(string.Format("Unknown MDUpdateAction {0}", MDUpdateAction));
        }

        public static char GetExecReportStatus(ExecutionReport execReport)
        {
            if (execReport.OrdStatus == OrdStatus.New)
                return LegacyOrderAck._STATUS_OPEN;
            else if (execReport.OrdStatus == OrdStatus.Canceled)
                return LegacyOrderAck._STATUS_CANCELLED;
            else if (execReport.OrdStatus == OrdStatus.Rejected)
                return LegacyOrderAck._STATUS_REJECTED;
            else if (execReport.OrdStatus == OrdStatus.PartiallyFilled)
                return LegacyOrderAck._STATUS_OPEN;
            else if (execReport.OrdStatus == OrdStatus.Filled)
                return LegacyOrderAck._STATUS_FULL_FILL;
            else if (execReport.OrdStatus == OrdStatus.Expired)
                return LegacyOrderAck._STATUS_EXPIRED;
            else
                return LegacyOrderAck._STATUS_UNKNOWN;

        }

        #endregion
    }
}
