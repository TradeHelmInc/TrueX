using DGTLBackendMock.Common.DTO.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList
{


    public class SecurityMasterRecord : WebSocketMessage
    {
        public int InstrumentId { get; set; }

        public string Symbol { get; set; }

        public string Description { get; set; }

        public string SecurityType { get; set; }

        public string ProductType { get; set; }

        public string AssetClass { get; set; }

        public string CurrencyPair { get; set; }

        public decimal MinPrice { get; set; }

        public decimal MaxPrice { get; set; }

        public decimal MinPriceIncrement { get; set; }

        public decimal MinSize { get; set; }

        public decimal MaxSize { get; set; }

        public decimal LotSize { get; set; }

        public string Platform { get; set; }

        public string MaturityDate { get; set; }

        public string Status { get; set; }
    }
}
