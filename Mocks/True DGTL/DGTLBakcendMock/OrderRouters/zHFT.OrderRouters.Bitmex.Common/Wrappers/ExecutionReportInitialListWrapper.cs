using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.OrderRouters.Bitmex.Common.DTO;

namespace zHFT.OrderRouters.Bitmex.Common.Wrappers
{
    public class ExecutionReportInitialListWrapper : ExecutionReportWrapper
    {
        #region Public Attributes

        public bool LastReport { get; set; }


        #endregion

        #region Constructors

        public ExecutionReportInitialListWrapper(zHFT.OrderRouters.Bitmex.BusinessEntities.ExecutionReport pExecutionReport,
                                                 zHFT.OrderRouters.Bitmex.BusinessEntities.Order pOrder, bool pLastReport)
            : base(pExecutionReport, pOrder)
        {
            LastReport = pLastReport;
        }

        #endregion


        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (xrField == ExecutionReportFields.LastReport)
                return LastReport;
            else

                return base.GetField(field);
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.EXECUTION_REPORT_INITIAL_LIST;
        }
    }
}
