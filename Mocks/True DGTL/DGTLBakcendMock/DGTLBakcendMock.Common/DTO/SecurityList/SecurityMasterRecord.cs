using DGTLBackendMock.Common.DTO.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList
{
    public class SecurityMasterRecord : SubscriptionMsg
    {
        public string Description { get; set; }

        public string SecurityType { get; set; }

        public string ProductType { get; set; }

        public string AssetClass { get; set; }

        public decimal MinPrice { get; set; }

        public decimal MaxPrice { get; set; }

        public decimal MinPriceIncrement { get; set; }

        public string MaturityDate { get; set; }

        public string Status { get; set; }
    }
}
