using DGTLBackendMock.Common.DTO.Temp.Positions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DGTLBackendMock.Common.Util.Portfolio
{
    public class PortfolioCalculator
    {

        #region Private void

        private void ValidateExecutions(List<TradeDTO> executions)
        {

            int count = 0;
            List<string> symbols = new List<string>();

            foreach (TradeDTO execution in executions)
            {
                if (!symbols.Contains(execution.Symbol))
                {
                    symbols.Add(execution.Symbol);

                    count++;
                }
                        
            }


            if (count > 1)
                throw new Exception(string.Format(" P&L calculations can only be calculated one security at a time and {0} were found", count));
        
        }

        private NetPositionDTO BuildNewPosition(TradeDTO execution)
        {

            NetPositionDTO position = new NetPositionDTO()
            {
                Symbol = execution.Symbol,
                NetContracts = execution.Side == TradeDTO._TRADE_BUY ? execution.ExecutionSize : -1 * execution.ExecutionSize,
                PositionExposure = execution.Side == TradeDTO._TRADE_BUY ? NetPositionDTO._LONG : NetPositionDTO._SHORT,
                OpenPrice = execution.ExecutionPrice
            };

            return position;
        
        }

        #endregion

        public double CalculateTotalProfitsAndLosses(List<TradeDTO> executions, double currentPrice)
        {

            ValidateExecutions(executions);

            double pandL = 0;

            List<NetPositionDTO> positions = new List<NetPositionDTO>();
          
            foreach (TradeDTO execution in executions)
            {
                if (positions.Count == 0)
                {
                    //First trade-> first position
                    positions.Add(BuildNewPosition(execution));
                }
                else
                {
                    double tradeSize = execution.GetSignedExecutionSize();

                    foreach(NetPositionDTO openPos in positions.Where(x=>x.NetContracts!=0))
                    {
                        if (Math.Abs(tradeSize) != 0)
                        {
                            if (openPos.IsCoverOrFlipping(execution))//The execution is in the opposite direction of this position
                            {
                                if (Math.Abs(openPos.NetContracts) > Math.Abs(tradeSize))//Partially Covered
                                {
                                    double covered = execution.GetSignedExecutionSize(tradeSize);
                                    tradeSize = 0;
                                    pandL += (-1 * covered) * (execution.ExecutionPrice - openPos.OpenPrice);
                                    openPos.NetContracts += covered;
                                    openPos.UpdateExposure();
                                }
                                else//Fully Covered or flipped
                                {
                                    tradeSize += openPos.NetContracts;
                                    double covered = openPos.NetContracts;
                                    pandL += covered * (execution.ExecutionPrice - openPos.OpenPrice);
                                    openPos.NetContracts = 0;
                                    openPos.UpdateExposure();
                                }


                            }
                            else// We have a trade in the same direction of the available positions
                            {
                                positions.Add(BuildNewPosition(execution));
                                tradeSize = 0;
                                break;
                            }
                        }
                    }

                    if (Math.Abs(tradeSize) != 0)
                    {
                        execution.ExecutionSize = Math.Abs(tradeSize);
                        positions.Add(BuildNewPosition(execution));
                        tradeSize = 0;
                        //we flipped the position (LONG-> SHORT, SHORT-> LONG)
                    }

                    positions = positions.Where(x => x.NetContracts != 0).ToList();
                }
            }

            //Now the open position
            NetPositionDTO currOpenPos = positions.Where(x => x.NetContracts != 0).FirstOrDefault();
            if (currOpenPos != null)
            {
                pandL += currOpenPos.NetContracts * (currentPrice - currOpenPos.OpenPrice);
            
            }

            return pandL;
        }

        public double? CalculateIncrementalProfitsAndLosses(NetPositionDTO prevNetContracts, double prevDSP, double? currentPrice,List<TradeDTO> todayTrades)
        {
            if (prevNetContracts == null || todayTrades == null || !currentPrice.HasValue)
                return null;

            double prevPAndL = prevNetContracts.NetContracts > 0 ? prevNetContracts.NetContracts * (currentPrice.Value - prevDSP)
                                                                 : prevNetContracts.NetContracts * (prevDSP - currentPrice.Value);

            foreach (TradeDTO trade in todayTrades)
            {
                double currPAndL = trade.Side == TradeDTO._TRADE_BUY ? trade.ExecutionSize * (currentPrice.Value - trade.ExecutionPrice)
                                                                        : trade.ExecutionSize * (trade.ExecutionPrice - currentPrice.Value);

                prevPAndL += currPAndL;
            
            }

            return prevPAndL;
        
        }

        public double? CalculateIncrementalVariationMargin(NetPositionDTO prevNetContracts, double prevDSP, double? todayDSP, List<TradeDTO> todayTrades)
        {
            if (prevNetContracts == null || todayTrades == null || !todayDSP.HasValue)
                return null;

            double prevPAndL = prevNetContracts.NetContracts > 0 ? prevNetContracts.NetContracts * (todayDSP.Value - prevDSP)
                                                                 : prevNetContracts.NetContracts * (prevDSP - todayDSP.Value);

            foreach (TradeDTO trade in todayTrades)
            {
                double currPAndL = trade.ExecutionSize > 0 ? trade.ExecutionSize * (todayDSP.Value - trade.ExecutionPrice)
                                                           : trade.ExecutionSize * (trade.ExecutionPrice - todayDSP.Value);

                prevPAndL += currPAndL;

            }

            return prevPAndL;

        }

        //public double? CalculateIncrementalProfitsAndLosses(double netContracts,double prevDSP, double? currentPrice)
        //{

        //    if (!currentPrice.HasValue)
        //        return null;

        //    if (netContracts > 0)
        //    {
        //        return netContracts * (currentPrice.Value - prevDSP);
        //    }
        //    else
        //    {

        //        return Math.Abs(netContracts) * (prevDSP - currentPrice.Value);
        //    }
        
        
        //}
    }
}
