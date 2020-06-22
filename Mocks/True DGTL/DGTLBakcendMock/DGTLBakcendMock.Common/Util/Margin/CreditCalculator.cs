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

    public class CreditCalculator
    {
        #region Protected Attributes

        protected UserRecord[] UserRecords { get; set; }

        protected SecurityMasterRecord[] SecurityMasterRecords { get; set; }

        protected DailySettlementPrice[] DailySettlementPrices { get; set; }

        protected FundedMargin[] FundedMargins { get; set; }

        protected Config Config { get; set; }

        public ILogSource Logger { get; set; }

        #endregion

        #region Constructor

        public CreditCalculator(SecurityMasterRecord[] securities,DailySettlementPrice[] prices ,UserRecord[] users,  FundedMargin[] fundedMargins, Config config, ILogSource logger)
        {
            UserRecords = users;
            SecurityMasterRecords = securities;
            DailySettlementPrices = prices;
            FundedMargins = fundedMargins;
            Config=config;
            Logger = logger;
        
        }

        #endregion

        #region Aux Methods

        protected void DoLog(string msg, zHFT.Main.Common.Util.Constants.MessageType type)
        {
            Logger.Debug(msg, type);
        }

        #endregion

        #region Private Methods

        private double GetPotentialxMargin(char side, string firmId, ClientPosition[] Positions, List<LegacyOrderRecord> Orders)
        {
            double acumMargin = 0;

            List<UserRecord> usersForFirm = UserRecords.Where(x => x.FirmId == firmId).ToList();

            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {
                double potentialNetContracts = 0;

                foreach (UserRecord userForFirms in usersForFirm)
                {

                    Positions.Where(x => x.Symbol == security.Symbol && x.UserId == userForFirms.UserId).ToList()
                            .ForEach(x => potentialNetContracts += x.Contracts);


                    Orders.Where(x => x.cSide == side && x.cStatus == LegacyOrderRecord._STATUS_OPEN
                                        && x.InstrumentId == security.Symbol && x.UserId == userForFirms.UserId).ToList()
                        .ForEach(x => potentialNetContracts += (x.cSide == LegacyOrderRecord._SIDE_BUY) ? x.LvsQty : (-1 * x.LvsQty));


                }

                DoLog(string.Format("Potential Contracts for Security {0}  after Orders:{1}", security.Symbol, potentialNetContracts), zHFT.Main.Common.Util.Constants.MessageType.Information);

                DailySettlementPrice DSP = DailySettlementPrices.Where(x => x.Symbol == security.Symbol).FirstOrDefault();

                if (DSP != null && DSP.Price.HasValue)
                {
                    acumMargin += Math.Abs(potentialNetContracts) * DSP.Price.Value * Config.MarginPct;
                }
            }


            //TODO : implement the calendar spreads margin calculation
            DoLog(string.Format("Acum Margin for FirmId {0} after Orders:{1}", firmId, acumMargin), zHFT.Main.Common.Util.Constants.MessageType.Information);
            return acumMargin - GetFundedMargin(firmId);
        }

        private double GetBaseMargin(string firmId, ClientPosition[] Positions, string symbol = null)
        {
            double acumMargin = 0;

            List<UserRecord> usersForFirm = UserRecords.Where(x => x.FirmId == firmId).ToList();

            foreach (SecurityMasterRecord security in SecurityMasterRecords.Where(x => (symbol == null || x.Symbol == symbol)))
            {
                double netContracts = 0;
                foreach (UserRecord userForFirms in usersForFirm)
                {

                    Positions.Where(x => x.Symbol == security.Symbol && x.UserId == userForFirms.UserId).ToList()
                                .ForEach(x => netContracts += x.Contracts);



                }

                DailySettlementPrice DSP = DailySettlementPrices.Where(x => x.Symbol == security.Symbol).FirstOrDefault();

                if (DSP != null && DSP.Price.HasValue)
                {
                    acumMargin += Math.Abs(netContracts) * DSP.Price.Value * Config.MarginPct;
                }

                DoLog(string.Format("Net Contracts for Security {0} :{1}", security.Symbol, netContracts), zHFT.Main.Common.Util.Constants.MessageType.Information);

            }

            //TODO : implement the calendar spreads margin calculation
            DoLog(string.Format("Base Margin for FirmId {0}:{1}", firmId, acumMargin), zHFT.Main.Common.Util.Constants.MessageType.Information);

            return acumMargin;
        }

        private double CalculateSpreadDiscount(NetPositionDTO currContract, NetPositionDTO nextContract, int spreadIndex)
        {
            double totalDiscount = 0;
            if (Math.Sign(nextContract.NetContracts) != Math.Sign(currContract.NetContracts))//we have a spread
            {

                double netSpread = Math.Min(Math.Abs(currContract.NetContracts), Math.Abs(nextContract.NetContracts));


                if (netSpread != 0)
                {
                    DailySettlementPrice DSP1 = DailySettlementPrices.Where(x => x.Symbol == currContract.Symbol).FirstOrDefault();
                    DailySettlementPrice DSP2 = DailySettlementPrices.Where(x => x.Symbol == nextContract.Symbol).FirstOrDefault();

                    if (spreadIndex == 1)//1-wide spread
                        totalDiscount += netSpread * Config.MarginPct * Config.OneWideCalDisc * (DSP1.Price.Value + DSP2.Price.Value);
                    else if (spreadIndex == 2)//2-wide spread
                        totalDiscount += netSpread * Config.MarginPct * Config.TwoWideCalDisc * (DSP1.Price.Value + DSP2.Price.Value);
                    else if (spreadIndex >= 3)//3-wide spread or wider
                        totalDiscount += netSpread * Config.MarginPct * Config.ThreeWideCalDisc * (DSP1.Price.Value + DSP2.Price.Value); ;

                    currContract.NetContracts -= (currContract.NetContracts > 0) ? netSpread : (-1 * netSpread);
                    nextContract.NetContracts -= (nextContract.NetContracts > 0) ? netSpread : (-1 * netSpread);
                }
            }

            return totalDiscount;
        }

        private double CalculateCalendarMarginDiscounts(NetPositionDTO[] netPositionsArr, string assetClass)
        {
            double totalDiscount = 0;
            int spreadIndex = 1;
            NetPositionDTO[] assetClassNetPositionsArr = netPositionsArr.Where(x => x.AssetClass == assetClass).ToArray();

            for (int i = 0; i < assetClassNetPositionsArr.Length; i++)
            {

                for (int j = 0; j < assetClassNetPositionsArr.Length; j++)
                {
                    NetPositionDTO currContract = assetClassNetPositionsArr.OrderBy(x => x.MaturityDate).ToArray()[j];
                    if ((j + spreadIndex) < assetClassNetPositionsArr.Length)
                    {
                        NetPositionDTO nextContract = assetClassNetPositionsArr.OrderBy(x => x.MaturityDate).ToArray()[j + spreadIndex];

                        totalDiscount += CalculateSpreadDiscount(currContract, nextContract, spreadIndex);
                    }
                }

                spreadIndex += 1;
            }

            return totalDiscount;
        }

        #endregion

        #region Public Methods

        //here I can work with just 1 security
        public double GetSecurityPotentialExposure(char side, double qty, string symbol, string firmId, ClientPosition[] Positions,
                                                   List<LegacyOrderRecord> Orders)
        {
            double finalExposure = 0;
            double currentExposure = 0;
            double netContracts = 0;
            qty = (ClientOrderRecord._SIDE_BUY == side) ? qty : -1 * qty;



            List<UserRecord> usersForFirm = UserRecords.Where(x => x.FirmId == firmId).ToList();

            foreach (UserRecord user in usersForFirm)
            {
                Positions.Where(x => x.Symbol == symbol && x.UserId == user.UserId).ToList().ForEach(x => netContracts += x.Contracts);

                //open orders too
                Orders.Where(x => x.cSide == side && x.cStatus == LegacyOrderRecord._STATUS_OPEN
                                         && x.InstrumentId == symbol && x.UserId == user.UserId).ToList()
                            .ForEach(x => netContracts += (x.cSide == LegacyOrderRecord._SIDE_BUY) ? x.LvsQty : (-1 * x.LvsQty));
            }

            DailySettlementPrice DSP = DailySettlementPrices.Where(x => x.Symbol == symbol).FirstOrDefault();

            if (DSP != null && DSP.Price.HasValue)
            {
                currentExposure = (Math.Abs(netContracts) * DSP.Price.Value);
                finalExposure = (Math.Abs(netContracts + qty) * DSP.Price.Value);
            }



            return finalExposure - currentExposure;
        }

        public double GetTotalSideExposure(char side, string firmId, ClientPosition[] Positions, List<LegacyOrderRecord> Orders)
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

        public double GetFundedMargin(string firmId)
        {
            return GetFundedCredit(firmId) * Config.MarginPct;
        }

        public double GetFundedCredit(string firmId)
        {
            FundedMargin fundedMargin = FundedMargins.Where(x => x.FirmId == firmId).FirstOrDefault();

            if (fundedMargin != null)
                return fundedMargin.Margin / Config.MarginPct;
            else
                return 0;
        }

        public double GetUsedCredit(string firmId, ClientPosition[] Positions)
        {

            List<UserRecord> usersForFirm = UserRecords.Where(x => x.FirmId == firmId).ToList();
            List<NetPositionDTO> netPositionsArr = new List<NetPositionDTO>();

            double creditUsed = 0;


            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {
                double netContracts = 0;
                foreach (UserRecord user in usersForFirm)
                {
                    Positions.Where(x => x.Symbol == security.Symbol && x.UserId == user.UserId).ToList().ForEach(x => netContracts += x.Contracts);
                }

                DailySettlementPrice DSP = DailySettlementPrices.Where(x => x.Symbol == security.Symbol).FirstOrDefault();

                if (DSP != null && DSP.Price.HasValue && netContracts != 0)
                    creditUsed += Math.Abs(netContracts) * DSP.Price.Value;

                if (security.MaturityDate != "")
                    netPositionsArr.Add(new NetPositionDTO() { AssetClass = security.AssetClass, Symbol = security.Symbol, MaturityDate = security.GetMaturityDate(), NetContracts = netContracts });

                DoLog(string.Format("Final Net Contracts for Security Id {0}:{1}", security.Symbol, netContracts), zHFT.Main.Common.Util.Constants.MessageType.Information);
            }

            if (Config.ImplementCalendarMarginDiscount)
            {
                creditUsed -= (CalculateCalendarMarginDiscounts(netPositionsArr.ToArray(), "SWP") / Config.MarginPct);
                creditUsed -= (CalculateCalendarMarginDiscounts(netPositionsArr.ToArray(), "NDF") / Config.MarginPct);
            }

            return creditUsed - GetFundedCredit(firmId);
        }


        #endregion
    }
}
