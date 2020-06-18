using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Temp.Positions
{
    public class NetPositionDTO
    {
        #region Public Attributes

        public string FirmId { get; set; }

        public string AssetClass { get; set; }

        public string Symbol { get; set; }

        public DateTime MaturityDate { get; set; }

        public double NetContracts { get; set; }

        #endregion
    }
}
