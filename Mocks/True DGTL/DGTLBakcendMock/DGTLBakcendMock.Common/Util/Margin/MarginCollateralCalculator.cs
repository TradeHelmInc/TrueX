using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Temp.Margin;
using DGTLBackendMock.Common.DTO.Temp.Positions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsShared.Logging;

namespace DGTLBackendMock.Common.Util.Margin
{
    public class MarginCollateralCalculator : BaseCreditLogic
    {
        #region Protected Attributes

        public DailySettlementPrice[] TodayDailySettlementPrices { get; set; }

        #endregion

        #region Constructor

        public MarginCollateralCalculator(SecurityMasterRecord[] pSecurities, DailySettlementPrice[] pTodayDSPs, DailySettlementPrice[] pPrevDSPs,
                                         DGTLBackendMock.Common.DTO.Auth.Config pConfig, ILogSource pLogger)
        {
            SecurityMasterRecords = pSecurities;

            DailySettlementPrices = pPrevDSPs;

            TodayDailySettlementPrices = pTodayDSPs;

            Config = pConfig;

            Logger = pLogger;
        }


        #endregion

        #region Protected Methods

        protected void FillPositions(List<ClientPosition> todayPositions)
        {
            foreach (SecurityMasterRecord sec in SecurityMasterRecords)
            {

                if (!todayPositions.Any(x => x.Symbol == sec.Symbol))
                {
                    todayPositions.Add(new ClientPosition()
                    {
                        Symbol = sec.Symbol,
                        Contracts = 0,
                        MarginFunded = false,
                        Msg = "ClientPosition",
                    });
                }
            }
        }

        protected List<ClientPosition> GetPreviousDayPositions(List<ClientPosition> todayPositions, List<TradeDTO> todayTrades)
        {
            List<ClientPosition> prevPositions = new List<ClientPosition>();

            todayPositions.ForEach(x => prevPositions.Add(x.Clone()));

            foreach (ClientPosition pos in prevPositions)
            {
                double netChange = 0;

                todayTrades.ForEach(x => netChange += x.GetSignedExecutionSize());

                pos.Contracts += netChange;
            
            }

            return prevPositions;
        }


        protected double? GetIMRequirement(string firmId, double priorIM, double IMToday, List<ClientPosition> todayPositions)
        {

            if (DailySettlementPrices.Length == TodayDailySettlementPrices.Length)
            {


                double todayMargin = GetBaseMargin(firmId, todayPositions.ToArray(), DSPsToUse: TodayDailySettlementPrices);

                return todayMargin - (priorIM + IMToday);
            }
            else
                return null;
        }

        protected double? GetVMRequirement(List<ClientPosition> prevPositions, List<TradeDTO> todayTrades)
        {
            if (DailySettlementPrices.Length == TodayDailySettlementPrices.Length)
            {

                double vmReqs = 0;
                foreach (ClientPosition prevPos in prevPositions)
                {
                    DailySettlementPrice prevDSP = DailySettlementPrices.Where(x => x.Symbol == prevPos.Symbol).FirstOrDefault();
                    DailySettlementPrice todayDSP = TodayDailySettlementPrices.Where(x => x.Symbol == prevPos.Symbol).FirstOrDefault();

                    if (prevDSP == null || !prevDSP.Price.HasValue)
                        throw new Exception(string.Format("Missing DSP for symbol {0}", prevPos.Symbol));

                    if (todayDSP == null || !todayDSP.Price.HasValue)
                        throw new Exception(string.Format("Today DSP for symbol {0}", prevPos.Symbol));


                    double prevPosVMReq = prevPos.Contracts * (todayDSP.Price.Value - prevDSP.Price.Value);

                    double todayPosVMReq = 0;

                    foreach (TradeDTO trade in todayTrades.Where(x => x.Symbol == prevPos.Symbol).ToList())
                    {
                        if (trade.GetSignedExecutionSize() > 0)
                        {
                            todayPosVMReq += trade.ExecutionSize * (todayDSP.Price.Value - trade.ExecutionPrice);
                        }
                        else
                        {
                            todayPosVMReq += trade.ExecutionSize * (trade.ExecutionPrice - todayDSP.Price.Value);
                        }
                    }

                    vmReqs += prevPosVMReq + todayPosVMReq;
                }

                return vmReqs * -1;//Negative is that I earned profit, Positive is that I loss
            }
            else
                return null;
        }

        protected double GetIMToday(string firmId, double priorIM, List<ClientPosition> todayPositions)
        {
            double newPositionsMargin = GetBaseMargin(firmId, todayPositions.ToArray());

            double IMtoday = newPositionsMargin > priorIM ? newPositionsMargin - priorIM : 0;

            return IMtoday;
        }


        #endregion
        


        #region Public Methods


        public MarginCollateralDTO CalculateMargin(string firmId, double todayCollateral, List<ClientPosition> todayPositions, List<TradeDTO> todayTrades)
        {
            //prev.1- If we have 0 contracts positions, we fill them
            FillPositions(todayPositions);

            //prev.2-we get theprevious days positions
            List<ClientPosition> prevPositions = GetPreviousDayPositions(todayPositions, todayTrades);
        
            //////Calculations
            double priorIM = GetBaseMargin(firmId, prevPositions.ToArray());

            double IMToday = GetIMToday(firmId, priorIM, todayPositions);

            double? imRequirement = GetIMRequirement(firmId, priorIM, IMToday, todayPositions);

            double? vmRequirement = GetVMRequirement(prevPositions, todayTrades);

            double? pendingCollateral = (imRequirement.HasValue && vmRequirement.HasValue) ? (double?)(todayCollateral + vmRequirement.Value + imRequirement.Value) : null;

            bool marginCall = pendingCollateral < 0;


            return new MarginCollateralDTO()
            {
                Firm = firmId,
                Collateral = todayCollateral,
                PendingCollateral = pendingCollateral,
                PriorIM = priorIM,
                IMToday = IMToday,
                IMRequirement = imRequirement,
                VMRequirement = vmRequirement,
                MarginCall = marginCall ? "Margin Call" : "Ok"
            };


        }

        #endregion
    }
}
