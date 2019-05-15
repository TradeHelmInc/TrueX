using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Websockets;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Websockets.Events;
using zHFT.InstructionBasedMarketClient.BitMex.Common.Util;
using zHFT.InstructionBasedMarketClient.BitMex.Common.Wrappers;
using zHFT.InstructionBasedMarketClient.BitMex.DAL.REST;
using zHFT.InstructionBasedMarketClient.BitMex.DAL.Websockets;
using zHFT.InstructionBasedMarketClient.BitMex.Logic;
using zHFT.InstructionBasedMarketClient.Common.Configuration;
using zHFT.InstructionBasedMarketClient.Cryptos.Client;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Converters;
using zHFT.MarketClient.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.BitMex
{
    public class BitMexInstructionBasedMarketClient : BaseInstructionBasedMarketClient
    {
        #region Protected Attributes

        public  MarketDataManager WSMarketDataManager { get; set; }

        public zHFT.InstructionBasedMarketClient.BitMex.DAL.MarketDataManager RESTMarketDataManager { get; set; }

        public SecurityListManager SecurityListManager { get; set; }

        protected OrderBookHandler OrderBookHandler { get; set; }

        protected PriceLevelHandler PriceLevelHandler { get; set; }

        protected List<zHFT.InstructionBasedMarketClient.BitMex.BE.Security> Securities { get; set; }

        #endregion

        #region Protected Methods

        protected BitMex.Common.Configuration.Configuration BitmexConfiguration
        {
            get { return (BitMex.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        #region Overriden

        protected override BaseConfiguration GetConfig()
        {
            return BitmexConfiguration;
        }
        
        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new BitMex.Common.Configuration.Configuration().GetConfiguration<BitMex.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected override int GetSearchForInstrInMiliseconds()
        {
            return 0;
        }

        protected override int GetAccountNumber()
        {
            return 0;
        }

        #endregion

        private  void HandleGenericSubscription(WebSocketResponseMessage WebSocketResponseMessage)
        {
            WebSocketSubscriptionResponse resp = (WebSocketSubscriptionResponse)WebSocketResponseMessage;

            if (resp.success)
                DoLog(string.Format("Successfully subscribed to {0} event on symbol {1}",
                                            resp.GetSubscriptionEvent(), resp.GetSubscriptionAsset()),Main.Common.Util.Constants.MessageType.Information);
            else
                Console.WriteLine(string.Format("Error on subscription to {0} event on symbol {1}:{2}",
                                            resp.GetSubscriptionEvent(), resp.GetSubscriptionAsset(),resp.error), Main.Common.Util.Constants.MessageType.Error);
        }

        protected void OrderBookSubscriptionResponse(WebSocketResponseMessage WebSocketResponseMessage)
        {
            HandleGenericSubscription(WebSocketResponseMessage);
        }

        protected  void TradeSubscriptionResponse(WebSocketResponseMessage subcrResp)
        {
            HandleGenericSubscription(subcrResp);
        }

        protected void QuoteSubscriptionResponse(WebSocketResponseMessage subcrResp)
        {
            HandleGenericSubscription(subcrResp);
        }


        protected void ProcessPriceLevelIds(string action, zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.OrderBookEntry entry)
        { 
         //First we have to process the price level Ids
            if ( action == "partial")
            {
                if (!PriceLevelHandler.GetPriceLevelDict(entry.symbol).ContainsKey(entry.id))
                    PriceLevelHandler.GetPriceLevelDict(entry.symbol).Add(entry.id, entry.price);
                else
                    DoLog(string.Format("@{0}:WARNING1:Received PARTIAL Price Level for a price leven that already existed", BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
            }
            else if (action == "insert")
            {
                if (!PriceLevelHandler.GetPriceLevelDict(entry.symbol).ContainsKey(entry.id))
                    PriceLevelHandler.GetPriceLevelDict(entry.symbol).Add(entry.id, entry.price);
                else
                    DoLog(string.Format("@{0}:WARNING1:Received INSERT Price Level for a price leven that already existed", BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
            }
            else if (action == "update" || action == "delete")
            {

                if (PriceLevelHandler.GetPriceLevelDict(entry.symbol).ContainsKey(entry.id))
                {
                    decimal priceLevel = PriceLevelHandler.GetPriceLevelDict(entry.symbol)[entry.id];
                    entry.price = priceLevel;
                }
                else
                    DoLog(string.Format("@{0}:WARNING2 :Received UPDATE/REMOVE Price Level for a price leven that did not existed", BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);

            }
            else
                DoLog(string.Format("Received message for unknown action : {0}", action), Main.Common.Util.Constants.MessageType.Error);
        }

        protected void SendOrderBookEntries(string action, 
                                            zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.OrderBookEntry[] bids,
                                            zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.OrderBookEntry[] asks)
        {
           


            foreach (zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.OrderBookEntry bid in bids)
            {
                ProcessPriceLevelIds(action, bid);
                BitmexMarketDataOrderBookEntryWrapper wrapper = new BitmexMarketDataOrderBookEntryWrapper(action, bid, true);
                OnMessageRcv(wrapper);
            }

            foreach (zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.OrderBookEntry ask in asks)
            {
                ProcessPriceLevelIds(action, ask);
                BitmexMarketDataOrderBookEntryWrapper wrapper = new BitmexMarketDataOrderBookEntryWrapper(action, ask, false);
                OnMessageRcv(wrapper);
            }
        
        }

        protected void ProcessSubscrError(WebSocketErrorMessage subscrError)
        {
            string error = string.Format("{0}. Symbol = {1}", subscrError.error, subscrError.Symbol);
            BitmexMarketDataErrorWrapper errorWrapper = new BitmexMarketDataErrorWrapper(subscrError.Symbol,error);
            OnMessageRcv(errorWrapper);
        
        }

        //We update an orderBook in memory and publish
        protected  void UpdateOrderBook(WebSocketSubscriptionEvent subscrEvent)
        {

            if (subscrEvent is WebSocketErrorMessage)
            {

                ProcessSubscrError((WebSocketErrorMessage)subscrEvent);
                return;
            }

            WebSocketOrderBookL2Event orderBookEvent = (WebSocketOrderBookL2Event)subscrEvent;

            try
            {
                zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.OrderBookEntry[] bids = orderBookEvent.data.Where(x => x.IsBuy()).OrderByDescending(x => x.price).ToArray();
                zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.OrderBookEntry[] asks = orderBookEvent.data.Where(x => x.IsSell()).OrderBy(x => x.price).ToArray();


                SendOrderBookEntries(orderBookEvent.action, bids, asks);
                OrderBookHandler.DoUpdateOrderBooks(orderBookEvent.action, bids, asks);

                //string symbol = "";
                //if (bids.Length > 0)
                //    symbol = bids[0].symbol;
                //else if (asks.Length > 0)
                //    symbol = asks[0].symbol;
                //else
                //    return;

                //if (ActiveSecuritiesQuotes.Values.Any(x => x.Symbol == symbol) && OrderBookHandler.OrderBooks.ContainsKey(symbol))
                //{
                //    Security sec = ActiveSecuritiesQuotes.Values.Where(x => x.Symbol == symbol).FirstOrDefault();

                //    OrderBookDictionary dict = OrderBookHandler.OrderBooks[symbol];
                //    zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.OrderBookEntry bestBid = dict.Entries.Values.Where(x => x.IsBuy() && x.size > 0).OrderByDescending(x => x.price).FirstOrDefault();
                //    zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.OrderBookEntry bestAsk = dict.Entries.Values.Where(x => x.IsSell() && x.size > 0).OrderBy(x => x.price).FirstOrDefault();

                //    sec.MarketData.BestBidPrice = Convert.ToDouble(bestBid.price);
                //    sec.MarketData.BestBidSize = Convert.ToInt64(bestBid.size);
                //    sec.MarketData.BestAskPrice = Convert.ToDouble(bestAsk.price);
                //    sec.MarketData.BestAskSize = Convert.ToInt64(bestAsk.size);

                //    //DoLog(string.Format("@{5}:Publishing Market Data  for symbol {0}: Best Bid Size={1} Best Bid Price={2} Best Ask Size={3} Best Ask Price={4}",
                //    //                    symbol, bestBid.size.ToString("##.########"), bestBid.price.ToString("##.##"),
                //    //                          bestAsk.size.ToString("##.########"), bestAsk.price.ToString("##.##"),
                //    //                    BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);



                //    MarketDataWrapper wrapper = new MarketDataWrapper(sec, GetConfig());
                //    OnMessageRcv(wrapper);


                //}
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error processing order book :{1}", BitmexConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void UpdateTrade(WebSocketSubscriptionEvent subscrEvent)
        {

            if (subscrEvent is WebSocketErrorMessage)
            {

                ProcessSubscrError((WebSocketErrorMessage)subscrEvent);
                return;
            }

            WebSocketTradeEvent trades = (WebSocketTradeEvent)subscrEvent;
            foreach (zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Trade trade in trades.data.OrderBy(x=>x.timestamp))
            {
                try
                {
                    if (ActiveSecuritiesQuotes.Values.Where(x => x.Symbol == trade.symbol).FirstOrDefault() != null)
                    {
                        Security sec = ActiveSecuritiesQuotes.Values.Where(x => x.Symbol == trade.symbol).FirstOrDefault();

                        sec.MarketData.Trade = Convert.ToDouble(trade.price);
                        sec.MarketData.MDTradeSize = Convert.ToDouble(trade.size);
                        sec.MarketData.LastTradeDateTime = trade.timestamp;
                        sec.ProcessStatistics();

                        DoLog(string.Format("@{5}:NEW TRADE for symbol {0}: Side={1} Size={2} Price={3} TickDirection={4}",
                            trade.symbol, trade.side, trade.size.ToString("##.##"), trade.price.ToString("##.########"), trade.tickDirection,
                            BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);

                        MarketDataTradesWrapper wrapper = new MarketDataTradesWrapper(sec, GetConfig());
                        OnMessageRcv(wrapper);
                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Error processing trade for symbol {1}:{2}", BitmexConfiguration.Name, trade.symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                }
            }

           

        }

        protected void UpdateQuotes(WebSocketSubscriptionEvent subscrEvent)
        {
            if (subscrEvent is WebSocketErrorMessage)
            {

                ProcessSubscrError((WebSocketErrorMessage)subscrEvent);
                return;
            }

            WebSocketQuoteEvent quotes = (WebSocketQuoteEvent)subscrEvent;
            foreach (Quote quote in quotes.data.OrderBy(x => x.timestamp))
            {
                try
                {
                    if (ActiveSecuritiesQuotes.Values.Where(x => x.Symbol == quote.symbol).FirstOrDefault() != null)
                    {
                        Security sec = ActiveSecuritiesQuotes.Values.Where(x => x.Symbol == quote.symbol).FirstOrDefault();


                        sec.MarketData.BestAskSize = quote.askSize.HasValue ? (long?)Convert.ToInt64(quote.askSize.Value) : null;
                        sec.MarketData.BestBidSize = quote.bidSize.HasValue ? (long?)Convert.ToInt64(quote.bidSize.Value) : null;
                        sec.MarketData.BestAskPrice = quote.askPrice;
                        sec.MarketData.BestBidPrice = quote.bidPrice;

                        DoLog(string.Format("@{1}:NEW Quote for symbol {0}", quote.symbol, BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);

                        MarketDataQuoteWrapper wrapper = new MarketDataQuoteWrapper(sec, GetConfig());
                        OnMessageRcv(wrapper);
                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Error processing quote for symbol {1}:{2}", BitmexConfiguration.Name, quote.symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        protected override void DoRequestMarketDataQuotes(Object param)
        {
            string symbol = (string)param;
            try
            {
                DoLog(string.Format("@{0}:Requesting market data quotes por symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                if (ActiveSecuritiesQuotes.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                {
                    lock (tLock)
                    {
                        WSMarketDataManager.SubscribeQuotes(symbol);
                    }
                }
                else
                {
                    DoLog(string.Format("@{0}:Unsubscribing market data quotes for symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Error Requesting market data quotes por symbol {1}:{2}", BitmexConfiguration.Name, symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected override void DoRequestMarketDataTrades(Object param)
        {
            string symbol = (string)param;
            try
            {
                DoLog(string.Format("@{0}:Requesting market data trades por symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                if (ActiveSecuritiesTrades.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                {
                    lock (tLock)
                    {
                        WSMarketDataManager.SubscribeTrades(symbol);
                    }
                }
                else
                {
                    DoLog(string.Format("@{0}:Unsubscribing market data trades for symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Error Requesting market data trades por symbol {1}:{2}", BitmexConfiguration.Name, symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected override void DoRequestMarketDataOrderBook(Object param)
        {
            string symbol = (string)param;
            try
            {
                DoLog(string.Format("@{0}:Requesting market data order book por symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                if (ActiveSecuritiesOrderBook.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                {
                    lock (tLock)
                    {
                        PriceLevelHandler.GetPriceLevelDict(symbol).Clear();
                        WSMarketDataManager.SubscribeOrderBookL2(symbol);
                    }
                }
                else
                {
                    DoLog(string.Format("@{0}:Unsubscribing market data order book  for symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Error Requesting market data order book  por symbol {1}:{2}", BitmexConfiguration.Name, symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        #endregion

        #region Public Methods

        protected override CMState ProcessSecurityListRequest(Wrapper wrapper)
        {
            try
            {
                BitmexSecurityListWrapper secListWrapper = new BitmexSecurityListWrapper(Securities, Config);

                OnMessageRcv(secListWrapper);

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected void ProcessContractSize(List<BE.Security> Securities)
        {
            foreach (BE.Security future in Securities.Where(x => x.SecurityType == zHFT.InstructionBasedMarketClient.BitMex.BE.SecurityType.FUT))
            {

                if (future.Symbol.ToUpper() != "XBT")
                {
                    BE.Security bitcoinFuture = Securities.Where(x => x.SecurityType == zHFT.InstructionBasedMarketClient.BitMex.BE.SecurityType.FUT
                                                                && x.Symbol == "XBT" && x.MaturityMonthYear == future.MaturityMonthYear).FirstOrDefault();

                    if (bitcoinFuture != null)
                    {

                        if (!bitcoinFuture.LastTrade.HasValue)
                            throw new Exception(string.Format("Missing Bitcoin future last trade for symbol {0}", bitcoinFuture.Symbol));

                        if (!future.LastTrade.HasValue)
                            throw new Exception(string.Format("Missing  future last trade for symbol {0}", future.Symbol));


                        if (bitcoinFuture.LastTrade.HasValue && future.LastTrade.HasValue)
                        {
                            double cryptoSpotPrice = bitcoinFuture.LastTrade.Value * future.LastTrade.Value;
                            future.LotSize *= Convert.ToDecimal(cryptoSpotPrice);
                        }

                    }
                    else
                        throw new Exception(string.Format("@{0}: Could not process contract size for symbol {1}. Not bitcoin future found for maturity {2}",
                                               Config.Name, future.Symbol, future.MaturityMonthYear));
                }
            
            
            }
        
        }

        protected void DoRemove(Dictionary<int, Security> Dict,string key)
        {

            List<int> reqsToRemove = new List<int>();
            foreach (int mdReqId in Dict.Keys)
            {
                if (Dict[mdReqId].Symbol == key)
                    reqsToRemove.Add(mdReqId);

            }

            reqsToRemove.ForEach(x => Dict.Remove(x));
        
        }

        protected void CancelMarketDataQuotes(Security sec)
        {
            DoLog(string.Format("@{0}:Requesting Unsubscribe Market Data quotes On Demand for Symbol: {0}", GetConfig().Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);

            if (ActiveSecuritiesQuotes.Values.Any(x => x.Symbol == sec.Symbol))
            {
                lock (ActiveSecuritiesQuotes)
                {
                    DoRemove(ActiveSecuritiesQuotes, sec.Symbol);
                    WSMarketDataManager.UnsubscribeQuotes(sec.Symbol);
                    //PriceLevelHandler.GetPriceLevelDict(sec.Symbol).Clear();
                }
            }
            else
                throw new Exception(string.Format("@{0}: Could not find active security to unsubscribe for symbol {1}", GetConfig().Name, sec.Symbol));
        }

        protected void CancelMarketDataTrades(Security sec)
        {
            DoLog(string.Format("@{0}:Requesting Unsubscribe Market Data trades On Demand for Symbol: {0}", GetConfig().Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);

            if (ActiveSecuritiesTrades.Values.Any(x => x.Symbol == sec.Symbol))
            {
                lock (ActiveSecuritiesTrades)
                {
                    DoRemove(ActiveSecuritiesTrades, sec.Symbol);
                    WSMarketDataManager.UnsubscribeTrades(sec.Symbol);
                }
            }
            else
                throw new Exception(string.Format("@{0}: Could not find active security to unsubscribe trades for symbol {1}", GetConfig().Name, sec.Symbol));
        }

        protected void CancelMarketDataOrderBook(Security sec)
        {
            DoLog(string.Format("@{0}:Requesting Unsubscribe Market Data order book On Demand for Symbol: {0}", GetConfig().Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);

            if (ActiveSecuritiesOrderBook.Values.Any(x => x.Symbol == sec.Symbol))
            {
                lock (ActiveSecuritiesOrderBook)
                {
                    DoRemove(ActiveSecuritiesOrderBook, sec.Symbol);
                    WSMarketDataManager.UnsubscribeOrderBookL2(sec.Symbol);
                    PriceLevelHandler.GetPriceLevelDict(sec.Symbol).Clear();
                }
            }
            else
                throw new Exception(string.Format("@{0}: Could not find active security to unsubscribe order book for symbol {1}", GetConfig().Name, sec.Symbol));
        }

        protected override CMState ProcessMarketDataQuotesRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data Quotes snaphsot not implemented for symbol {1}", BitmexConfiguration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    return ProcessMarketDataRequestQuotes(wrapper);
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    CancelMarketDataQuotes(mdr.Security);

                    return CMState.BuildSuccess();
                }
                else
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", BitmexConfiguration.Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected override CMState ProcessMarketDataTradesRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data Trades snaphsot not implemented for symbol {1}", BitmexConfiguration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    return ProcessMarketDataRequestTrades(wrapper);
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    CancelMarketDataTrades(mdr.Security);

                    return CMState.BuildSuccess();
                }
                else
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", BitmexConfiguration.Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected override CMState ProcessMarketDataOrderBookRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data Order Book snaphsot not implemented for symbol {1}", BitmexConfiguration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    return ProcessMarketDataRequestOrderBook(wrapper);
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    CancelMarketDataOrderBook(mdr.Security);

                    return CMState.BuildSuccess();
                }
                else
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", BitmexConfiguration.Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected override CMState ProcessMarketDataTradeListRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                int i = 1;
                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    List<zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Trade> trades = RESTMarketDataManager.GetTrades(mdr.Security.Symbol);

                    foreach (zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Trade trade in trades)
                    {
                        BitMexTradeWrapper tradeWrapper = new BitMexTradeWrapper(trade, i==trades.Count);

                        OnMessageRcv(tradeWrapper);

                        i++;
                    }

                    return CMState.BuildSuccess();
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    throw new Exception(string.Format("@{0}: Market Data trade List SnapshotAndUpdates not implemented for symbol {1}", BitmexConfiguration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    throw new Exception(string.Format("@{0}: Market Data trade List Unsuscribe not implemented for symbol {1}", BitmexConfiguration.Name, mdr.Security.Symbol));

                    return CMState.BuildSuccess();
                }
                else
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", BitmexConfiguration.Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    DoLog("Initializing WSMarketDataManager @ BitMexInstructionBasedMarketClient",Main.Common.Util.Constants.MessageType.Information);
                    WSMarketDataManager = new MarketDataManager(BitmexConfiguration.WebsocketURL, true);
                    RESTMarketDataManager = new zHFT.InstructionBasedMarketClient.BitMex.DAL.MarketDataManager(BitmexConfiguration.RESTURL);
                    DoLog(string.Format("Connected: {0} - Error Message: {1}", WSMarketDataManager.AuthSubscriptionResult.Success, WSMarketDataManager.AuthSubscriptionResult.ErrorMessage), Main.Common.Util.Constants.MessageType.Information);
                    DoLog("Initializing SecurityListManager @ BitMexInstructionBasedMarketClient", Main.Common.Util.Constants.MessageType.Information);
                    SecurityListManager = new DAL.REST.SecurityListManager(BitmexConfiguration.RESTURL);

                    PriceLevelHandler = new PriceLevelHandler();

                    //Securities = SecurityListManager.GetActiveSecurityList();

                    //ProcessContractSize(Securities);
                    DoLog("Assigning events @ BitMexInstructionBasedMarketClient", Main.Common.Util.Constants.MessageType.Information);

                    OrderBookHandler = new OrderBookHandler();

                    WSMarketDataManager.SubscribeResponseRequest(
                                                BaseManager._ORDERBOOK_L2,
                                                OrderBookSubscriptionResponse,
                                                new object[] { });


                    WSMarketDataManager.SubscribeResponseRequest(
                                                         BaseManager._TRADE,
                                                         TradeSubscriptionResponse,
                                                         new object[] { });


                    WSMarketDataManager.SubscribeResponseRequest(
                                                        BaseManager._QUOTE,
                                                        QuoteSubscriptionResponse,
                                                        new object[] { });

                    WSMarketDataManager.SubscribeEvents(BaseManager._ORDERBOOK_L2, UpdateOrderBook);
                    WSMarketDataManager.SubscribeEvents(BaseManager._TRADE, UpdateTrade);
                    WSMarketDataManager.SubscribeEvents(BaseManager._QUOTE, UpdateQuotes);

                    ActiveSecuritiesQuotes = new Dictionary<int, Security>();
                    ActiveSecuritiesTrades = new Dictionary<int, Security>();
                    ActiveSecuritiesOrderBook = new Dictionary<int, Security>();
                    ContractsTimeStamps = new Dictionary<int, DateTime>();
                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critical error initializing " + configFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}
