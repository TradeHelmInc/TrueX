using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.SecurityList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Temp.Csv
{
    public class PositionsCSVDTO
    {
        public Dictionary<string, List<ClientPosition>> FirmPositions { get; set; }

        public Dictionary<string, double> TodayDSPs { get; set; }

        public Dictionary<string, double> PrevDSPs { get; set; }

        #region Protected Methods

        private DailySettlementPrice[] GetDSPs(Dictionary<string, double> DSPDicts)
        {
            List<DailySettlementPrice> DSPs = new List<DailySettlementPrice>();
            foreach (string symbol in DSPDicts.Keys)
            {
                DailySettlementPrice DSP = new DailySettlementPrice();


                DSP.Msg = "DailySettlementPrice";
                DSP.Symbol = symbol;
                DSP.Price = DSPDicts[symbol];
                DSPs.Add(DSP);
            }

            return DSPs.ToArray();
        }


        public DailySettlementPrice[] GetTodayDailySettlementPrice()
        {
            return GetDSPs(TodayDSPs);

        }

        public DailySettlementPrice[] GetYesterdayDailySettlementPrice()
        {
            return GetDSPs(PrevDSPs);

        }

        public SecurityMasterRecord[] GetSecurityMasterRecords()
        {
            List<SecurityMasterRecord> securities = new List<SecurityMasterRecord>();
            int i = 1;
            foreach (string firm in FirmPositions.Keys)
            {
                foreach (ClientPosition pos in FirmPositions[firm])
                {
                    if (!securities.Any(x => x.Symbol == pos.Symbol))
                    {
                        SecurityMasterRecord security = new SecurityMasterRecord();
                        security.Symbol = pos.Symbol;
                        security.CurrencyPair = "XBT-USD";
                        security.InstrumentId = i;
                        security.MaturityDate = security.GetMaturityDateFromSymbol();

                        i++;

                        securities.Add(security);
                    }
                }
            }
            return securities.ToArray();
        }

        #endregion

    }
}
