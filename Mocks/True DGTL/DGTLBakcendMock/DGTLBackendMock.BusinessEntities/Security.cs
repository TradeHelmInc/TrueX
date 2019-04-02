using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.BusinessEntities
{
    public class Security
    {
        #region Public Static Consts

        public static string _SPOT = "SPOT";
        public static string _NDF = "NDF";
        public static string _SWAP = "SWP";

        #endregion

        #region Public Attributes

        public string Symbol { get; set; }

        public string Underlying { get; set; }

        public string Description { get; set; }

        public string SecurityType { get; set; }

        public MarketData MarketData { get; set; }

        #endregion
    }
}
