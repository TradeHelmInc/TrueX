using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientAccountPositionRecord : WebSocketMessageV2
    {
        #region Public Attributes

        public string Uuid { get; set; }

        public string FirmId { get; set; }

        public string AccountId { get; set; }

        public int InstrumentId { get; set; }

        public string Contract { get; set; }

        public int PriorDayNetPosition { get; set; }

        public double PriorDayDSP { get; set; }

        public int CurrentNetPosition { get; set; }

        public double CurrentDayDSP { get; set; }

        public double Change { get; set; }

        public double ProfitAndLoss { get; set; }

        public double? CurrentPrice { get; set; }

        public long TimeStamp { get; set; }

        #endregion
    }
}
