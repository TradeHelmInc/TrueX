using DGTLBackendMock.Common.DTO.OrderRouting.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.Blotters
{
    public class BlotterExecutionsList
    {
        #region Public Methods

        public ClientTradeRecord[] content { get; set; }

        public int totalCount { get; set; }

        public int pageNo { get; set; }

        public int recordPerPage { get; set; }

        #endregion
    }
}
