using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.SecurityList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DGTLBackendMock.Common.DTO.OrderRouting.V2;
using DGTLBackendMock.Common.DTO.Temp.Positions;
using DGTLBackendMock.Common.DTO.Account.V2.Credit_UI;
using ToolsShared.Logging;

namespace DGTLBackendMock.Common.Util.Margin
{
    public class CreditCalculator : BaseCreditLogic
    {
        #region Constructor

        public CreditCalculator(SecurityMasterRecord[] securities,DailySettlementPrice[] prices ,UserRecord[] users,  PriorDayMargin[] priorDayMargins, Config config, ILogSource logger)
        {
            UserRecords = users;
            SecurityMasterRecords = securities;
            DailySettlementPrices = prices;
            PriorDayMargins = priorDayMargins;
            Config=config;
            Logger = logger;
        
        }

        #endregion


        #region Public Methods

        //here I can work with just 1 security
        public double GetExposureChange(char side, double qty, string symbol, string firmId, ClientPosition[] Positions,List<OrderDTO> Orders)
        {
            double finalExposure = 0;
            double currentExposure = 0;
            double netContracts = 0;
            qty = (ClientOrderRecord._SIDE_BUY == side) ? qty : -1 * qty;

            netContracts = GetNetContracts(firmId, symbol, Positions, side, Orders);

            DailySettlementPrice DSP = DailySettlementPrices.Where(x => x.Symbol == symbol).FirstOrDefault();

            if (DSP != null && DSP.Price.HasValue)
            {
                currentExposure = (Math.Abs(netContracts) * DSP.Price.Value);
                finalExposure = (Math.Abs(netContracts + qty) * DSP.Price.Value);
            }


            return finalExposure - currentExposure;
        }

        public double GetTotalSideExposure(char side, string firmId, ClientPosition[] Positions, List<OrderDTO> Orders)
        {
            //1-Get Base Margin
            double BM = GetBaseMargin(firmId,Positions) - GetFundedMargin(firmId);

            //2-Calculate the exposure for the Buy/Sell orders
            double PxM = GetPotentialxMargin(side, firmId,Positions,Orders);

            //3- Calculate the potential x Exposure
            //double exposure = Math.Max(Convert.ToDouble(PxM - BM), 0) / Config.MarginPct;
            double exposure = Convert.ToDouble(PxM - BM) / Config.MarginPct;
            DoLog(string.Format("Side {0} exposure:{1}", side, exposure), zHFT.Main.Common.Util.Constants.MessageType.Information);
            return Convert.ToDouble(PxM - BM) / Config.MarginPct;

        }

        public double GetUsedCredit(string firmId, ClientPosition[] Positions)
        {

            List<NetPositionDTO> netPositionsArr = new List<NetPositionDTO>();

            double creditUsed = 0;

            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {
                double netContracts = GetNetContracts(firmId, security.Symbol, Positions); ;

                DailySettlementPrice DSP = DailySettlementPrices.Where(x => x.Symbol == security.Symbol).FirstOrDefault();

                if (DSP != null && DSP.Price.HasValue && netContracts != 0)
                    creditUsed += Math.Abs(netContracts) * DSP.Price.Value;

                if (security.MaturityDate != "")
                    netPositionsArr.Add(new NetPositionDTO() { AssetClass = security.AssetClass, Symbol = security.Symbol, MaturityDate = security.GetMaturityDate(), NetContracts = netContracts });

                DoLog(string.Format("Final Net Contracts for Security Id {0}:{1}", security.Symbol, netContracts), zHFT.Main.Common.Util.Constants.MessageType.Information);
            }

            if (Config.ImplementCalendarMarginDiscount)
            {
                string[] assetClasses = GetAvailableAssetClasses();

                foreach (string assetClass in assetClasses)
                {
                    creditUsed -= (CalculateCalendarMarginDiscounts(netPositionsArr.ToArray(), assetClass) / Config.MarginPct);
                }
            }

            return creditUsed - GetPriorDayCredit(firmId);
        }
        
        #endregion
    }
}
