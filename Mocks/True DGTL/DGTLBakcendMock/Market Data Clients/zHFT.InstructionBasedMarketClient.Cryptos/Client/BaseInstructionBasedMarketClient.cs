using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.Cryptos.DataAccessLayer.Managers;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.Cryptos.Client
{
    public abstract class BaseInstructionBasedMarketClient : BaseCommunicationModule
    {

        #region protected  Consts

        protected int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        protected int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        #endregion

        #region Protected Attributes

        protected Dictionary<int, Security> ActiveSecuritiesQuotes { get; set; }

        protected Dictionary<int, Security> ActiveSecuritiesTrades { get; set; }

        protected Dictionary<int, Security> ActiveSecuritiesOrderBook { get; set; }

        protected Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        protected InstructionManager InstructionManager { get; set; }

        protected AccountManager AccountManager { get; set; }

        #endregion

        #region Abstract Methods

        protected abstract void DoRequestMarketDataQuotes(Object param);

        protected abstract void DoRequestMarketDataTrades(Object param);

        protected abstract void DoRequestMarketDataOrderBook(Object param);

        //protected abstract CMState ProessMarketDataRequest(Wrapper wrapper);

        protected abstract CMState ProcessMarketDataQuotesRequest(Wrapper wrapper);

        protected abstract CMState ProcessMarketDataTradesRequest(Wrapper wrapper);

        protected abstract CMState ProcessMarketDataOrderBookRequest(Wrapper wrapper);

        protected abstract int GetSearchForInstrInMiliseconds();

        protected abstract BaseConfiguration GetConfig();

        protected abstract int GetAccountNumber();

        #endregion

        #region Protected Methods

        protected void CleanPrevInstructions()
        {
            List<Instruction> prevInstrx = InstructionManager.GetPendingInstructions(GetAccountNumber());

            foreach (Instruction prevInstr in prevInstrx)
            {
                prevInstr.Executed = true;
                prevInstr.AccountPosition = null;
                InstructionManager.Persist(prevInstr);
            }
        }

        protected void RemoveSymbol(string symbol)
        {
            List<int> keysToRemove = new List<int>();

            foreach (int key in ActiveSecuritiesQuotes.Keys)
            {
                Security sec = ActiveSecuritiesQuotes[key];

                if (sec.Symbol == symbol)
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (int keyToRemove in keysToRemove)
            {
                ContractsTimeStamps.Remove(keyToRemove);
                ActiveSecuritiesQuotes.Remove(keyToRemove);
            }
        }

        protected void DoCleanOldSecurities()
        {
            while (true)
            {
                Thread.Sleep(_SECURITIES_REMOVEL_PERIOD);//Once every hour

                lock (tLock)
                {
                    try
                    {
                        List<int> keysToRemove = new List<int>();
                        foreach (int key in ContractsTimeStamps.Keys)
                        {
                            DateTime timeStamp = ContractsTimeStamps[key];

                            if ((DateTime.Now - timeStamp).Hours >= _MAX_ELAPSED_HOURS_FOR_MARKET_DATA)
                            {
                                keysToRemove.Add(key);
                            }
                        }

                        foreach (int keyToRemove in keysToRemove)
                        {
                            ContractsTimeStamps.Remove(keyToRemove);
                            ActiveSecuritiesQuotes.Remove(keyToRemove);
                        }
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{1}: There was an error cleaning old securities from market data flow error={0} ", ex.Message, GetConfig().Name),
                              Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }
        }

        protected Security BuildSecurityFromInstruction(Instruction instrx)
        {

            Security sec = new Security()
            {
                Symbol = instrx.Symbol,
                SecType = SecurityType.CC
            };

            return sec;
        }


        protected CMState ProcessMarketDataRequestQuotes(Wrapper wrapper)
        {
            string symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            int mdReqId = (int)wrapper.GetField(MarketDataRequestField.MDReqId);

            Security sec = new Security() { Symbol = symbol };

            lock (ActiveSecuritiesQuotes)
            {

                ActiveSecuritiesQuotes.Add(mdReqId, sec);
            }


            Thread RequestMarketDataThread = new Thread(DoRequestMarketDataQuotes);
            RequestMarketDataThread.Start(symbol);

            return CMState.BuildSuccess();
        }

        protected CMState ProcessMarketDataRequestTrades(Wrapper wrapper)
        {
            string symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            int mdReqId = (int)wrapper.GetField(MarketDataRequestField.MDReqId);

            Security sec = new Security() { Symbol = symbol };

            lock (ActiveSecuritiesTrades)
            {

                ActiveSecuritiesTrades.Add(mdReqId, sec);
            }


            Thread RequestMarketDataThread = new Thread(DoRequestMarketDataTrades);
            RequestMarketDataThread.Start(symbol);

            return CMState.BuildSuccess();
        }

        protected CMState ProcessMarketDataRequestOrderBook(Wrapper wrapper)
        {
            string symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            int mdReqId = (int)wrapper.GetField(MarketDataRequestField.MDReqId);

            Security sec = new Security() { Symbol = symbol };

            lock (ActiveSecuritiesOrderBook)
            {

                ActiveSecuritiesOrderBook.Add(mdReqId, sec);
            }


            Thread RequestMarketDataThread = new Thread(DoRequestMarketDataOrderBook);
            RequestMarketDataThread.Start(symbol);

            return CMState.BuildSuccess();
        }

        protected virtual CMState ProcessSecurityListRequest(Wrapper wrapper)
        {

            return CMState.BuildSuccess();
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    if (action == Actions.SECURITY_LIST_REQUEST)
                    {
                        return ProcessSecurityListRequest(wrapper);
                    }
                    else if (Actions.MARKET_DATA_QUOTES_REQUEST == action)
                    {
                        return ProcessMarketDataQuotesRequest(wrapper);
                    }
                    else if (Actions.MARKET_DATA_TRADES_REQUEST == action)
                    {

                        return ProcessMarketDataTradesRequest(wrapper);
                    }
                    else if (Actions.MARKET_DATA_ORDERBOOK_REQUEST == action)
                    {
                        return ProcessMarketDataOrderBookRequest(wrapper);
                    }
                    //else if (Actions.MARKET_DATA_REQUEST == action)
                    //{
                    //    return ProessMarketDataRequest(wrapper);
                    //}
                    else
                    {
                        DoLog("Sending message " + action + " not implemented", Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception("Sending message " + action + " not implemented"));
                    }
                }
                else
                    throw new Exception("Invalid Wrapper");

            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Main.Common.Util.Constants.MessageType.Error);
                throw;
            }
        }

        #endregion

    }
}
