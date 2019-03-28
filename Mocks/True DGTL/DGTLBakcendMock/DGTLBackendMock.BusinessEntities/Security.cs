using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.BusinessEntities
{
    public class Security
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public string Description { get; set; }

        public MarketData MarketData { get; set; }

        #endregion
    }
}
