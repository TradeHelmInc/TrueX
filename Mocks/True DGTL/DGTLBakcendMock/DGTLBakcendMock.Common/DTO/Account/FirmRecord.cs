using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account
{
    public class FirmRecord
    {
        #region Public Attributes

        public string FirmId { get; set; }

        public string FirmName { get; set; }

        public string FirmType { get; set; }

        public string ShortName { get; set; }

        public string Status { get; set; }

        public string FeeGroup { get; set; }

        #endregion
    }
}
