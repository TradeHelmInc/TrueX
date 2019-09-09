using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData.V2
{
    public class ClientLastSale : WebSocketMessageV2
    {
        #region Public Attributes

        public string UUID { get; set; }

        public long InstrumentId { get; set; }

        public decimal? LastPrice { get; set; }

        public decimal? LastSize { get; set; }

        public decimal? Change { get; set; }

        public decimal? Volume { get; set; }

        public decimal? Open { get; set; }

        public decimal? High { get; set; }

        public decimal? Low { get; set; }

        public long Timestamp { get; set; }


        #endregion
    }
}
