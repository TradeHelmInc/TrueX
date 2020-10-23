using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Temp.Margin
{
    public class MarginCollateralDTO
    {

        #region Public Attributes

        public string Firm { get; set; }

        public double Collateral { get; set; }

        public double? PendingCollateral { get; set; }

        public double PriorIM { get; set; }

        public double? IMToday { get; set; }

        public double? IMRequirement { get; set; }

        public double? VMRequirement { get; set; }

        public string MarginCall { get; set; }

        #endregion

    }
}
