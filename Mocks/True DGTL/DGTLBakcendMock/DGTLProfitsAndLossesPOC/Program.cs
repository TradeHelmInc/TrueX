using DGTLBackendMock.Common.DTO.Temp.Positions;
using DGTLBackendMock.Common.Util.Portfolio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLProfitsAndLossesPOC
{
    class Program
    {
        #region Private Static Methods

        private static List<TradeDTO> GetExecutionsScenario1()
        {
            List<TradeDTO> trades  = new List<TradeDTO>();

            TradeDTO tradexx = new TradeDTO() { Side = TradeDTO._TRADE_BUY,Symbol = "xxx", ExecutionSize = 20, ExecutionPrice = 8100 };
          
            TradeDTO trade2 = new TradeDTO() { Side = TradeDTO._TRADE_BUY, Symbol = "xxx", ExecutionSize = 10, ExecutionPrice = 8200 };
            TradeDTO trade3 = new TradeDTO() { Side = TradeDTO._TRADE_SELL, Symbol = "xxx", ExecutionSize = 5, ExecutionPrice = 8300 };
            TradeDTO trade4 = new TradeDTO() { Side = TradeDTO._TRADE_SELL, Symbol = "xxx", ExecutionSize = 10, ExecutionPrice = 8400 };
            TradeDTO trade5 = new TradeDTO() { Side = TradeDTO._TRADE_SELL, Symbol = "xxx", ExecutionSize = 10, ExecutionPrice = 8500 };

            //P&L=5*(8300-8100) +10*(8400-8100) + 5 (8500-8100) + 5 (8500-8200) +5 (9100-8200)

            trades.Add(tradexx);
            trades.Add(trade2);
            trades.Add(trade3);
            trades.Add(trade4);
            trades.Add(trade5);

            return trades;
        
        }

        private static List<TradeDTO> GetExecutionsScenario2()
        {
            List<TradeDTO> trades = new List<TradeDTO>();

            TradeDTO trade1 = new TradeDTO() { Side = TradeDTO._TRADE_BUY, Symbol = "xxx", ExecutionSize = 20, ExecutionPrice = 8100 };
            TradeDTO trade2 = new TradeDTO() { Side = TradeDTO._TRADE_BUY, Symbol = "xxx", ExecutionSize = 10, ExecutionPrice = 8200 };
            TradeDTO trade3 = new TradeDTO() { Side = TradeDTO._TRADE_SELL, Symbol = "xxx", ExecutionSize = 40, ExecutionPrice = 8300 };
          

            trades.Add(trade1);
            trades.Add(trade2);
            trades.Add(trade3);

            return trades;

        }


        private static List<TradeDTO> GetExecutionsScenario3()
        {
            List<TradeDTO> trades = new List<TradeDTO>();

            TradeDTO trade1 = new TradeDTO() { Side = TradeDTO._TRADE_BUY, Symbol = "xxx", ExecutionSize = 20, ExecutionPrice = 8100 };
            TradeDTO trade2 = new TradeDTO() { Side = TradeDTO._TRADE_BUY, Symbol = "xxx", ExecutionSize = 10, ExecutionPrice = 8200 };
            TradeDTO trade3 = new TradeDTO() { Side = TradeDTO._TRADE_SELL, Symbol = "xxx", ExecutionSize = 5, ExecutionPrice = 8300 };
            TradeDTO trade4 = new TradeDTO() { Side = TradeDTO._TRADE_SELL, Symbol = "xxx", ExecutionSize = 35, ExecutionPrice = 8400 };


            trades.Add(trade1);
            trades.Add(trade2);
            trades.Add(trade3);
            trades.Add(trade4);

            return trades;

        }

        #endregion

        static void Main(string[] args)
        {

            double profitsAndLosses = 0;
            PortfolioCalculator calc = new PortfolioCalculator();
            
            double currentPrice = 9100;


            //executions scenario 1
            List<TradeDTO> executionsScenario1 = GetExecutionsScenario1();
            profitsAndLosses=profitsAndLosses = calc.CalculateProfitsAndLosses(executionsScenario1, currentPrice);
            Console.WriteLine(string.Format("Profits and losses scenario 1:{0}", profitsAndLosses));

            //executions scenario 2
            List<TradeDTO> executionsScenario2 = GetExecutionsScenario2();
            profitsAndLosses = profitsAndLosses = calc.CalculateProfitsAndLosses(executionsScenario2, currentPrice);
            Console.WriteLine(string.Format("Profits and losses scenario 2:{0}", profitsAndLosses));

            //executions scenario 3
            List<TradeDTO> executionsScenario3 = GetExecutionsScenario3();
            profitsAndLosses = profitsAndLosses = calc.CalculateProfitsAndLosses(executionsScenario3, currentPrice);
            Console.WriteLine(string.Format("Profits and losses scenario 3:{0}", profitsAndLosses));


            Console.ReadKey();



        }
    }
}
