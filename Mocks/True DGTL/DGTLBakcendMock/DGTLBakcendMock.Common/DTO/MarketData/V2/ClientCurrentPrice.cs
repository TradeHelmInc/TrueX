using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData.V2
{
    public class ClientCurrentPrice : WebSocketMessageV2
    {
        public string InstrumentId { get; set; }

        public string Uuid { get; set; }

        public decimal? CurrentPrice { get; set; }

        public decimal? ChangePercentage { get; set; }

        public long Timestamp { get; set; }
    }
}
