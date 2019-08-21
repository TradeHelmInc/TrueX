using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList.V2
{
    public class InstrumentMsg : WebSocketMessageV2
    {
        public int ExchangeId { get; set; }

        public string InstrumentId { get; set; }

        public string InstrumentName { get; set; }

        public double MinLotSize { get; set; }

        public double MaxLotSize { get; set; }

        public string InstrumentDate { get; set; } //format YYYYMMDD 

        public bool Test { get; set; }

        public string Description { get; set; }

        public long UpdatedAt { get; set; }

        public long CreatedAt { get; set; }

        public string LastUpdatedBy { get; set; }

        public string Currency1 { get; set; }

        public string Currency2 { get; set; }

        //missing CurrencyPair,AssetClass,MaxPrice,MinPrice,MinPriceIncrement,
    }
}
