using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Account.V2.Credit_UI;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Temp.Positions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsShared.Logging;

namespace DGTLBackendMock.Common.Util.Margin
{
    public class BaseCreditLogic
    {
        #region Protected Attributes

        public ILogSource Logger { get; set; }

        protected SecurityMasterRecord[] SecurityMasterRecords { get; set; }

        protected Config Config { get; set; }

        protected UserRecord[] UserRecords { get; set; }

        protected DailySettlementPrice[] DailySettlementPrices { get; set; }

        protected PriorDayMargin[] PriorDayMargins { get; set; }

        #endregion

        #region Protected Methods

        protected void DoLog(string msg, zHFT.Main.Common.Util.Constants.MessageType type)
        {
            Logger.Debug(msg, type);
        }

        protected double GetFundedMargin(string firmId)
        {
            return GetPriorDayCredit(firmId) * Config.MarginPct;
        }

        protected double GetPotentialxMargin(char side, string firmId, ClientPosition[] Positions, List<OrderDTO> Orders)
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


                    Orders.Where(x => x.cSide == side && x.cStatus == OrderDTO._STATUS_OPEN
                                        && x.InstrumentId == security.Symbol && x.UserId == userForFirms.UserId).ToList()
                        .ForEach(x => potentialNetContracts += (x.cSide == OrderDTO._SIDE_BUY) ? x.LvsQty : (-1 * x.LvsQty));


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

        protected double GetBaseMargin(string firmId, ClientPosition[] Positions, string symbol = null)
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

        protected double CalculateCalendarMarginDiscounts(NetPositionDTO[] netPositionsArr, string assetClass)
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

        #region Aux Methods


        protected string[] GetAvailableAssetClasses()
        {
            List<string> assetClasses = new List<string>();

            foreach (SecurityMasterRecord sec in SecurityMasterRecords)
            {
                if (!assetClasses.Contains(sec.AssetClass) && sec.AssetClass != SecurityMasterRecord._AS_SPOT)
                {
                    assetClasses.Add(sec.AssetClass);
                }
            }

            return assetClasses.ToArray();
        }

        #endregion

        #region Public Methods
        public double GetPriorDayCredit(string firmId)
        {
            PriorDayMargin fundedMargin = PriorDayMargins.Where(x => x.FirmId == firmId).FirstOrDefault();

            if (fundedMargin != null)
                return fundedMargin.Margin / Config.MarginPct;
            else
                return 0;
        }


        #endregion
    }
}
