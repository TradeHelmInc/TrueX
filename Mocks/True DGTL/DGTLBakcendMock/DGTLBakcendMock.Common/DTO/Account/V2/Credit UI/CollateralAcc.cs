using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class CollateralAcc
    {
        public string FirmId { get; set; }

        public double PriorCollateral { get; set; }

        public double Collateral { get; set; }
    }
}
