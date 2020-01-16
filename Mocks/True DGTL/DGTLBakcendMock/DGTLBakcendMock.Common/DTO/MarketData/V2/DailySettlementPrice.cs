using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.MarketData.V2
{
    public class ClientDSP : WebSocketMessageV2
    {
        #region Public Attributes

        public string Uuid { get; set; }

        public string InstrumentId { get; set; }

        public string InstrumentName { get; set; }

        public decimal? DailySettlementPrice { get; set; }

        public int CalculationDate { get; set; }

        public string CalculationTime { get; set; }

        public int Sender { get; set; }

        public string Time { get; set; }

        #endregion
    }
}
