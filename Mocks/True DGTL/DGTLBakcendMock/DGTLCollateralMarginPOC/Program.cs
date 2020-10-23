using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Temp.Csv;
using DGTLBackendMock.Common.DTO.Temp.Margin;
using DGTLBackendMock.Common.DTO.Temp.Positions;
using DGTLBackendMock.Common.Loaders.csv;
using DGTLBackendMock.Common.Util.Margin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsShared.Logging;

namespace DGTLCollateralMarginPOC
{

    class Program
    {

        #region Protected Static Methods

        protected static Config GetConfig()
        {
            string strConfig = File.ReadAllText(@".\Config.json");

            Config config =  JsonConvert.DeserializeObject<Config>(strConfig);

            return config;
        }

        protected static void PrintMarginCollateral(MarginCollateralDTO marginCollateral)
        {
            Console.WriteLine(string.Format(" Firm={0} Collateral={1} PendingCollateral={2} PriorIM={3} IMToday={4} IM Req.={5} VM Req.={6} Status={7}",
                                            marginCollateral.Firm,
                                            marginCollateral.Collateral.ToString("0.##"),
                                            marginCollateral.PendingCollateral.HasValue ? marginCollateral.PendingCollateral.Value.ToString("0.##") : "-",
                                            marginCollateral.PriorIM.ToString("0.##"),
                                            marginCollateral.IMToday.HasValue ? marginCollateral.IMToday.Value.ToString("0.##") : "-",
                                            marginCollateral.IMRequirement.HasValue ? marginCollateral.IMRequirement.Value.ToString("0.##") : "-",
                                            marginCollateral.VMRequirement.HasValue ? marginCollateral.VMRequirement.Value.ToString("0.##") : "-",
                                            marginCollateral.MarginCall));
        }

        #endregion


        static void Main(string[] args)
        {

            string positionsCSV = ConfigurationManager.AppSettings["PositionsFile"];
            string tradesCSV = ConfigurationManager.AppSettings["TodayTrades"];

            PositionsCSVDTO positionsDTO = PositionsLoader.GetPositions(positionsCSV);

            List<TradeDTO> todayTrades = ExecutionsLoader.GetTrades(tradesCSV);

            ILogSource Logger = new PerDayFileLogSource(Directory.GetCurrentDirectory() + "\\Log", Directory.GetCurrentDirectory() + "\\Log\\Backup")
            {
                FilePattern = "Log.{0:yyyy-MM-dd}.log",
                DeleteDays = 20
            };


            MarginCollateralCalculator calc = new MarginCollateralCalculator(pSecurities: positionsDTO.GetSecurityMasterRecords(),
                                                                             pTodayDSPs: positionsDTO.GetTodayDailySettlementPrice(),
                                                                             pPrevDSPs: positionsDTO.GetYesterdayDailySettlementPrice(),
                                                                             pConfig: GetConfig(),
                                                                             pLogger: Logger);


            Console.WriteLine("===================== MARGIN/COLLATERAL grid ===================== ");
            foreach (string firm in positionsDTO.FirmPositions.Keys)
            {

                MarginCollateralDTO marginCollateral= calc.CalculateMargin(firmId: firm, todayCollateral: 0, todayPositions: positionsDTO.FirmPositions[firm], todayTrades: todayTrades);


                PrintMarginCollateral(marginCollateral);

            
            }



            Console.ReadKey();

        }
    }
}
