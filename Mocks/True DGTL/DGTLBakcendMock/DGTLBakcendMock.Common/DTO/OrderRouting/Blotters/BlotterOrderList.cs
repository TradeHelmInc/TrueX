using DGTLBackendMock.Common.DTO.OrderRouting.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.Blotters
{
    public class BlotterOrderList
    {
        #region Public Methods

        public ClientOrderRecord[] content { get; set; }

        public int totalCount { get; set; }

        public int pageNo { get; set; }

        public int recordPerPage { get; set; }

        #endregion
    }
}
