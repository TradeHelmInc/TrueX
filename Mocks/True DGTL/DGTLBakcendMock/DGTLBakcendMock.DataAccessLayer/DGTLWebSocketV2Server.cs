using DGTLBackendMock.BusinessEntities.enums;
using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account;
using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Account.V2.Credit_UI;
using DGTLBackendMock.Common.DTO.Auth.V2;
using DGTLBackendMock.Common.DTO.Auth.V2.Credit_UI;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.MarketData.V2;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.OrderRouting.V2;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.SecurityList.V2;
using DGTLBackendMock.Common.DTO.Subscription;
using DGTLBackendMock.Common.DTO.Subscription.V2;
using DGTLBackendMock.Common.DTO.Temp.Positions;
using DGTLBackendMock.Common.Util;
using DGTLBackendMock.DataAccessLayer.Service;
using DGTLBackendMock.DataAccessLayer.Service.V2;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace DGTLBackendMock.DataAccessLayer
{
    public class DGTLWebSocketV2Server : DGTLWebSocketServer
    {
        #region Constructors

        public DGTLWebSocketV2Server(string pURL, string pRESTAdddress)
            : base(pURL, pRESTAdddress)
        {
            NotificationEmails = new Dictionary<string, string[]>();

            LoadTestEmails();

            LoadPositions();

            LoadFundedMargins();
        }

        #endregion

        #region Private Static Consts

        private static decimal _MAX_NOTIONAL_DEFAULT = 10000m;

        private static decimal _SECURITY_HALTING_THRESHOLD = 10m;

        private static decimal _MARKET_HALTING_THRESHOLD = 20m;

        private static decimal _MARKET_CLOSED_THRESHOLD = 30m;

        #endregion

        #region Protected Attributes

        protected ClientInstrumentBatch InstrBatch { get; set; }

        protected string LastTokenGenerated { get; set; }

        protected Thread HeartbeatThread { get; set; }

        protected bool SubscribedLQ { get; set; }

        public string LoggedFirmId { get; set; }

        public string LoggedUserId { get; set; }

        protected FirmsListResponse FirmListResp { get; set; }

        protected Dictionary<string, string[]> NotificationEmails { get; set; }

        protected ClientPosition[] Positions { get; set; }

        protected FundedMargin[] FundedMargins { get; set; }

        #endregion

        #region Private Methods

        private void LoadTestEmails()
        {
            NotificationEmails.Add("548346919", new string[] { "test548346919@test.com" });
            NotificationEmails.Add("271058668", new string[] { "test271058668@test.com" });
            NotificationEmails.Add("579693223", new string[] { "test579693223@test.com" });
        }

        private void LoadPositions()
        {
            string strPositions = File.ReadAllText(@".\input\Positions.json");

            Positions = JsonConvert.DeserializeObject<ClientPosition[]>(strPositions);
        
        }

        private void LoadFundedMargins()
        {
            string strFundedMargins = File.ReadAllText(@".\input\FundedMargins.json");

            FundedMargins = JsonConvert.DeserializeObject<FundedMargin[]>(strFundedMargins);

        }

        protected ClientOrderRecord[] GetAllOrders(DateTime from, DateTime to)
        {
            if (InstrBatch == null)
                InstrBatch = LoadInstrBatch();

            List<ClientOrderRecord> orderList = new List<ClientOrderRecord>();

            long epochFrom = Convert.ToInt64( (from - new DateTime(1970, 1, 1)).TotalMilliseconds);
            long epochTo = Convert.ToInt64((to - new DateTime(1970, 1, 1)).TotalMilliseconds);


            foreach(LegacyOrderRecord legOrder in Orders)
            {
                if (legOrder.UpdateTime >= epochFrom && legOrder.UpdateTime <= epochTo)
                {

                    ClientInstrument instr = GetInstrumentBySymbol(legOrder.InstrumentId);
                    ClientOrderRecord clOrder = TranslateOldLegacyOrderRecord(legOrder, instr, false, null);
                    orderList.Add(clOrder);
                }
            
            }

            return orderList.ToArray();
        
        }

        protected ClientTradeRecord[] GetAllTrades(DateTime from, DateTime to)
        {

            if(InstrBatch==null)
                InstrBatch = LoadInstrBatch();

            List<ClientTradeRecord> tradeList = new List<ClientTradeRecord>();

            long epochFrom = Convert.ToInt64((from - new DateTime(1970, 1, 1)).TotalMilliseconds);
            long epochTo = Convert.ToInt64((to - new DateTime(1970, 1, 1)).TotalMilliseconds);

            foreach (LegacyTradeHistory trade in Trades)
            {

                if (trade.TradeTimeStamp >= epochFrom && trade.TradeTimeStamp <= epochTo)
                {
                    ClientInstrument instr = GetInstrumentBySymbol(trade.Symbol);
                    ClientTradeRecord clTrade = TranslateOldLegacyTradeHistory(trade, instr, null);
                    tradeList.Add(clTrade);
                }
            }

            return tradeList.ToArray();
        }

        protected override void LoadHistoryService()
        {
            string url = RESTURL;

            try
            {

                DoLog(string.Format("Creating history service for controller HistoryServiceController on URL {0}", url),
                      MessageType.Information);

                HttpSelfHostConfiguration config = new HttpSelfHostConfiguration(url);

                config.Routes.MapHttpRoute(name: "DefaultApi",
                                           routeTemplate: "{controller}/{id}",
                                           defaults: new {  id = RouteParameter.Optional });


                historyController.OnLog += DoLog;
                historyController.OverridenGet += historyControllerV2.Get;
                historyControllerV2.OnLog += DoLog;
                historyControllerV2.OnGetAllTrades += GetAllTrades;
                historyControllerV2.OnGetAllOrders += GetAllOrders;

                Server = new HttpSelfHostServer(config);
                Server.OpenAsync().Wait();
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                Exception innerEx = ex.InnerException;

                while (innerEx != null)
                {
                    error += "-" + innerEx.Message;
                    innerEx = innerEx.InnerException;
                }


                DoLog(string.Format("Critical error creating history service for controller HistoryServiceController on URL {0}:{1}",
                      url, error), MessageType.Error);
            }


        }


        protected void CanceledLegacyOrderRecord(IWebSocketConnection socket, LegacyOrderRecord ordRecord,string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            LegacyOrderRecord OyMsg = new LegacyOrderRecord();

            OyMsg.ClientOrderId = ordRecord.ClientOrderId;
            OyMsg.cSide = ordRecord.cSide;
            OyMsg.cStatus = LegacyOrderRecord._STATUS_CANCELED;
            OyMsg.cTimeInForce = LegacyOrderRecord._TIMEINFORCE_DAY;
            OyMsg.FillQty = ordRecord.FillQty;
            OyMsg.InstrumentId = ordRecord.InstrumentId;
            OyMsg.LvsQty = 0;
            OyMsg.Msg = "LegacyOrderRecord";
            //OyMsg.OrderId = Guid.NewGuid().ToString();
            OyMsg.OrderId = ordRecord.OrderId;
            OyMsg.OrdQty = Convert.ToDouble(ordRecord.OrdQty);
            OyMsg.Price = (double?)ordRecord.Price;
            OyMsg.Sender = 0;
            OyMsg.UpdateTime = Convert.ToInt64(elapsed.TotalMilliseconds);
            OyMsg.UserId = ordRecord.UserId;

            TranslateAndSendOldLegacyOrderRecord(socket, UUID, OyMsg, newOrder: false);
            TranslateAndSendOldLegacyOrderRecordToMyOrders(socket, UUID, OyMsg, newOrder: false);
        }

        private void RefreshOpenOrders(IWebSocketConnection socket, string userId,string UUID)
        {
            TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

            try
            {
                int openOrdersCount = Orders.Where(x => x.cStatus == LegacyOrderRecord._STATUS_OPEN /*|| x.cStatus == LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/).ToList().Count;

                DoLog(string.Format("Sending open order count for all symbols : {0}", openOrdersCount), MessageType.Information);

                ClientOrderCount openOrders = new ClientOrderCount()
                {
                    //Msg = "OrderCount",
                    Msg = "ClientOrderCount",
                    UserId=userId,
                    TimeStamp=Convert.ToInt64(elaped.TotalMilliseconds).ToString(),
                    Uuid=UUID,
                    Count=openOrdersCount
                };

                DoSend<ClientOrderCount>(socket, openOrders);
               
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception on RefreshOpenOrders: {0}", ex.Message), MessageType.Error);
            }
        }

         //1.2.3-Update and send ClientCreditUpdate
         private void  UpdateCredit(IWebSocketConnection socket, LegacyTradeHistory newTrade, string UUID)
         {
             if (FirmListResp == null)
                 CreateFirmListCreditStructure(UUID, 0, 10000);

             FirmsCreditRecord firm = FirmListResp.Firms.Where(x => x.FirmId.ToString() == LoggedFirmId).FirstOrDefault();

             if (firm != null)
             {
                 double prevUsedCredit = firm.UsedCredit;
                 firm.UsedCredit = GetUsedCredit(LoggedFirmId);
                 firm.AvailableCredit -= firm.UsedCredit-prevUsedCredit;

                 TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                 ClientCreditUpdate ccUpd = new ClientCreditUpdate()
                 {
                     Msg = "ClientCreditUpdate",
                     AccountId = 0,
                     CreditLimit = firm.AvailableCredit + firm.UsedCredit,
                     CreditUsed = firm.UsedCredit,
                     BuyExposure = GetTotalSideExposure(LegacyOrderRecord._SIDE_BUY),
                     SellExposure = GetTotalSideExposure(LegacyOrderRecord._SIDE_SELL),
                     cStatus = firm.cTradingStatus,
                     cUpdateReason = ClientCreditUpdate._UPDATE_REASON_DEFAULT,
                     FirmId = Convert.ToInt64(firm.FirmId),
                     MaxNotional = firm.MaxNotional,
                     Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                     Uuid = UUID
                 };

                 DoSend<ClientCreditUpdate>(socket, ccUpd);
             }
         }

        private void SendTradeNotification(IWebSocketConnection socket, LegacyTradeHistory trade, string userId, ClientInstrument instr, string UUID)
        {
            //if (NotificationsSubscriptions.Contains(trade.Symbol))
            //{
                DoLog(string.Format("We send a Trade Notification for Size={0} and Price={1} for symbol {2}", trade.TradeQuantity, trade.TradePrice, trade.Symbol), MessageType.Information);
                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);


                ClientNotification notif = new ClientNotification()
                {
                    Msg = "ClientNotification",
                    InstrumentId = instr.InstrumentId.ToString(),
                    cSide = trade.cMySide,
                    Price = trade.TradePrice,
                    Size = trade.TradeQuantity,
                    UserId=userId,
                    ExecId=trade.TradeId,
                    Uuid=UUID,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds).ToString(),
                    
                    

                };

                DoSend<ClientNotification>(socket, notif);
            //}
            //else
            //    DoLog(string.Format("Not sending Trade Notification for Size={0} and Price={1} for symbol {2} because it was not subscribed", trade.TradeQuantity, trade.TradePrice, trade.Symbol), MessageType.Information);


        }

        private void EvalPriceLevels(IWebSocketConnection socket, ClientOrderRecord order,string UUID)
        {

            ClientInstrument instr = GetInstrumentByIntInstrumentId(order.InstrumentId);
            if (!order.Price.HasValue)
                return;

            TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

            if (order.cSide == LegacyOrderReq._SIDE_BUY)
            {
                DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._BID_ENTRY
                                                                       && x.Symbol == instr.InstrumentName
                                                                       && x.Price == Convert.ToDecimal(order.Price.Value)).FirstOrDefault();

                if (existingPriceLevel != null)
                {
                    decimal newSize = existingPriceLevel.Size - Convert.ToDecimal(order.LeavesQty);

                    DepthOfBook updPriceLevel = new DepthOfBook();
                    updPriceLevel.cAction = newSize > 0 ? DepthOfBook._ACTION_CHANGE : DepthOfBook._ACTION_REMOVE;
                    updPriceLevel.cBidOrAsk = DepthOfBook._BID_ENTRY;
                    updPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                    updPriceLevel.Index = existingPriceLevel.Index;
                    updPriceLevel.Msg = existingPriceLevel.Msg;
                    updPriceLevel.Price = existingPriceLevel.Price;
                    updPriceLevel.Sender = existingPriceLevel.Sender;
                    updPriceLevel.Size = newSize;
                    updPriceLevel.Symbol = existingPriceLevel.Symbol;

                    existingPriceLevel.Size = updPriceLevel.Size;

                    if (updPriceLevel.cAction == DepthOfBook._ACTION_REMOVE)
                        DepthOfBooks.Remove(existingPriceLevel);

                    DoLog(string.Format("Sending upd bid entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
                    TranslateAndSendOldDepthOfBook(socket, updPriceLevel, instr, UUID);
                    //DoSend<DepthOfBook>(socket, updPriceLevel);
                }
                else
                {

                    throw new Exception(string.Format("Critical Error: Cancelling an order for an unexisting bid price level: {0}", order.Price.Value));
                }

            }
            else if (order.cSide == LegacyOrderReq._SIDE_SELL)
            {
                DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._ASK_ENTRY
                                                                       && x.Symbol == instr.InstrumentName
                                                                       && x.Price == Convert.ToDecimal(order.Price.Value)).FirstOrDefault();

                if (existingPriceLevel != null)
                {
                    decimal newSize = existingPriceLevel.Size - Convert.ToDecimal(order.LeavesQty);

                    DepthOfBook updPriceLevel = new DepthOfBook();
                    updPriceLevel.cAction = newSize > 0 ? DepthOfBook._ACTION_CHANGE : DepthOfBook._ACTION_REMOVE;
                    updPriceLevel.cBidOrAsk = DepthOfBook._ASK_ENTRY;
                    updPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                    updPriceLevel.Index = existingPriceLevel.Index;
                    updPriceLevel.Msg = existingPriceLevel.Msg;
                    updPriceLevel.Price = existingPriceLevel.Price;
                    updPriceLevel.Sender = existingPriceLevel.Sender;
                    updPriceLevel.Size = existingPriceLevel.Size - Convert.ToDecimal(order.LeavesQty);
                    updPriceLevel.Symbol = existingPriceLevel.Symbol;

                    existingPriceLevel.Size = updPriceLevel.Size;

                    if (updPriceLevel.cAction == DepthOfBook._ACTION_REMOVE)
                        DepthOfBooks.Remove(existingPriceLevel);

                    DoLog(string.Format("Sending upd ask entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
                    TranslateAndSendOldDepthOfBook(socket, updPriceLevel, instr, UUID);
                    //DoSend<DepthOfBook>(socket, updPriceLevel);
                }
                else
                {
                    throw new Exception(string.Format("Critical Error: Cancelling an order for an unexisting ask price level: {0}", order.Price.Value));
                }
            }
        }

        private char GetStateOnSymbol(ClientInstrument instr)
        {
            if (instr.InstrumentName == "ADA-USD")
                return ClientInstrumentState._STATE_CLOSE;
            else if (instr.InstrumentName == "XMR-USD")
                return ClientInstrumentState._STATE_INACTIVE;
            else if (instr.InstrumentName == "XRP-USD")
                return ClientInstrumentState._STATE_UNKNOWN;
            else
                return ClientInstrumentState._STATE_HALT;
        
        
        }

        //1.2.1.1- If change is more than 5% , we halt the security
        //         If change is more than 20% , we halt the market
        //         If change is more than 30% , we close the market
        private void EvalHaltingSecurityOrMarket(IWebSocketConnection socket, decimal? prevLastPrice, LegacyTradeHistory newTrade, ClientInstrument instr)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            if (prevLastPrice.HasValue)
            {

                decimal pctChange = ((Convert.ToDecimal(newTrade.TradePrice) / prevLastPrice.Value) - 1) * 100;
                if(pctChange<0)
                    pctChange*=-1;

                if (pctChange > _SECURITY_HALTING_THRESHOLD && pctChange < _MARKET_HALTING_THRESHOLD) 
                {//we halt the security

                    ClientInstrumentState state = new ClientInstrumentState();
                    state.Msg = "ClientInstrumentState";
                    state.cState = GetStateOnSymbol(instr);
                    state.ExchangeId = "0";
                    state.InstrumentId = instr.InstrumentId;
                    state.cReasonCode = ClientInstrumentState._REASON_CODE_2;
                    state.TriggerPrice = newTrade.TradePrice;

                    DoLog(string.Format("Halting security {0} because of price change {1} % ",instr.InstrumentName,pctChange), MessageType.Information);
                    DoSend<ClientInstrumentState>(socket, state);
                }
                else if (pctChange > _MARKET_HALTING_THRESHOLD && pctChange < _MARKET_CLOSED_THRESHOLD)
                {//we halt the MARKET

                    ClientMarketState marketStateMsg = new ClientMarketState();
                    marketStateMsg.ExchangeId = ClientMarketState._DEFAULT_EXCHANGE_ID;
                    marketStateMsg.cReasonCode = '0';
                    marketStateMsg.cState = ClientMarketState._MARKET_CLOSED;
                    marketStateMsg.Msg = "ClientMarketState";
                    marketStateMsg.StateTime = Convert.ToInt64(epochElapsed.TotalMilliseconds);

                    DoLog(string.Format("Halting market because of price change {0} % ", pctChange), MessageType.Information);
                    DoSend<ClientMarketState>(socket, marketStateMsg);
                }
                else if (pctChange > _MARKET_CLOSED_THRESHOLD)
                {//we halt the MARKET

                    ClientMarketState marketStateMsg = new ClientMarketState();
                    marketStateMsg.ExchangeId = ClientMarketState._DEFAULT_EXCHANGE_ID;
                    marketStateMsg.cReasonCode = '0';
                    marketStateMsg.cState = ClientMarketState._SYSTEM_CLOSED;
                    marketStateMsg.Msg = "ClientMarketState";
                    marketStateMsg.StateTime = Convert.ToInt64(epochElapsed.TotalMilliseconds);

                    DoLog(string.Format("Closing market because of price change {0} % ", pctChange), MessageType.Information);
                    DoSend<ClientMarketState>(socket, marketStateMsg);
                }

            }
        
        
        }

        //1.2.1-We update market data for a new trade
        private void EvalMarketData(IWebSocketConnection socket, LegacyTradeHistory newTrade,ClientInstrument instr,string UUID)
        {

            DoLog(string.Format("We update LastSale MD for Size={0} and Price={1} for symbol {2}", newTrade.TradeQuantity, newTrade.TradePrice, newTrade.Symbol), MessageType.Information);

            LastSale ls = LastSales.Where(x => x.Symbol == newTrade.Symbol).FirstOrDefault();

            if (ls == null)
            {
                ls = new LastSale() { Change = 0, DiffPrevDay = 0, Msg = "LastSale", Symbol = newTrade.Symbol, Volume = 0 };
                LastSales.Add(ls);

            }

            if (!ls.FirstPrice.HasValue)
                ls.FirstPrice = ls.LastPrice;

            decimal? prevLastPrice = ls.LastPrice;
            ls.LastPrice = Convert.ToDecimal(newTrade.TradePrice);
            ls.LastShares = Convert.ToDecimal(newTrade.TradeQuantity);
            ls.LastTime = newTrade.TradeTimeStamp;
            ls.Volume += Convert.ToDecimal(newTrade.TradeQuantity);
            ls.Change = ls.LastPrice - ls.FirstPrice;
            ls.DiffPrevDay = ((ls.LastPrice / ls.FirstPrice) - 1) ;

            if (!ls.High.HasValue || Convert.ToDecimal(newTrade.TradePrice) > ls.High)
                ls.High = Convert.ToDecimal(newTrade.TradePrice);

            if (!ls.Low.HasValue || Convert.ToDecimal(newTrade.TradePrice) < ls.Low)
                ls.Low = Convert.ToDecimal(newTrade.TradePrice);

            DoLog(string.Format("LastSale updated..."), MessageType.Information);


            DoLog(string.Format("We udate Quotes MD for Size={0} and Price={1} for symbol {2}", newTrade.TradeQuantity, newTrade.TradePrice, newTrade.Symbol), MessageType.Information);
            UpdateQuotes(socket, instr, UUID);
            DoLog(string.Format("Quotes updated..."), MessageType.Information);

            EvalHaltingSecurityOrMarket(socket, prevLastPrice, newTrade, instr);
        }


        //1.1-We eliminate the price level (@DepthOfBook and @Orders and we send the message)
        private void RemovePriceLevel(DepthOfBook bestOffer, char bidOrAsk, IWebSocketConnection socket,ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            DoLog(string.Format("We eliminate the {2} price level {0} for symbol{1} (@DepthOfBook and @Orders and we send the message)",
                bestOffer.Price, bestOffer.Symbol, bidOrAsk == DepthOfBook._BID_ENTRY ? "bid" : "ask"), MessageType.Information);


            List<LegacyOrderRecord> updOrders = Orders.Where(x => x.InstrumentId == bestOffer.Symbol
                                                                  && x.Price.Value == Convert.ToDouble(bestOffer.Price)
                                                                  && x.cSide == (bidOrAsk == DepthOfBook._BID_ENTRY ? LegacyOrderRecord._SIDE_BUY : LegacyOrderRecord._SIDE_SELL)).ToList();

            foreach (LegacyOrderRecord order in updOrders)
            {
                order.SetFilledStatus();
                TranslateAndSendOldLegacyOrderRecord(socket, UUID, order);
                TranslateAndSendOldLegacyOrderRecordToMyOrders(socket, UUID, order);

                if (order.Price.HasValue)
                {
                    DoLog(string.Format("We send the Trade for my other affected filled order for symbol {0} Side={1} Price={2} ", order.InstrumentId, order.cSide == LegacyOrderRecord._SIDE_BUY ? "buy" : "sell", order.Price.Value), MessageType.Information);
                    SendNewTrade(order.cSide, Convert.ToDecimal(order.Price.Value), Convert.ToDecimal(order.OrdQty), socket, instr, UUID);
                }
            }


            DepthOfBooks.Remove(bestOffer);

            DoLog(string.Format("We send the DepthOfBookMessage removing the {2} price level {0} for symbol{1} (@DepthOfBook and @Orders and we send the message)",
                  bestOffer.Price, bestOffer.Symbol, bidOrAsk == DepthOfBook._BID_ENTRY ? "bid" : "ask"), MessageType.Information);
            DepthOfBook remPriceLevel = new DepthOfBook();
            remPriceLevel.cAction = DepthOfBook._ACTION_REMOVE;
            remPriceLevel.cBidOrAsk = bidOrAsk;
            remPriceLevel.DepthTime = Convert.ToInt64(elapsed.TotalMilliseconds);
            remPriceLevel.Index = 0;
            remPriceLevel.Msg = "DepthOfBook";
            remPriceLevel.Price = bestOffer.Price;
            remPriceLevel.Sender = 0;
            remPriceLevel.Size = 0;
            remPriceLevel.Symbol = bestOffer.Symbol;

            TranslateAndSendOldDepthOfBook(socket, remPriceLevel, instr, UUID);
            //DoSend<DepthOfBook>(socket, remPriceLevel);
        }

        //1.2- We send a Trade by <size>
        private void SendNewTrade(char side, decimal price, decimal size, IWebSocketConnection socket, ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            DoLog(string.Format("We send a Trade for Size={0} and Price={1} for symbol {2}", size, price, instr.InstrumentName), MessageType.Information);

            LegacyTradeHistory newTrade = new LegacyTradeHistory()
            {
                cMySide = side,
                Msg = "LegacyTradeHistory",
                MyTrade = true,
                Sender = 0,
                Symbol = instr.InstrumentName,
                TradeId = Guid.NewGuid().ToString(),
                TradePrice = Convert.ToDouble(price),
                TradeQuantity = Convert.ToDouble(size),
                TradeTimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds)

            };
            TranslateAndSendOldLegacyTradeHistory(socket, UUID, newTrade);//rt
            TranslateAndSendOldLegacyTradeHistoryToMarketActivity(socket, UUID, newTrade);//MA
            AppendTrades(newTrade);
            //DoSend<LegacyTradeHistory>(socket, newTrade);

            //1.2.1-We update market data for a new trade
            EvalMarketData(socket, newTrade, instr, UUID);
            
            //1.2.2-We send a trade notification for the new trade
            SendTradeNotification(socket, newTrade, LoggedUserId, instr, UUID);

            //1.2.3-Update and send ClientCreditUpdate
            //UpdateCredit(socket, newTrade, UUID);

        }

        //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
        private void UpdatePriceLevel(ref DepthOfBook bestOffer, char bidOrAsk, double tradeSize, IWebSocketConnection socket, ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            string symbol = bestOffer.Symbol;
            double bestOfferPrice = Convert.ToDouble(bestOffer.Price);

            DoLog(string.Format("We update the {2} price level {0} for symbol {1} (@DepthOfBook and @Orders and we send the message)",
                                bestOffer.Price, bestOffer.Symbol, bidOrAsk == DepthOfBook._BID_ENTRY ? "bid" : "ask"), MessageType.Information);

            bestOffer.Size -= Convert.ToDecimal(tradeSize);
            bestOffer.cAction = DepthOfBook._ACTION_CHANGE;

            DoLog(string.Format("Searching for affecter orders for symbol {0}", bestOffer.Symbol), MessageType.Information);

            List<LegacyOrderRecord> updOrders = Orders.Where(x => x.InstrumentId == symbol
                                                                 && x.Price.Value == bestOfferPrice
                                                                 && x.cSide == (bidOrAsk == DepthOfBook._BID_ENTRY ? LegacyOrderRecord._SIDE_BUY : LegacyOrderRecord._SIDE_SELL)).ToList();

            foreach (LegacyOrderRecord order in updOrders)
            {
                DoLog(string.Format("Updating order status for price level {0}", order.Price.Value), MessageType.Information);

                double prevFillQty = order.FillQty;
                order.SetPartiallyFilledStatus(ref tradeSize);
                TranslateAndSendOldLegacyOrderRecord(socket, UUID, order);
                TranslateAndSendOldLegacyOrderRecordToMyOrders(socket, UUID, order);
                //DoSend<LegacyOrderRecord>(socket, order);

                if (order.Price.HasValue)
                {
                    DoLog(string.Format("We send the Trade for my other affected {3} order for symbol {0} Side={1} Price={2} Trade Size={4} ", order.InstrumentId, order.cSide == LegacyOrderRecord._SIDE_BUY ? "buy" : "sell", order.Price.Value, order.cStatus, Convert.ToDecimal(order.FillQty - prevFillQty)), MessageType.Information);
                    SendNewTrade(order.cSide, Convert.ToDecimal(order.Price.Value), Convert.ToDecimal(order.FillQty - prevFillQty), socket, instr, UUID);
                }

                if (tradeSize <= 0)
                    break;
            }


            DoLog(string.Format("We send the DepthOfBook Message updating the ask price level {0} for symbol {1} (@DepthOfBook and @Orders and we send the message)", bestOffer.Price, bestOffer.Symbol), MessageType.Information);

            TranslateAndSendOldDepthOfBook(socket, bestOffer, instr, UUID);
        }

        private void CreatePriceLevel(char side, decimal price, decimal size, IWebSocketConnection socket, ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            DepthOfBook newPriceLevel = new DepthOfBook();
            newPriceLevel.cAction = DepthOfBook._ACTION_INSERT;
            newPriceLevel.cBidOrAsk = side;
            newPriceLevel.DepthTime = Convert.ToInt64(elapsed.TotalMilliseconds);
            newPriceLevel.Index = 0;
            newPriceLevel.Msg = "DepthOfBook";
            newPriceLevel.Price = price;
            newPriceLevel.Sender = 0;
            newPriceLevel.Size = size;
            newPriceLevel.Symbol = instr.InstrumentName;


            DepthOfBooks.Add(newPriceLevel);

            TranslateAndSendOldDepthOfBook(socket, newPriceLevel, instr, UUID);
            //DoSend<DepthOfBook>(socket, newPriceLevel);
        }

        private void EvalNewOrder(IWebSocketConnection socket, ClientOrderReq ordReq,string orderId, char cStatus, double fillQty, ClientInstrument instr, string UUID)
        {
            lock (Orders)
            {
                TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

                LegacyOrderRecord OyMsg = new LegacyOrderRecord();

                OyMsg.ClientOrderId = ordReq.ClientOrderId;
                OyMsg.cSide = ordReq.cSide;
                OyMsg.cStatus = cStatus;
                OyMsg.cTimeInForce = LegacyOrderRecord._TIMEINFORCE_DAY;
                OyMsg.FillQty = fillQty;
                OyMsg.InstrumentId = instr.InstrumentName;
                OyMsg.LvsQty = Convert.ToDouble(ordReq.Quantity) - fillQty;
                OyMsg.Msg = "LegacyOrderRecord";
                OyMsg.OrderId = orderId.ToString();
                OyMsg.OrdQty = Convert.ToDouble(ordReq.Quantity);
                OyMsg.Price = (double?)ordReq.Price;
                OyMsg.Sender = 0;
                OyMsg.UpdateTime = Convert.ToInt64(elaped.TotalMilliseconds);
                OyMsg.UserId = ordReq.UserId;

                DoLog(string.Format("brkpnt Creating new order in Orders collection for OrderId = {0}", OyMsg.OrderId), MessageType.Information);


                TranslateAndSendOldLegacyOrderRecord(socket, UUID, OyMsg);
                TranslateAndSendOldLegacyOrderRecordToMyOrders(socket, UUID, OyMsg);

                DoLog(string.Format("Creating new order in Orders collection for OrderId = {0}", OyMsg.OrderId), MessageType.Information);
                Orders.Add(OyMsg);

                RefreshOpenOrders(socket, OyMsg.UserId,UUID);

            }
        }

        private void EvalPriceLevelsIfNotTrades(IWebSocketConnection socket, ClientOrderReq ordReq, ClientInstrument instr)
        {
            lock (Orders)
            {
                TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

                if (ordReq.cSide == LegacyOrderReq._SIDE_BUY)
                {
                    DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._BID_ENTRY
                                                                           && x.Symbol == instr.InstrumentName
                                                                           && x.Price == ordReq.Price).FirstOrDefault();

                    if (existingPriceLevel != null)
                    {

                        DepthOfBook updPriceLevel = new DepthOfBook();
                        updPriceLevel.cAction = DepthOfBook._ACTION_CHANGE;
                        updPriceLevel.cBidOrAsk = DepthOfBook._BID_ENTRY;
                        updPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                        updPriceLevel.Index = existingPriceLevel.Index;
                        updPriceLevel.Msg = existingPriceLevel.Msg;
                        updPriceLevel.Price = existingPriceLevel.Price;
                        updPriceLevel.Sender = existingPriceLevel.Sender;
                        updPriceLevel.Size = existingPriceLevel.Size + ordReq.Quantity;
                        updPriceLevel.Symbol = existingPriceLevel.Symbol;

                        existingPriceLevel.Size = updPriceLevel.Size;
                        DoLog(string.Format("Sending upd bid entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
                        TranslateAndSendOldDepthOfBook(socket, updPriceLevel, instr, ordReq.Uuid);
                    }
                    else
                    {

                        DepthOfBook newPriceLevel = new DepthOfBook();
                        newPriceLevel.cAction = DepthOfBook._ACTION_INSERT;
                        newPriceLevel.cBidOrAsk = DepthOfBook._BID_ENTRY;
                        newPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                        newPriceLevel.Index = 0;
                        newPriceLevel.Msg = "DepthOfBook";
                        newPriceLevel.Price = ordReq.Price.HasValue ? ordReq.Price.Value : 0;
                        newPriceLevel.Sender = 0;
                        newPriceLevel.Size = ordReq.Quantity;
                        newPriceLevel.Symbol = instr.InstrumentName;

                        DepthOfBooks.Add(newPriceLevel);
                        DoLog(string.Format("Sending new bid entry: Price={0} Size={1} ...", newPriceLevel.Price, newPriceLevel.Size), MessageType.Information);
                        TranslateAndSendOldDepthOfBook(socket, newPriceLevel, instr, ordReq.Uuid);
                    }

                }
                else if (ordReq.cSide == LegacyOrderReq._SIDE_SELL)
                {
                    DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._ASK_ENTRY
                                                                           && x.Symbol == instr.InstrumentName
                                                                           && x.Price == ordReq.Price).FirstOrDefault();

                    if (existingPriceLevel != null)
                    {

                        DepthOfBook updPriceLevel = new DepthOfBook();
                        updPriceLevel.cAction = DepthOfBook._ACTION_CHANGE;
                        updPriceLevel.cBidOrAsk = DepthOfBook._ASK_ENTRY;
                        updPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                        updPriceLevel.Index = existingPriceLevel.Index;
                        updPriceLevel.Msg = existingPriceLevel.Msg;
                        updPriceLevel.Price = existingPriceLevel.Price;
                        updPriceLevel.Sender = existingPriceLevel.Sender;
                        updPriceLevel.Size = existingPriceLevel.Size + ordReq.Quantity;
                        updPriceLevel.Symbol = existingPriceLevel.Symbol;

                        existingPriceLevel.Size = updPriceLevel.Size;
                        DoLog(string.Format("Sending upd ask entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
                        TranslateAndSendOldDepthOfBook(socket, updPriceLevel, instr, ordReq.Uuid);
                    }
                    else
                    {

                        DepthOfBook newPriceLevel = new DepthOfBook();
                        newPriceLevel.cAction = DepthOfBook._ACTION_INSERT;
                        newPriceLevel.cBidOrAsk = DepthOfBook._ASK_ENTRY;
                        newPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                        newPriceLevel.Index = 0;
                        newPriceLevel.Msg = "DepthOfBook";
                        newPriceLevel.Price = ordReq.Price.HasValue ? ordReq.Price.Value : 0;
                        newPriceLevel.Sender = 0;
                        newPriceLevel.Size = ordReq.Quantity;
                        newPriceLevel.Symbol = instr.InstrumentName;

                        DepthOfBooks.Add(newPriceLevel);
                        DoLog(string.Format("Sending new ask entry: Price={0} Size={1} ...", newPriceLevel.Price, newPriceLevel.Size), MessageType.Information);
                        TranslateAndSendOldDepthOfBook(socket, newPriceLevel, instr, ordReq.Uuid);
                    }

                }
            }
        }

        private bool EvalTrades(ClientOrderReq orderReq, ClientOrderResponse clientOrdAck, ClientInstrument instr, string UUID, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            lock (Orders)
            {
                //1- Main algo for updating price levels and publish the trades
                if (orderReq.cSide == LegacyOrderReq._SIDE_BUY)
                {
                    DepthOfBook bestAsk = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName &&
                                                                 x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();

                    if (bestAsk == null)
                        return false;

                    DoLog(string.Format("Best ask for buy order (Limit Price={1}) found at price {0}", bestAsk.Price, orderReq.Price.Value), MessageType.Information);
                    if (orderReq.Price.HasValue && orderReq.Price.Value >= bestAsk.Price)
                    {
                        //we had a trade
                        decimal leftQty = orderReq.Quantity;

                        bool fullFill = false;
                        while (leftQty > 0 && bestAsk != null && orderReq.Price.Value >= bestAsk.Price)
                        {
                            decimal prevLeftQty = leftQty;
                            leftQty -= bestAsk.Size;

                            if (leftQty >= 0)
                            {
                                //1.1-We eliminate the price level (@DepthOfBook and @Orders and we send the message)
                                RemovePriceLevel(bestAsk, DepthOfBook._ASK_ENTRY, socket, instr, UUID);

                                //1.2- We send a Trade by bestAsk.Size
                                SendNewTrade(orderReq.cSide, bestAsk.Price, bestAsk.Size, socket, instr, UUID);


                                LegacyTradeHistory newTrade = new LegacyTradeHistory() { cMySide = LegacyTradeHistory._SIDE_BUY, TradeQuantity = Convert.ToDouble(bestAsk.Size), TradePrice = Convert.ToDouble(bestAsk.Price), Symbol = instr.InstrumentName };

                                //1.2.bis - We create the position
                                CreatePosition(newTrade,LoggedUserId);

                                //1.3 - We update the credit used
                                //UpdateCredit(socket, newTrade, UUID);


                                //1.4-Calculamos el nuevo bestAsk
                                bestAsk = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName
                                                                 && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();


                                fullFill = leftQty == 0;

                            }
                            else//order fully filled
                            {
                                //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
                                UpdatePriceLevel(ref bestAsk, DepthOfBook._ASK_ENTRY, Convert.ToDouble(prevLeftQty), socket, instr, UUID);

                                

                                //1.2- We send a trade by prevLeftQty
                                SendNewTrade(orderReq.cSide, bestAsk.Price, prevLeftQty, socket, instr, UUID);

                                LegacyTradeHistory newTrade = new LegacyTradeHistory() { cMySide = LegacyTradeHistory._SIDE_BUY, TradeQuantity = Convert.ToDouble(prevLeftQty), TradePrice = Convert.ToDouble(bestAsk.Price), Symbol = instr.InstrumentName };

                                //1.2.bis - We create the position
                                CreatePosition(newTrade, LoggedUserId);

                                //1.3 - We update the credit used
                                //UpdateCredit(socket, newTrade, UUID);

                                fullFill = true;
                            }

                            bestAsk = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();
                        }

                        if (leftQty > 0)
                        {
                            DoLog(string.Format("We create the remaining buy price level {1}  for size {0}", leftQty, orderReq.Price.Value), MessageType.Information);

                            //2-We send the new price level for the remaining order
                            CreatePriceLevel(DepthOfBook._BID_ENTRY, orderReq.Price.Value, leftQty, socket,instr,UUID);
                        }
                        else
                        {
                            DoLog(string.Format("Final leftQty=0 out of loop for buy order at price level {0}", orderReq.Price.Value), MessageType.Information);
                        }


                        //3-We send the full fill/partiall fill for the order --> Oy:LegacyOrderRecord
                        EvalNewOrder(socket, orderReq,clientOrdAck.OrderId,
                                     fullFill ? LegacyOrderRecord._STATUS_FILLED : /*LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/LegacyOrderRecord._STATUS_OPEN,
                                     Convert.ToDouble(fullFill ? orderReq.Quantity : orderReq.Quantity - leftQty),
                                     instr,UUID);

                        //4-We update the final quotes state
                        UpdateQuotes(socket,instr,UUID);

                        //5-we update the credit for potential exposures
                        UpdateCredit(socket, null, UUID);

                        //6-we update the account balance (margin tab) for the firm
                        DoSendAccountBalance(socket, UUID);

                        return true;
                    }
                    else
                    {
                        DoLog(string.Format("Could not find matching sell price for symbol {0} and order price {1}", instr.InstrumentName, orderReq.Price), MessageType.Information);
                        return false;
                    }
                }
                else if (orderReq.cSide == LegacyOrderReq._SIDE_SELL)
                {
                    DepthOfBook bestBid = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName
                                                                  && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();

                    if (bestBid == null)
                        return false;

                    DoLog(string.Format("Best bid for sell order (Limit Price={1}) found at price {0}", bestBid.Price, orderReq.Price.Value), MessageType.Information);
                    if (orderReq.Price.HasValue && orderReq.Price.Value <= bestBid.Price)
                    {
                        //we had a trade
                        decimal leftQty = orderReq.Quantity;

                        bool fullFill = false;
                        while (leftQty > 0 && bestBid != null && orderReq.Price.Value <= bestBid.Price)
                        {
                            decimal prevLeftQty = leftQty;
                            leftQty -= bestBid.Size;

                            if (leftQty >= 0)
                            {
                                //1.1-We eliminate the price level (@DepthOfBook and @Orders and we send the message)
                                RemovePriceLevel(bestBid, DepthOfBook._BID_ENTRY, socket, instr, UUID);

                                //1.2- We send a Trade by bestBid.Size
                                SendNewTrade(orderReq.cSide, bestBid.Price, bestBid.Size, socket, instr, UUID);

                                LegacyTradeHistory newTrade = new LegacyTradeHistory() { cMySide=LegacyTradeHistory._SIDE_SELL, TradeQuantity = Convert.ToDouble(bestBid.Size), TradePrice = Convert.ToDouble(bestBid.Price), Symbol = instr.InstrumentName };

                                //1.2.bis - We create the position
                                CreatePosition(newTrade, LoggedUserId);

                                //1.3 - We update the credit used
                                //UpdateCredit(socket, newTrade, UUID);


                                //1.4-Calculamos el nuevo bestBid
                                bestBid = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName
                                                                 && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();

                                fullFill = leftQty == 0;

                            }
                            else//order fully filled
                            {
                                //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
                                UpdatePriceLevel(ref bestBid, DepthOfBook._BID_ENTRY, Convert.ToDouble(prevLeftQty), socket,instr,UUID);

                                //1.2- We send a trade by prevLeftQty
                                SendNewTrade(orderReq.cSide, bestBid.Price, prevLeftQty, socket,instr, UUID);

                                LegacyTradeHistory newTrade = new LegacyTradeHistory() { cMySide = LegacyTradeHistory._SIDE_SELL, TradeQuantity = Convert.ToDouble(prevLeftQty), TradePrice = Convert.ToDouble(bestBid.Price), Symbol = instr.InstrumentName };

                                //1.2.bis - We create the position
                                CreatePosition(newTrade, LoggedUserId);

                                fullFill = true;
                            }

                            bestBid = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();
                        }

                        if (leftQty > 0)
                        {
                            DoLog(string.Format("We create the remaining buy price level {1}  for size {0}", leftQty, orderReq.Price.Value), MessageType.Information);

                            //2-We send the new price level for the remaining order
                            CreatePriceLevel(DepthOfBook._ASK_ENTRY, orderReq.Price.Value, leftQty, socket,instr,UUID);
                        }
                        else
                        {
                            DoLog(string.Format("Final leftQty=0 out of loop for sell order at price level {0}", orderReq.Price.Value), MessageType.Information);
                        }

                        //3-We send the full fill/partiall fill for the order --> Oy:LegacyOrderRecord
                        EvalNewOrder(socket, orderReq,clientOrdAck.OrderId,
                                     fullFill ? LegacyOrderRecord._STATUS_FILLED : /* LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/ LegacyOrderRecord._STATUS_OPEN,
                                     Convert.ToDouble(fullFill ? orderReq.Quantity : orderReq.Quantity - leftQty),
                                     instr,UUID);

                        //4-We update the final quotes state
                        UpdateQuotes(socket, instr,UUID);

                        //5-we update the credit for potential exposures
                        UpdateCredit(socket, null, UUID);

                        //6-we update the account balance (margin tab) for the firm
                        DoSendAccountBalance(socket, UUID);
                        return true;
                    }
                    else
                    {
                        DoLog(string.Format("Could not find matching buy price for symbol {0} and order price {1}", instr.InstrumentName, orderReq.Price), MessageType.Information);
                        return false;
                    }
                }
                else
                {
                    DoLog(string.Format("Could not process side {1} for symbol {0} ", instr.InstrumentName, orderReq.cSide), MessageType.Information);
                    return false;
                }
            }

        }

        private bool ProcessRejectionsForNewOrders(ClientOrderReq clientOrderReq, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            //TODO : Conseguir InstrumentId de instrumento para rechazar
            if (clientOrderReq.cSide == ClientOrderReq._SIDE_BUY && clientOrderReq.InstrumentId == _REJECTED_SECURITY_ID && clientOrderReq.Price.Value < 6000)
            {


                ClientOrderResponse clientOrdAck = new ClientOrderResponse()
                {
                    Msg = "ClientOrderResponse",
                    ClientOrderId = clientOrderReq.ClientOrderId,
                    InstrumentId = clientOrderReq.InstrumentId,
                    Message = string.Format("Invalid Order for security id {0}", clientOrderReq.InstrumentId),
                    Success = false,
                    OrderId = "0",
                    UserId = clientOrderReq.UserId,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds).ToString(),
                    Uuid = clientOrderReq.Uuid
                };
                DoLog(string.Format("Sending ClientOrderResponse rejected ..."), MessageType.Information);
                DoSend<ClientOrderResponse>(socket, clientOrdAck);

                ClientInstrument instr = GetInstrumentByServiceKey(clientOrderReq.InstrumentId.ToString());

                LegacyOrderRecord rejOrder = new LegacyOrderRecord();
                rejOrder.Msg = "LegacyOrderRecord";
                rejOrder.ClientOrderId = clientOrderReq.ClientOrderId;
                rejOrder.OrderId = GUIDToLongConverter.GUIDToLong(Guid.NewGuid().ToString()).ToString();
                rejOrder.InstrumentId = instr != null ? instr.InstrumentName : "";
                rejOrder.LvsQty = 0;
                rejOrder.OrdQty = Convert.ToDouble(clientOrderReq.Quantity);
                rejOrder.Price = Convert.ToDouble(clientOrderReq.Price);
                rejOrder.cSide = clientOrderReq.cSide;
                rejOrder.cStatus = LegacyOrderRecord._STATUS_REJECTED;
                rejOrder.UpdateTime = Convert.ToInt64(elapsed.TotalMilliseconds);
                rejOrder.OrdRejReson = string.Format("Invalid Order for security id {0}", clientOrderReq.InstrumentId);
                rejOrder.UserId=LoggedUserId;

                Orders.Add(rejOrder);
                TranslateAndSendOldLegacyOrderRecord(socket, clientOrderReq.Uuid, rejOrder, newOrder: true);
                TranslateAndSendOldLegacyOrderRecordToMyOrders(socket, clientOrderReq.Uuid, rejOrder, newOrder: true);

                return true;
            }

            return false;

        }


        private void ProcessFirmsTradingStatusUpdateRequest(IWebSocketConnection socket, string m)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);

            FirmsTradingStatusUpdateRequest wsFirmsTradingStatusUpdateRequest = JsonConvert.DeserializeObject<FirmsTradingStatusUpdateRequest>(m);
            if (FirmListResp != null)
            {
                FirmsCreditRecord firm = FirmListResp.Firms.Where(x => x.FirmId == wsFirmsTradingStatusUpdateRequest.FirmId.ToString()).FirstOrDefault();

                if (firm != null)
                {
                    try
                    {
                        firm.cTradingStatus = wsFirmsTradingStatusUpdateRequest.cTradingStatus;

                        FirmsTradingStatusUpdateResponse resp = new FirmsTradingStatusUpdateResponse()
                        {
                            Success = true,
                            Firm = firm,
                            JsonWebToken = wsFirmsTradingStatusUpdateRequest.JsonWebToken,
                            Msg = "FirmsTradingStatusUpdateResponse",
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            Uuid = wsFirmsTradingStatusUpdateRequest.Uuid
                        };

                        DoSend<FirmsTradingStatusUpdateResponse>(socket, resp);
                    }
                    catch (Exception ex)
                    {
                        FirmsTradingStatusUpdateResponse resp = new FirmsTradingStatusUpdateResponse()
                        {
                            Success = false,
                            JsonWebToken = wsFirmsTradingStatusUpdateRequest.JsonWebToken,
                            Message = ex.Message,
                            Msg = "FirmsTradingStatusUpdateResponse",
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            Uuid = wsFirmsTradingStatusUpdateRequest.Uuid
                        };

                        DoSend<FirmsTradingStatusUpdateResponse>(socket, resp);
                    }
                }
                else
                {

                    FirmsTradingStatusUpdateResponse resp = new FirmsTradingStatusUpdateResponse()
                    {
                        Success = false,
                        JsonWebToken = wsFirmsTradingStatusUpdateRequest.JsonWebToken,
                        Message = string.Format("FirmId {0} not found", wsFirmsTradingStatusUpdateRequest.FirmId),
                        Msg = "FirmsTradingStatusUpdateResponse",
                        Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                        Uuid = wsFirmsTradingStatusUpdateRequest.Uuid

                    };

                    DoSend<FirmsTradingStatusUpdateResponse>(socket, resp);
                }
            }
            else
            {
                FirmsTradingStatusUpdateResponse resp = new FirmsTradingStatusUpdateResponse()
                {
                    Success = false,
                    JsonWebToken = wsFirmsTradingStatusUpdateRequest.JsonWebToken,
                    Message = string.Format("You must invoke FirmListRequest before invoking CreditLimitUpdateRequest "),
                    Msg = "FirmsTradingStatusUpdateResponse",
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    Uuid = wsFirmsTradingStatusUpdateRequest.Uuid

                };

                DoSend<FirmsTradingStatusUpdateResponse>(socket, resp);

            }
        }

        private void ProcessEmailNotificationsDeleteRequest(IWebSocketConnection socket, string m)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            EmailNotificationsDeleteRequest wsEmailNotifDeleteReq = JsonConvert.DeserializeObject<EmailNotificationsDeleteRequest>(m);
            List<Mail> genEmails = new List<Mail>();

            try
            {
                string[] emails;
                if (NotificationEmails.ContainsKey(wsEmailNotifDeleteReq.SettlementFirmId))
                {
                    emails = NotificationEmails[wsEmailNotifDeleteReq.SettlementFirmId];

                    string prevEmail = emails.Where(x => x == wsEmailNotifDeleteReq.Email).FirstOrDefault();

                    if (prevEmail != null)
                    {
                        List<Mail> newEmailList = new List<Mail>();

                        emails.Where(x => x != wsEmailNotifDeleteReq.Email).ToList().ForEach(x => newEmailList.Add(new Mail() { Email = x }));

                        List<string> strEmails = new List<string>();
                        newEmailList.ToList().ForEach(x => strEmails.Add(x.Email));
                        NotificationEmails[wsEmailNotifDeleteReq.SettlementFirmId] = strEmails.ToArray();

                        EmailNotificationsDeleteResponse resp = new EmailNotificationsDeleteResponse()
                        {
                            JsonWebToken = wsEmailNotifDeleteReq.JsonWebToken,
                            Msg = "EmailNotificationsDeleteResponse",
                            SettlementFirmId = wsEmailNotifDeleteReq.SettlementFirmId,
                            Emails = newEmailList.ToArray(),
                            Success = true,
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            Uuid = wsEmailNotifDeleteReq.Uuid
                        };

                        DoSend<EmailNotificationsDeleteResponse>(socket, resp);
                    }
                    else
                    {

                        EmailNotificationsDeleteResponse resp = new EmailNotificationsDeleteResponse()
                        {
                            JsonWebToken = wsEmailNotifDeleteReq.JsonWebToken,
                            Message = string.Format("No email {0} found for Settlement Firm Id {1} found", wsEmailNotifDeleteReq.Email, wsEmailNotifDeleteReq.SettlementFirmId),
                            Msg = "EmailNotificationsDeleteResponse",
                            SettlementFirmId = wsEmailNotifDeleteReq.SettlementFirmId,
                            Success = false,
                            Emails = genEmails.ToArray(),
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            Uuid = wsEmailNotifDeleteReq.Uuid
                        };

                        DoSend<EmailNotificationsDeleteResponse>(socket, resp);
                    }


                }
                else
                {
                    EmailNotificationsDeleteResponse resp = new EmailNotificationsDeleteResponse()
                    {
                        JsonWebToken = wsEmailNotifDeleteReq.JsonWebToken,
                        Message = string.Format("No Settlement Firm Id {0} found", wsEmailNotifDeleteReq.SettlementFirmId),
                        Msg = "EmailNotificationsDeleteResponse",
                        SettlementFirmId = wsEmailNotifDeleteReq.SettlementFirmId,
                        Success = false,
                        Emails = genEmails.ToArray(),
                        Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                        Uuid = wsEmailNotifDeleteReq.Uuid
                    };

                    DoSend<EmailNotificationsDeleteResponse>(socket, resp);
                }


            }
            catch (Exception ex)
            {
                EmailNotificationsUpdateResponse resp = new EmailNotificationsUpdateResponse()
                {
                    JsonWebToken = wsEmailNotifDeleteReq.JsonWebToken,
                    Message = ex.Message,
                    Msg = "EmailNotificationsUpdateResponse",
                    SettlementFirmId = wsEmailNotifDeleteReq.SettlementFirmId,
                    Success = false,
                    Emails = genEmails.ToArray(),
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    Uuid = wsEmailNotifDeleteReq.Uuid
                };

                DoSend<EmailNotificationsUpdateResponse>(socket, resp);
            }
        
        }

        private void ProcessEmailNotificationsUpdateRequest(IWebSocketConnection socket, string m)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            EmailNotificationsUpdateRequest wsEmailNotifUpdateReq = JsonConvert.DeserializeObject<EmailNotificationsUpdateRequest>(m);
            List<Mail> genEmails = new List<Mail>();

            try
            {
                string[] emails;
                if (NotificationEmails.ContainsKey(wsEmailNotifUpdateReq.SettlementFirmId))
                {
                    emails = NotificationEmails[wsEmailNotifUpdateReq.SettlementFirmId];

                    string prevEmail = emails.Where(x => x == wsEmailNotifUpdateReq.EmailCurrent).FirstOrDefault();

                    if (prevEmail != null)
                    {
                        List<Mail> newEmailList = new List<Mail>();

                        emails.Where(x => x != wsEmailNotifUpdateReq.EmailCurrent).ToList().ForEach(x => newEmailList.Add(new Mail() { Email = x }));
                        newEmailList.Add(new Mail() { Email = wsEmailNotifUpdateReq.EmailNew });

                        List<string> strEmails = new List<string>();
                        newEmailList.ToList().ForEach(x => strEmails.Add(x.Email));
                        NotificationEmails[wsEmailNotifUpdateReq.SettlementFirmId] = strEmails.ToArray();

                        EmailNotificationsUpdateResponse resp = new EmailNotificationsUpdateResponse()
                        {
                            JsonWebToken = wsEmailNotifUpdateReq.JsonWebToken,
                            Msg = "EmailNotificationsUpdateResponse",
                            SettlementFirmId = wsEmailNotifUpdateReq.SettlementFirmId,
                            Emails = newEmailList.ToArray(),
                            Success = true,
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            Uuid = wsEmailNotifUpdateReq.Uuid
                        };

                        DoSend<EmailNotificationsUpdateResponse>(socket, resp);
                    }
                    else
                    {

                        EmailNotificationsUpdateResponse resp = new EmailNotificationsUpdateResponse()
                        {
                            JsonWebToken = wsEmailNotifUpdateReq.JsonWebToken,
                            Message = string.Format("No email {0} found for Settlement Firm Id {1} found", wsEmailNotifUpdateReq.EmailCurrent, wsEmailNotifUpdateReq.SettlementFirmId),
                            Msg = "EmailNotificationsUpdateResponse",
                            SettlementFirmId = wsEmailNotifUpdateReq.SettlementFirmId,
                            Success = false,
                            Emails=genEmails.ToArray(),
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            Uuid = wsEmailNotifUpdateReq.Uuid
                        };

                        DoSend<EmailNotificationsUpdateResponse>(socket, resp);
                    }

                   
                }
                else
                {
                    EmailNotificationsUpdateResponse resp = new EmailNotificationsUpdateResponse()
                    {
                        JsonWebToken = wsEmailNotifUpdateReq.JsonWebToken,
                        Message = string.Format("No Settlement Firm Id {0} found", wsEmailNotifUpdateReq.SettlementFirmId),
                        Msg = "EmailNotificationsUpdateResponse",
                        SettlementFirmId = wsEmailNotifUpdateReq.SettlementFirmId,
                        Success = false,
                        Emails = genEmails.ToArray(),
                        Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                        Uuid = wsEmailNotifUpdateReq.Uuid
                    };

                    DoSend<EmailNotificationsUpdateResponse>(socket, resp);
                }

             
            }
            catch (Exception ex)
            {
                EmailNotificationsUpdateResponse resp = new EmailNotificationsUpdateResponse()
                {
                    JsonWebToken = wsEmailNotifUpdateReq.JsonWebToken,
                    Message = ex.Message,
                    Msg = "EmailNotificationsUpdateResponse",
                    SettlementFirmId = wsEmailNotifUpdateReq.SettlementFirmId,
                    Success = false,
                    Emails = genEmails.ToArray(),
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    Uuid = wsEmailNotifUpdateReq.Uuid
                };

                DoSend<EmailNotificationsUpdateResponse>(socket, resp);
            }
        
        }

        private void ProcessEmailNotificationsCreateRequest(IWebSocketConnection socket, string m)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            EmailNotificationsCreateRequest wsEmailNotifCreateReq = JsonConvert.DeserializeObject<EmailNotificationsCreateRequest>(m);
            List<Mail> genEmails = new List<Mail>();

            try
            {
                string[] emails;
                if (NotificationEmails.ContainsKey(wsEmailNotifCreateReq.SettlementFirmId))
                {
                    emails = NotificationEmails[wsEmailNotifCreateReq.SettlementFirmId];

                    Array.Resize(ref emails, emails.Length + 1);
                    emails[emails.Length - 1] = wsEmailNotifCreateReq.Email;

                    NotificationEmails[wsEmailNotifCreateReq.SettlementFirmId] = emails;
                }
                else
                {
                    emails = new string[] { wsEmailNotifCreateReq.Email };
                    NotificationEmails.Add(wsEmailNotifCreateReq.SettlementFirmId, emails);
                }

                List<Mail> mails = new List<Mail>();
                NotificationEmails[wsEmailNotifCreateReq.SettlementFirmId].ToList().ForEach(x => mails.Add(new Mail() { Email = x }));

                EmailNotificationsCreateResponse resp = new EmailNotificationsCreateResponse()
                {
                    JsonWebToken = wsEmailNotifCreateReq.JsonWebToken,
                    Msg = "EmailNotificationsCreateResponse",
                    SettlementFirmId = wsEmailNotifCreateReq.SettlementFirmId,
                    Emails = mails.ToArray(),
                    Success = true,
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    Uuid = wsEmailNotifCreateReq.Uuid
                };

                DoSend<EmailNotificationsCreateResponse>(socket, resp);
            }
            catch (Exception ex)
            {
                EmailNotificationsCreateResponse resp = new EmailNotificationsCreateResponse()
                {
                    JsonWebToken = wsEmailNotifCreateReq.JsonWebToken,
                    Message = ex.Message,
                    Msg = "EmailNotificationsCreateResponse",
                    SettlementFirmId = wsEmailNotifCreateReq.SettlementFirmId,
                    Success = false,
                    Emails=genEmails.ToArray(),
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    Uuid = wsEmailNotifCreateReq.Uuid
                };

                DoSend<EmailNotificationsCreateResponse>(socket, resp);
            }
        
        }

        private void ProcessEmailNotificationsListRequest(IWebSocketConnection socket, string m)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            EmailNotificationsListRequest wsEmailNotifListReq = JsonConvert.DeserializeObject<EmailNotificationsListRequest>(m);
            List<Mail> emails = new List<Mail>();

            try
            {
                if (NotificationEmails.ContainsKey(wsEmailNotifListReq.SettlementFirmId))
                {

                    NotificationEmails[wsEmailNotifListReq.SettlementFirmId].ToList().ForEach(x => emails.Add(new Mail() { Email = x }));

                    EmailNotificationsListResponse resp = new EmailNotificationsListResponse()
                    {
                        JsonWebToken = wsEmailNotifListReq.JsonWebToken,
                        Msg = "EmailNotificationsListResponse",
                        SettlementFirmId = wsEmailNotifListReq.SettlementFirmId,
                        Emails = emails.ToArray(),
                        Success = true,
                        Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                        Uuid = wsEmailNotifListReq.Uuid
                    };

                    DoSend<EmailNotificationsListResponse>(socket, resp);

                }
                else
                {
                    

                    EmailNotificationsListResponse resp = new EmailNotificationsListResponse()
                    {
                        JsonWebToken = wsEmailNotifListReq.JsonWebToken,
                        Msg = "EmailNotificationsListResponse",
                        SettlementFirmId = wsEmailNotifListReq.SettlementFirmId,
                        Success = true,
                        Emails = emails.ToArray(), 
                        Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                        Uuid = wsEmailNotifListReq.Uuid
                    };

                    DoSend<EmailNotificationsListResponse>(socket, resp);
                    
                }
            }
            catch (Exception ex)
            {
                EmailNotificationsListResponse resp = new EmailNotificationsListResponse()
                {
                    JsonWebToken = wsEmailNotifListReq.JsonWebToken,
                    Message = ex.Message,
                    Msg = "EmailNotificationsListResponse",
                    SettlementFirmId = wsEmailNotifListReq.SettlementFirmId,
                    Success = false,
                    Emails = emails.ToArray(), 
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    Uuid = wsEmailNotifListReq.Uuid
                };

                DoSend<EmailNotificationsListResponse>(socket, resp);
            }
        }

        private void ProcessFirmsCreditLimitUpdateRequest(IWebSocketConnection socket, string m)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            FirmsCreditLimitUpdateRequest wsFirmCreditLimitUpdRq = JsonConvert.DeserializeObject<FirmsCreditLimitUpdateRequest>(m);

            if (FirmListResp != null)
            {
                DoLog(string.Format("Looking form Firm with Id {0} to update its credit limit", wsFirmCreditLimitUpdRq.FirmId), MessageType.Information);
                FirmsCreditRecord firm = FirmListResp.Firms.Where(x => x.FirmId == wsFirmCreditLimitUpdRq.FirmId.ToString()).FirstOrDefault();

                if (firm != null)
                {
                    DoLog(string.Format("Firm {0} found. Updating its credit limit", wsFirmCreditLimitUpdRq.FirmId), MessageType.Information);

                    try
                    {

                        CreateFakePositionToConciliateUsedCredit(wsFirmCreditLimitUpdRq.FirmId.ToString(), wsFirmCreditLimitUpdRq.UsedCredit - firm.UsedCredit);

                        //firm.CreditLimit.TradingStatus = wsFirmCreditLimitUpdRq.TradingStatus;
                        firm.AvailableCredit = wsFirmCreditLimitUpdRq.AvailableCredit;
                        
                        //Balance = Total - Usage
                        firm.UsedCredit = GetUsedCredit(wsFirmCreditLimitUpdRq.FirmId.ToString());
                        firm.MaxNotional = wsFirmCreditLimitUpdRq.MaxNotional;
                        firm.MaxQuantity = wsFirmCreditLimitUpdRq.MaxQuantity;
                        firm.PotentialExposure = wsFirmCreditLimitUpdRq.PotentialExposure;

                        FirmsCreditLimitUpdateResponse resp = new FirmsCreditLimitUpdateResponse()
                        {
                            Success = true,
                            FirmId = wsFirmCreditLimitUpdRq.FirmId,
                            JsonWebToken = wsFirmCreditLimitUpdRq.JsonWebToken,
                            Message = null,
                            Msg = "FirmsCreditLimitUpdateResponse",
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            Uuid = wsFirmCreditLimitUpdRq.Uuid,
                            //Firm = firm
                        };

                        DoSend<FirmsCreditLimitUpdateResponse>(socket, resp);

                        FirmsCreditRecord newCreditLimit = new FirmsCreditRecord()
                        {
                            Msg = "FirmsCreditLimitRecord",
                            //Firm = firm,
                            AvailableCredit=firm.AvailableCredit,
                            cTradingStatus=firm.cTradingStatus,
                            //CurrencyRootId=firm.CurrencyRootId,
                            FirmId=firm.FirmId.ToString(),
                            MaxNotional=firm.MaxNotional,
                            MaxQuantity=firm.MaxQuantity,
                            //Name=firm.Name,
                            //ShortName=firm.ShortName,
                            UsedCredit=firm.UsedCredit,
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            //Uuid = wsFirmCreditLimitUpdRq.Uuid
                        };

                        DoSend<FirmsCreditRecord>(socket, newCreditLimit);
                    }
                    catch (Exception ex)
                    {

                        FirmsCreditLimitUpdateResponse resp = new FirmsCreditLimitUpdateResponse()
                        {
                            Success = false,
                            FirmId = wsFirmCreditLimitUpdRq.FirmId,
                            JsonWebToken = wsFirmCreditLimitUpdRq.JsonWebToken,
                            Message = string.Format("Error updating firm Id {0} not found:{1}", wsFirmCreditLimitUpdRq.FirmId, ex.Message),
                            Msg = "FirmsCreditLimitUpdateResponse",
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            Uuid = wsFirmCreditLimitUpdRq.Uuid
                        };

                        DoSend<FirmsCreditLimitUpdateResponse>(socket, resp);

                    }
                }
                else
                {
                    DoLog(string.Format("Firm {0} not found", wsFirmCreditLimitUpdRq.FirmId), MessageType.Error);

                    FirmsCreditLimitUpdateResponse resp = new FirmsCreditLimitUpdateResponse()
                    {
                        Success = false,
                        FirmId = wsFirmCreditLimitUpdRq.FirmId,
                        JsonWebToken = wsFirmCreditLimitUpdRq.JsonWebToken,
                        Message = string.Format("Firm Id {0} not found", wsFirmCreditLimitUpdRq.FirmId),
                        Msg = "FirmsCreditLimitUpdateResponse",
                        Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                        Uuid = wsFirmCreditLimitUpdRq.Uuid

                    };

                    DoSend<FirmsCreditLimitUpdateResponse>(socket, resp);
                }
            }
            else
            {
                FirmsCreditLimitUpdateResponse resp = new FirmsCreditLimitUpdateResponse()
                {
                    Success = false,
                    FirmId = wsFirmCreditLimitUpdRq.FirmId,
                    JsonWebToken = wsFirmCreditLimitUpdRq.JsonWebToken,
                    Message = string.Format("You must invoke FirmListRequest before invoking CreditLimitUpdateRequest "),
                    Msg = "FirmsCreditLimitUpdateResponse",
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    Uuid = wsFirmCreditLimitUpdRq.Uuid

                };

                DoSend<FirmsCreditLimitUpdateResponse>(socket, resp);
            
            }
        
        }

        private double GetTotalSideExposure(char side)
        {
            //1-Get Base Margin
            double BM = GetBaseMargin(LoggedFirmId) - GetFundedMargin(LoggedFirmId) ;

            //2-Calculate the exposure for the Buy/Sell orders
            double PxM = GetPotentialxMargin(side);

            //3- Calculate the potential x Exposure
            //double exposure = Math.Max(Convert.ToDouble(PxM - BM), 0) / Config.MarginPct;
            double exposure = Convert.ToDouble(PxM - BM) / Config.MarginPct;
            DoLog(string.Format("Side {0} exposure:{1}", side, exposure), MessageType.Information);
            return Convert.ToDouble(PxM - BM)/Config.MarginPct;
        
        }

        private double  GetBaseMargin(string firmId)
        {
            double acumMargin = 0;

            List<UserRecord> usersForFirm = UserRecords.Where(x => x.FirmId == LoggedFirmId).ToList();

            foreach (SecurityMasterRecord security in SecurityMasterRecords)
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

                DoLog(string.Format("Net Contracts for Security {0} :{1}", security.Symbol, netContracts), MessageType.Information);

            }
           
            //TODO : implement the calendar spreads margin calculation
            DoLog(string.Format("Base Margin for FirmId {0}:{1}", LoggedFirmId, acumMargin), MessageType.Information);

            return acumMargin;
        }

        //here I can work with just 1 security
        private double GetSecurityPotentialExposure(char side,double qty,string symbol,string firmId)
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
                        totalDiscount += netSpread * Config.MarginPct * Config.TwoWideCalDisc* (DSP1.Price.Value + DSP2.Price.Value);
                    else if (spreadIndex >= 3)//3-wide spread or wider
                        totalDiscount += netSpread * Config.MarginPct * Config.ThreeWideCalDisc* (DSP1.Price.Value + DSP2.Price.Value);;

                    currContract.NetContracts -= (currContract.NetContracts > 0) ? netSpread : (-1 * netSpread);
                    nextContract.NetContracts -= (nextContract.NetContracts > 0) ? netSpread : (-1 * netSpread);
                }
            }

            return totalDiscount;
        }

        private double CalculateCalendarMarginDiscounts(NetPositionDTO[] netPositionsArr,string assetClass)
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

        private double GetFundedMargin(string firmId)
        {
            return GetFundedCredit(firmId) * Config.MarginPct;
        }


        private double GetFundedCredit(string firmId)
        {
            FundedMargin fundedMargin = FundedMargins.Where(x => x.FirmId == firmId).FirstOrDefault();

            if (fundedMargin != null)
                return fundedMargin.Margin / Config.MarginPct;
            else
                return 0;
        }

        private double GetUsedCredit(string firmId)
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

                if(DSP!=null && DSP.Price.HasValue && netContracts!=0)
                    creditUsed += Math.Abs(netContracts) * DSP.Price.Value;

                if(security.MaturityDate!="")
                    netPositionsArr.Add(new NetPositionDTO() {AssetClass=security.AssetClass, Symbol = security.Symbol, MaturityDate = security.GetMaturityDate(), NetContracts = netContracts });

                DoLog(string.Format("Final Net Contracts for Security Id {0}:{1}", security.Symbol, netContracts), MessageType.Information);
            }

            if (Config.ImplementCalendarMarginDiscount)
            {
                creditUsed -= (CalculateCalendarMarginDiscounts(netPositionsArr.ToArray(), "SWP") / Config.MarginPct);
                creditUsed -= (CalculateCalendarMarginDiscounts(netPositionsArr.ToArray(), "NDF") / Config.MarginPct);
            }

            return creditUsed - GetFundedCredit(firmId) ;
        }

        private void CreateFirmListCreditStructure(string UUID, int pageNo,int pageRecords)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            Dictionary<string, FirmsCreditRecord> firms = new Dictionary<string, FirmsCreditRecord>();
            List<FirmsCreditRecord> finalList = new List<FirmsCreditRecord>();

            foreach (AccountRecord accRecord in AccountRecords)
            {
                if (!firms.ContainsKey(accRecord.EPFirmId))
                {
                    //1- We create the accounts list
                    List<AccountRecord> firmAccounts = AccountRecords.Where(x => x.EPFirmId == accRecord.EPFirmId).ToList();
                    //List<ClientAccountRecord> v2accountList = new List<ClientAccountRecord>();
                    List<string> v2accountList = new List<string>();
                    firmAccounts.ForEach(x => v2accountList.Add(GetClientAccountRecordFromV1AccountRecord(x).AccountId));

                    //2- We creat the credit list
                    DGTLBackendMock.Common.DTO.Account.AccountRecord defaultAccount = AccountRecords.Where(x => x.EPFirmId == accRecord.EPFirmId && x.Default).FirstOrDefault();

                    double creditUsed = GetUsedCredit(accRecord.EPFirmId);
                    FirmsCreditRecord firm = new FirmsCreditRecord()
                    {
                        FirmId = accRecord.EPFirmId,
                        AvailableCredit = (defaultAccount != null) ? (defaultAccount.CreditLimit - creditUsed) : 0,
                        UsedCredit = creditUsed,
                        PotentialExposure = 0,
                        MaxNotional = accRecord.MaxNotional,
                        MaxQuantity = accRecord.MaxNotional / 7000,//We set an hypothetical maxqty based on BTC price
                        cTradingStatus = CreditLimit._TRADING_STATUS_TRADING,
                        Accounts = v2accountList.ToArray(),
                    };

                    firms.Add(accRecord.EPFirmId, firm);
                    finalList.Add(firm);
                }
            }


            double totalPages = Math.Ceiling(Convert.ToDouble(finalList.Count / pageRecords));
            FirmListResp = new FirmsListResponse()
            {
                Msg = "FirmsListResponse",
                Success = true,
                Firms = finalList.Skip(pageNo * pageRecords).Take(pageRecords).ToArray(),
                //JsonWebToken = token,
                Uuid = UUID,
                //Message = null,
                PageNo = pageNo,
                Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                PageRecords = Convert.ToInt32(totalPages),
            };
        
        }

        private void CreatePosition(LegacyTradeHistory newTrade,string userId)
        {
            ClientPosition newPos = new ClientPosition()
            {
                Contracts = newTrade.cMySide == LegacyTradeHistory._SIDE_BUY ? newTrade.TradeQuantity : newTrade.TradeQuantity * -1,
                MarginFunded = false,
                Msg = "Position",
                Price = newTrade.TradePrice,
                Symbol = newTrade.Symbol,
                UserId = userId
            };

            List<ClientPosition> tempPos = Positions.ToList();
            tempPos.Add(newPos);
            Positions = tempPos.ToArray();
        
        }

        //When updating a firm's credit level, we create a fake position so the Used Credit = Positions Used Credit
        private void CreateFakePositionToConciliateUsedCredit(string firmId ,double usedDiff)
        {

            DailySettlementPrice firstDSP = DailySettlementPrices.Where(x => x.Price.HasValue).FirstOrDefault();
            UserRecord firstUser = UserRecords.Where(x => x.FirmId == firmId).FirstOrDefault();

            if (firstDSP != null && firstUser != null)
            {
                double price = Convert.ToDouble(firstDSP.Price.Value);
                ClientPosition unkPos = new ClientPosition()
                {
                    Contracts = usedDiff / price,
                    MarginFunded = false,
                    Msg = "Position",
                    Price = price,
                    Symbol = firstDSP.Symbol + "<conc>",
                    UserId = firstUser.UserId
                };

                List<ClientPosition> tempList = Positions.ToList();
                tempList.Add(unkPos);
                Positions = tempList.ToArray();
            }
        }

        private void ProcessFirmsListRequest (IWebSocketConnection socket, string m)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            FirmsListRequest wsFirmListRq = JsonConvert.DeserializeObject<FirmsListRequest>(m);
            try
            {
               
                if(FirmListResp==null)
                    CreateFirmListCreditStructure(wsFirmListRq.Uuid, 0,int.MaxValue);

                FirmListResp.Uuid = wsFirmListRq.Uuid;

                DoLog(string.Format("Process FirmsListReqest: {0} firms loaded", FirmListResp.Firms.Length), MessageType.Information);

                List<FirmsCreditRecord> firmsCreditLimitRecord = new List<FirmsCreditRecord>();
                foreach (var firm in FirmListResp.Firms)
                {

                    FirmsCreditRecord firmResp = new FirmsCreditRecord();
                    firmResp.Msg = "FirmCreditRecord ";
                    //firmResp.Uuid = wsFirmListRq.Uuid;
                    firmResp.FirmId = firm.FirmId.ToString();
                    //firmResp.Name = firm.Name;
                    //firmResp.ShortName = firm.ShortName;
                    firmResp.AvailableCredit = firm.AvailableCredit;
                    firmResp.UsedCredit = firm.UsedCredit;
                    firmResp.PotentialExposure = firm.PotentialExposure;
                    firmResp.MaxNotional = firm.MaxNotional;
                    firmResp.MaxQuantity = firm.MaxQuantity;
                    //firmResp.CurrencyRootId = firm.CurrencyRootId;
                    firmResp.TradingStatus = firm.TradingStatus;
                    firmResp.Time = Convert.ToInt64(epochElapsed.TotalMilliseconds);
                    firmResp.Accounts = firm.Accounts;

                    firmsCreditLimitRecord.Add(firmResp);

                    //DoSend<FirmsCreditLimitRecord>(socket, thisResp);
                }

                //double totalPages = Math.Ceiling(Convert.ToDouble(FirmListResp.Firms.Length / wsFirmListRq.PageRecords));

                FirmsListResponse thisResp = new FirmsListResponse();
                thisResp.Firms = firmsCreditLimitRecord.ToArray();
                thisResp.SettlementAgentId = "DefaultSettlAgent";
                thisResp.Msg = "FirmsListResponse";
                thisResp.PageNo = 0;
                thisResp.PageRecords = 1;
                thisResp.Uuid = wsFirmListRq.Uuid;
                thisResp.Success = true;

                DoSend<FirmsListResponse>(socket, thisResp);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Process FirmsListReqest Error: {0} ", ex.Message), MessageType.Error);

                FirmsListResponse firmListResp = new FirmsListResponse()
                {
                    Msg = "FirmsListResponse",
                    Success = false,
                    SettlementAgentId = "DefaultSettlAgent",
                    //JsonWebToken = wsFirmListRq.JsonWebToken,
                    Uuid = wsFirmListRq.Uuid,
                    //Message = ex.Message,
                    PageNo = wsFirmListRq.PageNo,
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    PageRecords = 0,
                };

                DoSend<FirmsListResponse>(socket, firmListResp);
            }
        
        }

        private void ProcessTokenResponse(IWebSocketConnection socket, string m)
        {
            TokenRequest wsTokenReq = JsonConvert.DeserializeObject<TokenRequest>(m);

            LastTokenGenerated = Guid.NewGuid().ToString();

            TokenResponse resp = new TokenResponse()
            {
                Msg = "TokenResponse",
                Token = LastTokenGenerated,
                Uuid = wsTokenReq.Uuid,
                Success = true,
                Time = wsTokenReq.Time
            };

            DoSend<TokenResponse>(socket, resp);
        }

        private void SendLoginRejectReject(IWebSocketConnection socket, ClientLoginRequest wsLogin, string msg)
        {
            ClientLoginResponse reject = new ClientLoginResponse()
            {
                Msg = "ClientLoginResponse",
                Uuid = wsLogin.Uuid,
                JsonWebToken = LastTokenGenerated,
                Message = msg,
                Success = false,
                Time = wsLogin.Time,
                UserId = ""
            };

            DoSend<ClientLoginResponse>(socket, reject);
        }

        private void SendInstrumentStates(IWebSocketConnection socket, string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            int i = 1;

            List<ClientInstrument> instrList = new List<ClientInstrument>();
            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {
                ClientInstrumentState state = new ClientInstrumentState();
                state.Msg = "ClientInstrumentState";
                state.cState = ClientInstrumentState.GetSecurityStatus(security.Status);
                state.ExchangeId = "0";
                state.InstrumentId = security.InstrumentId.ToString();
                state.cReasonCode = ClientInstrumentState._REASON_CODE_2;
                state.TriggerPrice = null;

                DoLog(string.Format("Sending ClientInstrumentState "), MessageType.Information);
                DoSend<ClientInstrumentState>(socket, state);
            }

        }

        private ClientInstrumentBatch LoadInstrBatch()
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            int i = 1;

            List<ClientInstrument> instrList = new List<ClientInstrument>();

            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {

                ClientInstrument instrumentMsg = new ClientInstrument();
                instrumentMsg.Msg = "ClientInstrument";
                instrumentMsg.CreatedAt = Convert.ToInt64(epochElapsed.TotalMilliseconds).ToString();
                instrumentMsg.UpdatedAt = Convert.ToInt64(epochElapsed.TotalMilliseconds).ToString();
                instrumentMsg.LastUpdatedBy = "";
                instrumentMsg.ExchangeId = "0";
                instrumentMsg.Description = security.Description;
                instrumentMsg.InstrumentDate = !string.IsNullOrEmpty(security.MaturityDate) ? Convert.ToInt32(security.MaturityDate) : 0;
                instrumentMsg.InstrumentId = i.ToString();
                instrumentMsg.InstrumentName = security.Symbol;
                instrumentMsg.LastUpdatedBy = "fernandom";
                instrumentMsg.LotSize = Config.SendLotSize ? (decimal?) security.LotSize : null ;
                instrumentMsg.MaxOrdQty = Config.SendMaxOrdQty ? (double?)Convert.ToDouble(security.MaxSize) : null;
                instrumentMsg.MinOrdQty = Config.SendMinOrdQty ? (double?) Convert.ToDouble(security.MinSize) : null;
                instrumentMsg.cProductType = ClientInstrument.GetProductType(security.AssetClass);
                instrumentMsg.MinQuotePrice = security.MinPrice;
                instrumentMsg.MaxQuotePrice = security.MaxPrice;
                instrumentMsg.MinPriceIncrement = Config.SendMinPriceIncrement ? (decimal?)security.MinPriceIncrement : null;
                instrumentMsg.IsTrading = ClientInstrumentState.GetSecurityStatus(security.Status) == ClientInstrumentState._STATE_OPEN;

                if (Config.SendMaxNotionalValue)
                    instrumentMsg.MaxNotionalValue = security.MaxNotional.HasValue ? (decimal?)security.MaxNotional.Value : null;

                if (instrumentMsg.cProductType == ClientInstrument._SPOT)
                {
                    instrumentMsg.Currency1 = "crypto";
                    instrumentMsg.Currency2 = "USD";
                    //instrumentMsg.Currency1 = security.CurrencyPair.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    //instrumentMsg.Currency2 = security.CurrencyPair.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[0];
                }
                else
                {
                    instrumentMsg.Currency1 = "XBT";
                    instrumentMsg.Currency2 = "USD";
                }

                instrumentMsg.Test = false;
                //instrumentMsg.UUID = Uuid;

                security.InstrumentId = i;
                i++;

                //DoLog(string.Format("Sending Instrument "), MessageType.Information);
                //DoSend<Instrument>(socket, instrumentMsg);
                instrList.Add(instrumentMsg);
            }

            InstrBatch = new ClientInstrumentBatch() { Msg = "ClientInstrumentBatch", messages = instrList.ToArray() };

            return InstrBatch;
        }

        private void SendCRMInstruments(IWebSocketConnection socket, string Uuid)
        {
            InstrBatch = LoadInstrBatch();
            DoLog(string.Format("Sending Instrument Batch "), MessageType.Information);
            DoSend<ClientInstrumentBatch>(socket, InstrBatch);

        }

        //Sending logged user
        private void SendCRMUsers(IWebSocketConnection socket, string login, string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            UserRecord userRecord = UserRecords.Where(x => x.UserId == login).FirstOrDefault();

            if (userRecord != null)
            {

                AccountRecord accountRecord = AccountRecords.Where(x => x.EPFirmId == userRecord.FirmId && x.Default).FirstOrDefault();

                ClientUserRecord userRecordMsg = new ClientUserRecord();
                userRecordMsg.Address = "";
                userRecordMsg.cConnectionType = '0';
                userRecordMsg.City = "";
                userRecordMsg.cUserType = '0';
                userRecordMsg.DefaultAccount = accountRecord != null ? accountRecord.AccountId : null;
                userRecordMsg.Email = "";
                userRecordMsg.FirmId = Convert.ToInt64(userRecord.FirmId);
                userRecordMsg.FirstName = userRecord.FirstName;
                userRecordMsg.GroupId = "";
                userRecordMsg.IsAdmin = false;
                userRecordMsg.LastName = userRecord.LastName;
                userRecordMsg.Msg = "ClientUserRecord";
                userRecordMsg.PostalCode = "";
                userRecordMsg.State = "";
                userRecordMsg.UserId = userRecord.UserId;
                //userRecordMsg.UUID = Uuid;

                DoLog(string.Format("Sending ClientUserRecord"), MessageType.Information);
                DoSend<ClientUserRecord>(socket, userRecordMsg);
            }
            else
                throw new Exception(string.Format("User not found {0}", login));
        }

        private void SendMarketStatus(IWebSocketConnection socket, string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);

            ClientMarketState marketStateMsg = new ClientMarketState();
            marketStateMsg.ExchangeId = ClientMarketState._DEFAULT_EXCHANGE_ID;
            marketStateMsg.cReasonCode = '0';
            marketStateMsg.cState = ClientMarketState.TranslateV1StatesToV2States(PlatformStatus.cState);
            //marketStateMsg.Msg = "ClientMarketState";
            marketStateMsg.Msg = "MarketState";
            marketStateMsg.StateTime = Convert.ToInt64(epochElapsed.TotalMilliseconds);
            //marketStateMsg.UUID = Uuid;


            ClientMarketStateBatch marketStateBatch = new ClientMarketStateBatch()
            {
                Msg = "ClientMarketStateBatch",
                messages = new ClientMarketState[] { marketStateMsg }

            };

            DoLog(string.Format("Sending ClientMarketStateBatch"), MessageType.Information);
            DoSend<ClientMarketStateBatch>(socket, marketStateBatch);
        }

        private void DoSendAccountBalance(IWebSocketConnection socket, string Uuid)
        {
            double todayMargin = GetFundedCredit(LoggedFirmId) * Config.MarginPct;
            double totalMargin = GetUsedCredit(LoggedFirmId)* Config.MarginPct;


            AccountBalance balance = new AccountBalance()
            {
                Msg = "AccountBalance",
                Uuid = Uuid,
                FirmId = LoggedFirmId,
                UserId = LoggedUserId,
                Collateral = 0,
                TodaysIM = todayMargin,
                PriorDaysIM = todayMargin,
                IMRequirement = totalMargin,
                VMRequirement = totalMargin - todayMargin,
                cStatus = AccountBalance._ACTIVE_STATUS,
                MarginCall = todayMargin < 0
            };

            DoLog(string.Format("Sending AccountBalance "), MessageType.Information);
            DoSend<AccountBalance>(socket, balance);
        }

        private ClientAccountRecord GetClientAccountRecordFromV1AccountRecord(DGTLBackendMock.Common.DTO.Account.AccountRecord accountRecord)
        {
            ClientAccountRecord v2AccountRecordMsg = new ClientAccountRecord();
            v2AccountRecordMsg.Msg = "ClientAccountRecord";
            v2AccountRecordMsg.AccountId = accountRecord.AccountId;
            v2AccountRecordMsg.FirmId = Convert.ToInt64(accountRecord.EPFirmId);
            v2AccountRecordMsg.SettlementFirmId = accountRecord.ClearingFirmId;
            v2AccountRecordMsg.AccountName = accountRecord.EPNickName;
            v2AccountRecordMsg.AccountAlias = accountRecord.AccountId;


            v2AccountRecordMsg.AccountNumber = "";
            v2AccountRecordMsg.AccountType = 0;
            v2AccountRecordMsg.cStatus = ClientAccountRecord._STATUS_ACTIVE;
            v2AccountRecordMsg.cUserType = ClientAccountRecord._DEFAULT_USER_TYPE;
            v2AccountRecordMsg.cCti = ClientAccountRecord._CTI_OTHER;
            v2AccountRecordMsg.Lei = "";
            v2AccountRecordMsg.Currency = "";
            v2AccountRecordMsg.IsSuspense = false;
            v2AccountRecordMsg.UsDomicile = true;
            v2AccountRecordMsg.UpdatedAt = "0";
            v2AccountRecordMsg.CreatedAt = "0";
            v2AccountRecordMsg.LastUpdatedBy = "";
            v2AccountRecordMsg.WalletAddress = "";
            v2AccountRecordMsg.Default = accountRecord.Default;
            //accountRecordMsg.UUID = Uuid;

            return v2AccountRecordMsg;
        
        }

        private void SendCRMAccounts(IWebSocketConnection socket, string login, string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            UserRecord userRecord = UserRecords.Where(x => x.UserId == login).FirstOrDefault();

            if (userRecord != null)
            {
                List<DGTLBackendMock.Common.DTO.Account.AccountRecord> accountRecords = AccountRecords.Where(x => x.EPFirmId == userRecord.FirmId).ToList();

                List<ClientAccountRecord> accRecordList = new List<ClientAccountRecord>();
                foreach (DGTLBackendMock.Common.DTO.Account.AccountRecord accountRecord in accountRecords)
                {
                    ClientAccountRecord accountRecordMsg = GetClientAccountRecordFromV1AccountRecord(accountRecord);
                    accRecordList.Add(accountRecordMsg);
                }

                ClientAccountRecordBatch accRecordBatch = new ClientAccountRecordBatch()
                {
                    Msg = "ClientAccountRecordBatch",
                    messages = accRecordList.ToArray()
                };

                DoLog(string.Format("Sending ClientAccountRecordBatch "), MessageType.Information);
                DoSend<ClientAccountRecordBatch>(socket, accRecordBatch);
            }
            else
                throw new Exception(string.Format("User not found {0}", login));

        }

        private void SendCRMMessages(IWebSocketConnection socket, string login, string Uuid = null)
        {
            SendCRMInstruments(socket, Uuid);

            SendInstrumentStates(socket, Uuid);

            SendCRMUsers(socket, login, Uuid);

            SendMarketStatus(socket, Uuid);

            SendCRMAccounts(socket, login, Uuid);
        }

        #endregion

        #region Protected Methods

        protected override void DoCLoseThread(object p)
        {
            lock (tLock)
            {

                ProcessLastSaleThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessLastSaleThreads.Clear();

                ProcessLastQuoteThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessLastQuoteThreads.Clear();

                //ProcessDailyOfficialFixingPriceThreads.Values.ToList().ForEach(x => x.Abort());
                //ProcessDailyOfficialFixingPriceThreads.Clear();

                //ProcessDailySettlementThreads.Values.ToList().ForEach(x => x.Abort());
                //ProcessDailySettlementThreads.Clear();

                //ProcessSecuritStatusThreads.Values.ToList().ForEach(x => x.Abort());
                //ProcessSecuritStatusThreads.Clear();

                //ProcessCreditLimitUpdatesThreads.Values.ToList().ForEach(x => x.Abort());
                //ProcessCreditLimitUpdatesThreads.Clear();


                //NotificationsSubscriptions.Clear();
                //OpenOrderCountSubscriptions.Clear();

                Connected = false;

                DoLog("Turning threads off on socket disconnection", MessageType.Information);
            }
        }

        protected void ProcessUnsubscriptions(Subscribe subscrMsg)
        {
          
            lock (SecurityMappings)
            {

                if (subscrMsg.Service == "LS")
                {
                    if (ProcessLastSaleThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessLastSaleThreads[subscrMsg.ServiceKey].Abort();
                        ProcessLastSaleThreads.Remove(subscrMsg.ServiceKey);
                    }
                }
                else if (subscrMsg.Service == "LQ")
                {
                    if (ProcessLastQuoteThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessLastQuoteThreads[subscrMsg.ServiceKey].Abort();
                        ProcessLastQuoteThreads.Remove(subscrMsg.ServiceKey);
                    }
                }
                //else if (subscrMsg.Service == "FP")
                //{
                //    if (ProcessDailyOfficialFixingPriceThreads.ContainsKey(subscrMsg.ServiceKey))
                //    {
                //        ProcessDailyOfficialFixingPriceThreads.Remove(subscrMsg.ServiceKey);
                //    }
                //}
                //else if (subscrMsg.Service == "TI")
                //{
                //    if (ProcessSecuritStatusThreads.ContainsKey(subscrMsg.ServiceKey))
                //    {
                //        ProcessSecuritStatusThreads[subscrMsg.ServiceKey].Abort();
                //        ProcessSecuritStatusThreads.Remove(subscrMsg.ServiceKey);
                //    }
                //}
                //else if (subscrMsg.Service == "Ot")
                //{
                //    if (OpenOrderCountSubscriptions.Contains(subscrMsg.ServiceKey))
                //        OpenOrderCountSubscriptions.Add(subscrMsg.ServiceKey);
                //}
                //else if (subscrMsg.Service == "TN")
                //{
                //    if (NotificationsSubscriptions.Contains(subscrMsg.ServiceKey))
                //        NotificationsSubscriptions.Add(subscrMsg.ServiceKey);
                //}
                //else if (subscrMsg.Service == "FD")
                //{
                //    if (ProcessDailySettlementThreads.ContainsKey(subscrMsg.ServiceKey))
                //    {
                //        ProcessDailySettlementThreads[subscrMsg.ServiceKey].Abort();
                //        ProcessDailySettlementThreads.Remove(subscrMsg.ServiceKey);
                //    }
                //}
                //else if (subscrMsg.Service == "Cm")
                //{
                //    if (ProcessCreditLimitUpdatesThreads.ContainsKey(subscrMsg.ServiceKey))
                //    {
                //        ProcessCreditLimitUpdatesThreads[subscrMsg.ServiceKey].Abort();
                //        ProcessCreditLimitUpdatesThreads.Remove(subscrMsg.ServiceKey);
                //    }
                //}
                //else if (subscrMsg.Service == "CU")
                //{
                //    //ProcessCreditRecordUpdates(socket, subscrMsg);
                //}
            }

        }

        protected void SendHeartbeat(object param)
        {
            IWebSocketConnection socket = (IWebSocketConnection)((object[])param)[0];
            string token = (string)((object[])param)[1];
            string uuid = (string)((object[])param)[2];

            try
            {

                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                ClientHeartbeat heartbeat = new ClientHeartbeat()
                {
                    Msg = "ClientHeartbeat",
                    JsonWebToken = token,
                    Uuid = uuid,
                    Time = Convert.ToInt64(elapsed.TotalMilliseconds)

                };

                while (true)
                {
                    Thread.Sleep(3000);//3 seconds
                    DoSend<ClientHeartbeat>(socket, heartbeat);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error sending heartbeat for token {0}: {1}", token,ex.Message), MessageType.Error);
            }

        }
        
        protected void ProcessClientLoginV2(IWebSocketConnection socket, string m)
        {
            ClientLoginRequest wsLogin = JsonConvert.DeserializeObject<ClientLoginRequest>(m);

            try
            {

                JsonCredentials jsonCredentials = AESCryptohandler.DecryptCredentials(wsLogin.Secret, LastTokenGenerated);

                if (!UserRecords.Any(x => x.UserId == jsonCredentials.UserId))
                {
                    DoLog(string.Format("Unknown user: {0}", jsonCredentials.UserId), MessageType.Error);
                    SendLoginRejectReject(socket, wsLogin, string.Format("Unknown user: {0}", jsonCredentials.UserId));
                    return;
                
                }


                if (jsonCredentials.UserId == "SIMUSER_3_UI1" && !wsLogin.ReLogin)
                {
                    DoLog(string.Format("Sending user already logged in for for user {0}", jsonCredentials.UserId), MessageType.Error);
                    SendLoginRejectReject(socket, wsLogin, string.Format("Duplicate Login request", jsonCredentials.UserId, jsonCredentials.UserId));
                    return;
                }

                if (jsonCredentials.Password == "temp123")
                {
                    UserRecord memUsr = UserRecords.Where(x => x.UserId == jsonCredentials.UserId).FirstOrDefault();

                    ClientLoginResponse chgPwdResp = new ClientLoginResponse()
                    {
                        Msg = "ClientLoginResponse",
                        Uuid = wsLogin.Uuid,
                        JsonWebToken = LastTokenGenerated,
                        Success = true,
                        Time = wsLogin.Time,
                        UserId = memUsr != null ? memUsr.UserId : null,
                        PasswordReset = true
                    };

                    DoLog(string.Format("Sending ClientLoginResponse<PasswordReset=true> with UUID {0}", wsLogin.Uuid), MessageType.Information);

                    DoSend<ClientLoginResponse>(socket, chgPwdResp);
                    return;
                
                }


                if (jsonCredentials .Password!= "Testing123")
                {
                    DoLog(string.Format("Wrong password {1} for user {0}", jsonCredentials.UserId, jsonCredentials.Password), MessageType.Error);
                    SendLoginRejectReject(socket, wsLogin, string.Format("Wrong password {1} for user {0}", jsonCredentials.UserId, jsonCredentials.UserId));
                    return;

                }

                UserRecord memUserRecord = UserRecords.Where(x => x.UserId == jsonCredentials.UserId).FirstOrDefault();
                LoggedUserId = memUserRecord.UserId;
                LoggedFirmId = memUserRecord.FirmId;

                ClientLoginResponse loginResp = new ClientLoginResponse()
                {
                    Msg = "ClientLoginResponse",
                    Uuid = wsLogin.Uuid,
                    JsonWebToken = LastTokenGenerated,
                    Success = true,
                    Time = wsLogin.Time,
                    UserId = memUserRecord.UserId
                };

                DoLog(string.Format("Sending ClientLoginResponse with UUID {0}", loginResp.Uuid), MessageType.Information);

                DoSend<ClientLoginResponse>(socket, loginResp);

                SendCRMMessages(socket, jsonCredentials.UserId);

                HeartbeatThread = new Thread(SendHeartbeat);
                HeartbeatThread.Start(new object[] { socket, loginResp.JsonWebToken, loginResp.Uuid });
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception unencrypting secret {0}: {1}", wsLogin.Secret, ex.Message), MessageType.Error);
                SendLoginRejectReject(socket, wsLogin,string.Format("Exception unencrypting secret {0}: {1}", wsLogin.Secret, ex.Message));
            }
        }

        //protected long ProcessOrderIdToLong(string orderId)
        //{
          
        //    try
        //    {
                
        //        Guid guidId = new Guid(orderId);
        //        long fromGuid = GUIDToLongConverter.GUIDToLong(guidId.ToString());
        //        DoLog(string.Format("brkpnt Converting from OrderId {0} to Long {1}", orderId, fromGuid), MessageType.Information);
        //        return fromGuid;

        //    }
        //    catch (Exception ex)
        //    {
        //        try
        //        {
        //            DoLog(string.Format("brkpnt Converting from OrderId {0} as Int 64", orderId), MessageType.Information);
        //            long longOrderId = Convert.ToInt64(orderId);
        //            DoLog(string.Format("brkpnt  OrderId {0} converted to Int 64", longOrderId), MessageType.Information);
        //            return longOrderId;
        //        }
        //        catch (Exception ex2)
        //        {
        //            throw new Exception(string.Format("Invalid Order Id :{0}", orderId));
        //        }
        //    }
        //}

        private void ProcessFakeCancellationRejection(IWebSocketConnection socket, ClientOrderCancelReq ordCxlReq, ClientInstrument instr)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            DoLog(string.Format("Processing Fake cancellation Rejection for security {0}", instr.InstrumentName), MessageType.Information);

            ClientOrderCancelResponse ack = new ClientOrderCancelResponse();
            //ack.ClientOrderId = ordCxlReq.ClientOrderId;
            ack.OrderId = ordCxlReq.OrderId;
            ack.FirmId = ordCxlReq.FirmId;
            ack.Message = string.Format("Rejecting cancelation test for order {0}", ordCxlReq.OrderId);
            ack.Msg = "ClientOrderCancelResponse";
            ack.OrderId = "0";
            ack.Success = false;
            ack.TimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds).ToString();
            ack.UserId = ordCxlReq.UserId;
            ack.Uuid = ordCxlReq.Uuid;

            DoSend<ClientOrderCancelResponse>(socket,ack);
        }

        protected void ProcessClientMassCancelReq(IWebSocketConnection socket, string m)
        {
            DoLog(string.Format("Processing ProcessLegacyOrderMassCancelMock"), MessageType.Information);
            ClientMassCancelReq clientMassCxlReq = JsonConvert.DeserializeObject<ClientMassCancelReq>(m);
            try
            {
                lock (Orders)
                {
                    TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

                    foreach (LegacyOrderRecord order in Orders.Where(x => x.cStatus == LegacyOrderRecord._STATUS_OPEN /*|| x.cStatus == LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/).ToList())
                    {
                        //1-Manamos el ClientOrderCancelResponse
                        ClientOrderCancelResponse ack = new ClientOrderCancelResponse();
                        ack.ClientOrderId = order.ClientOrderId;
                        ack.FirmId = Convert.ToInt64(LoggedFirmId);
                        //ack.Message = "Just cancelled @ mock v2 after mass cancellation";
                        ack.Message = null;
                        ack.Msg = "ClientOrderCancelResponse";
                        ack.OrderId = order.OrderId;
                        ack.Success = true;
                        ack.TimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds).ToString();
                        ack.UserId = clientMassCxlReq.UserId;
                        ack.Uuid = clientMassCxlReq.Uuid;

                        DoLog(string.Format("Sending cancellation ack for ClOrdId: {0}", order.ClientOrderId), MessageType.Information);
                        DoSend<ClientOrderCancelResponse>(socket, ack);

                        //2-Actualizamos el PL
                        DoLog(string.Format("Evaluating price levels for ClOrdId: {0}", order.ClientOrderId), MessageType.Information);
                        ClientInstrument instr = GetInstrumentBySymbol(order.InstrumentId);
                        ClientOrderRecord newOrder = new ClientOrderRecord() { InstrumentId = instr.InstrumentId, cSide = order.cSide, Price = order.Price, LeavesQty = order.LvsQty };
                        EvalPriceLevels(socket, newOrder, clientMassCxlReq.Uuid);

                        //2.1 We update the order status
                        order.cStatus = LegacyOrderRecord._STATUS_CANCELED;
                        order.LvsQty = 0;
                        TranslateAndSendOldLegacyOrderRecord(socket, clientMassCxlReq.Uuid, order, newOrder: false);
                        TranslateAndSendOldLegacyOrderRecordToMyOrders(socket, clientMassCxlReq.Uuid, order, newOrder: false);

                        //3-Upd Quotes
                        UpdateQuotes(socket, instr, clientMassCxlReq.Uuid);

                        //4-We update the trade for potentail exposures
                        UpdateCredit(socket, null, clientMassCxlReq.Uuid);

                    }


                    //4-We send the final response
                    ClientMassCancelResponse resp = new ClientMassCancelResponse();
                    resp.Msg = "ClientMassCancelResponse";
                    resp.UserId = clientMassCxlReq.UserId;
                    resp.Success = true;
                    resp.Message = "";
                    resp.Uuid = clientMassCxlReq.UserId;
                    DoLog(string.Format("Sending ClientMassCancelResponse"), MessageType.Information);
                    DoSend<ClientMassCancelResponse>(socket, resp);

                    //5-We send the final response
                    RefreshOpenOrders(socket, "*", clientMassCxlReq.UserId);
                }
            }
            catch (Exception ex)
            {
                ClientMassCancelResponse resp = new ClientMassCancelResponse();
                resp.Msg = "ClientMassCancelResponse";
                resp.UserId = clientMassCxlReq.UserId;
                resp.Success = false;
                resp.Message = ex.Message;
                resp.Uuid = clientMassCxlReq.UserId;
                DoLog(string.Format("Sending ClientMassCancelResponse"), MessageType.Information);
                DoSend<ClientMassCancelResponse>(socket, resp);
                DoLog(string.Format("Exception processing LegacyOrderMassCancelReq: {0}", ex.Message), MessageType.Error);
            }
        
        }

        protected void ProcessClientOrderCancelReq(IWebSocketConnection socket, string m)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            DoLog(string.Format("Processing ClientOrderCancelReq"), MessageType.Information);
            ClientOrderCancelReq ordCxlReq = JsonConvert.DeserializeObject<ClientOrderCancelReq>(m);

            //TODO: Implement cancellation rejections
            try
            {
                lock (Orders)
                {
                    

                    DoLog(string.Format("Searching order by OrderId={0}", ordCxlReq.OrderId), MessageType.Information);
                    
                    //TKT 1183
                    LegacyOrderRecord order = Orders.Where(x => x.OrderId.Trim() == ordCxlReq.OrderId.ToString()).FirstOrDefault();
                    //LegacyOrderRecord order = Orders.Where(x => x.ClientOrderId == ordCxlReq.ClientOrderId).FirstOrDefault();

                    if (order != null)
                    {


                        ClientInstrument instr = GetInstrumentBySymbol(order.InstrumentId);

                        if (instr.InstrumentName == "SWP-XBT-USD-Z19")
                        {
                            ProcessFakeCancellationRejection(socket, ordCxlReq, instr);
                            return;
                        }

                        //1-We send el CancelAck
                        ClientOrderCancelResponse ack = new ClientOrderCancelResponse();
                        //ack.ClientOrderId = ordCxlReq.ClientOrderId;
                        ack.OrderId = ordCxlReq.OrderId;
                        ack.FirmId = ordCxlReq.FirmId;
                        //ack.Message = "Just cancelled @ mock v2";
                        ack.Message = null;
                        ack.Msg = "ClientOrderCancelResponse";
                        ack.OrderId = order.OrderId;
                        ack.Success = true;
                        ack.TimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds).ToString();
                        ack.UserId = ordCxlReq.UserId;
                        ack.Uuid = ordCxlReq.Uuid;

                        DoLog(string.Format("Sending cancellation ack for OrderId: {0}", ordCxlReq.OrderId), MessageType.Information);
                        DoSend<ClientOrderCancelResponse>(socket, ack);


                        //2-Actualizamos el PL
                        DoLog(string.Format("Evaluating price levels for OrderId: {0}", ordCxlReq.OrderId), MessageType.Information);
                        EvalPriceLevels(socket, new ClientOrderRecord() { InstrumentId = instr.InstrumentId, Symbol = instr.InstrumentName, Price = order.Price, cSide = order.cSide, LeavesQty = order.LvsQty, AccountId = "testAccount" }, ordCxlReq.Uuid);

                        //3-Upd orders in mem
                        DoLog(string.Format("Updating orders in mem"), MessageType.Information);
                        order.cStatus = LegacyOrderRecord._STATUS_CANCELED;
                        order.LvsQty = 0;
                        //Orders.Remove(order);

                        //4- Update Quotes
                        DoLog(string.Format("Updating quotes on order cancelation"), MessageType.Information);
                        UpdateQuotes(socket, instr, ordCxlReq.Uuid);

                        //5-Refreshing open orders
                        RefreshOpenOrders(socket, ordCxlReq.UserId, ordCxlReq.Uuid);

                        //6-Send LegacyOrderRecord
                        CanceledLegacyOrderRecord(socket, order, ordCxlReq.Uuid);

                        //7-we update the credit for potential exposures
                        UpdateCredit(socket, null, ordCxlReq.Uuid);

                    }
                    else
                    {

                        DoLog(string.Format("Last OrderId existing : {0}", Orders.OrderByDescending(x => x.UpdateTime).FirstOrDefault().OrderId), MessageType.Information);

                        //1-Rejecting cancelation because client orderId not found
                        ClientOrderCancelResponse ack = new ClientOrderCancelResponse();
                        //ack.ClientOrderId = ordCxlReq.ClientOrderId;
                        ack.OrderId = ordCxlReq.OrderId;
                        ack.FirmId = ordCxlReq.FirmId;
                        ack.Message = string.Format("Rejecting cancelation because orderId {0} not found", ordCxlReq.OrderId);
                        ack.Msg = "ClientOrderCancelResponse";
                        ack.OrderId = "0";
                        ack.Success = false;
                        ack.TimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds).ToString();
                        ack.UserId = ordCxlReq.UserId;
                        ack.Uuid = ordCxlReq.Uuid;

                        DoLog(string.Format("Rejecting cancelation because orderId not found: {0}", ordCxlReq.OrderId), MessageType.Information);
                        DoSend<ClientOrderCancelResponse>(socket, ack);
                        //RefreshOpenOrders(socket, ordCxlReq.InstrumentId, ordCxlReq.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                //1-Rejecting cancelation because  orderId not found
                ClientOrderCancelResponse ack = new ClientOrderCancelResponse();
                //ack.ClientOrderId = ordCxlReq.ClientOrderId;
                ack.OrderId = ordCxlReq.OrderId;
                ack.FirmId = ordCxlReq.FirmId;
                ack.Message = string.Format("Rejecting cancelation because of an error: {0}", ex.Message);
                ack.Msg = "ClientOrderCancelResponse";
                ack.OrderId = "0";
                ack.Success = false;
                ack.TimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds).ToString();
                ack.UserId = ordCxlReq.UserId;
                ack.Uuid = ordCxlReq.Uuid;
                DoLog(string.Format("Rejecting cancelation because of an error: {0}", ex.Message), MessageType.Information);
                DoSend<ClientOrderCancelResponse>(socket, ack);
            }
        }

        private void TestClientReject(IWebSocketConnection socket, ClientCreditRequest clientCreditReq, ClientInstrument instr)
        {
            if (instr.InstrumentName == "SWP-XBT-USD-X18")
            {
                ClientReject reject = new ClientReject()
                {
                    Msg = "ClientReject",
                    RejectCode = ClientReject._GENERIC_REJECT_CODE,
                    Sender = 0,
                    Text = "Test Client Reject",
                    Time = 0,
                    Uuid = clientCreditReq.Uuid
                };

                DoSend<ClientReject>(socket, reject);

                return;
            }
        }

        private void ProcessClientCreditRequest(IWebSocketConnection socket, string m)
        {

            ClientCreditRequest clientCreditReq = JsonConvert.DeserializeObject<ClientCreditRequest>(m);

            ClientInstrument instr = GetInstrumentByServiceKey(clientCreditReq.InstrumentId);

            try
            {
                TestClientReject(socket,clientCreditReq, instr);


                ClientCreditResponse resp = new ClientCreditResponse()
                {
                    Msg = "ClientCreditResponse",
                    UserId = LoggedUserId,
                    FirmId = clientCreditReq.FirmId,
                    CreditAvailable = true,//later we will decide if we trully have credit available
                    ExposureChange = 0,//later we will decide the real exposure change
                    Success=true,
                    Uuid = clientCreditReq.Uuid
                };

                double deltaExp = GetSecurityPotentialExposure(clientCreditReq.cSide,clientCreditReq.Quantity,instr.InstrumentName, LoggedFirmId);

                if (FirmListResp == null)
                    CreateFirmListCreditStructure(clientCreditReq.Uuid, 0, 10000);
                FirmsCreditRecord firm = FirmListResp.Firms.Where(x => x.FirmId == clientCreditReq.FirmId).FirstOrDefault();

                double neededCredit = firm.UsedCredit + GetTotalSideExposure(clientCreditReq.cSide) + deltaExp;
                double totalCredit = firm.UsedCredit + firm.AvailableCredit;
                resp.CreditAvailable = neededCredit < totalCredit;
                resp.ExposureChange = Convert.ToInt64(deltaExp);

                if (!resp.CreditAvailable)
                    throw new Exception(string.Format("No credit available for the operation. Credit Needed={0} USD (used + exposure + new order exposure). Total Credit={1} USD",
                                                      neededCredit, totalCredit));

                DoSend<ClientCreditResponse>(socket, resp);
            }
            catch (Exception ex)
            {
                double exposure = GetSecurityPotentialExposure(clientCreditReq.cSide, clientCreditReq.Quantity, instr.InstrumentName, LoggedFirmId);
                ClientCreditResponse resp = new ClientCreditResponse()
                {
                    Msg = "ClientCreditResponse",
                    UserId = LoggedUserId,
                    FirmId = clientCreditReq.FirmId,
                    CreditAvailable = false,//later we will decide if we trully have credit available
                    ExposureChange = Convert.ToInt64(exposure),//later we will decide the real exposure change
                    Uuid = clientCreditReq.Uuid,
                    Success=false,
                    Message = ex.Message
                };

                DoSend<ClientCreditResponse>(socket, resp);
            }
        }

        protected void ProcessResetPasswordRequest(IWebSocketConnection socket, string m)
        {

            ResetPasswordRequest resetPwdReq = JsonConvert.DeserializeObject<ResetPasswordRequest>(m);

            try
            {

                JsonCredentials tempCredentials = AESCryptohandler.DecryptCredentials(resetPwdReq.TempSecret, LastTokenGenerated);

                JsonCredentials newCredentials = AESCryptohandler.DecryptCredentials(resetPwdReq.NewSecret, LastTokenGenerated);

                if (tempCredentials.Password != "temp123")//el tempsecret no tiene el pwd temporal valido
                    throw new Exception("Invalid Temp password");

                if (newCredentials.Password == "WillReject70$71")//el new no tiene un pwd válido
                    throw new Exception("Invalid New password!!");


                ClientLoginResponse logged = new ClientLoginResponse()
                {
                    Msg = "ClientLoginResponse",
                    Uuid = resetPwdReq.UUID,
                    JsonWebToken = LastTokenGenerated,
                    Message = null,
                    Success = true,
                    Time = 0,
                    UserId = ""
                };

                DoSend<ClientLoginResponse>(socket, logged);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing ProcessResetPasswordRequest: {0}", ex.Message), MessageType.Error);


                ClientLoginResponse logged = new ClientLoginResponse()
                {
                    Msg = "ClientLoginResponse",
                    Uuid = resetPwdReq.UUID,
                    JsonWebToken = LastTokenGenerated,
                    Message = ex.Message,
                    Success = false,
                    Time = 0,
                    UserId = ""
                };

                DoSend<ClientLoginResponse>(socket, logged);
            }
        
        }

        protected void ProcessForgotPasswordRequest(IWebSocketConnection socket, string m)
        {
            ForgotPasswordRequest forgotPwdReq = JsonConvert.DeserializeObject<ForgotPasswordRequest>(m);

            try
            {


                if (forgotPwdReq.User == "MM3_CLOBUI2")
                    throw new Exception("Cannot change password for user MM3_CLOBUI2");

                ForgotPasswordResponse resp = new ForgotPasswordResponse()
                {
                    Msg = "ForgotPasswordResponse",
                    Uuid = forgotPwdReq.Uuid,
                    Success = true,
                    Message = null,
                    JsonWebToken = forgotPwdReq.JsonWebToken,
                    Sender = 0,
                    Time = "0"
                };

                DoSend<ForgotPasswordResponse>(socket, resp);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing ProcessForgotPasswordRequest: {0}", ex.Message), MessageType.Error);

                ForgotPasswordResponse resp = new ForgotPasswordResponse()
                {
                    Msg = "ForgotPasswordResponse",
                    Uuid = forgotPwdReq.Uuid,
                    Success = false,
                    Message = string.Format("Error updating password:{0}", ex.Message),
                    JsonWebToken = forgotPwdReq.JsonWebToken,
                    Sender = 0,
                    Time = "0"
                };

                DoSend<ForgotPasswordResponse>(socket, resp);
            }
        }

        protected void ProcessOrderReqMock(IWebSocketConnection socket, string m)
        {
            try
            {
                lock (Orders)
                {
                    TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                    ClientOrderReq clientOrderReq = JsonConvert.DeserializeObject<ClientOrderReq>(m);

                    if (!ProcessRejectionsForNewOrders(clientOrderReq, socket))
                    {

                        ClientInstrument instr =  GetInstrumentByServiceKey(clientOrderReq.InstrumentId.ToString());

                        ClientOrderResponse clientOrdAck = new ClientOrderResponse()
                        {
                            Msg = "ClientOrderResponse",
                            ClientOrderId = clientOrderReq.ClientOrderId,
                            InstrumentId = clientOrderReq.InstrumentId,
                            Message = null,
                            Success = true,
                            OrderId = Guid.NewGuid().ToString(),
                            UserId=clientOrderReq.UserId,
                            Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds).ToString(),
                            Uuid = clientOrderReq.Uuid
                        };
                        DoLog(string.Format("Sending ClientOrderResponse ..."), MessageType.Information);
                        DoSend<ClientOrderResponse>(socket, clientOrdAck);


                        if (!EvalTrades(clientOrderReq, clientOrdAck, instr, clientOrderReq.Uuid, socket))
                        {
                            DoLog(string.Format("Evaluating price levels ..."), MessageType.Information);
                            EvalPriceLevelsIfNotTrades(socket, clientOrderReq, instr);
                            DoLog(string.Format("Evaluating LegacyOrderRecord ..."), MessageType.Information);
                            EvalNewOrder(socket, clientOrderReq, clientOrdAck.OrderId, LegacyOrderRecord._STATUS_OPEN, 0, instr, clientOrderReq.Uuid);
                            DoLog(string.Format("Updating quotes ..."), MessageType.Information);
                            UpdateQuotes(socket, instr, clientOrderReq.Uuid);
                            DoLog(string.Format("Updating credit ..."), MessageType.Information);
                            UpdateCredit(socket, null, clientOrderReq.Uuid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing ClientOrderReq: {0}", ex.Message), MessageType.Error);
            }


        }


        #endregion

        #region Private Static Consts

        private static string _REJECTED_SECURITY_ID = "1";

        #endregion

        #region Protected Methods

        protected ClientInstrument GetInstrumentBySymbol(string symbol)
        {
            return  InstrBatch.messages.Where(x => x.InstrumentName == symbol).FirstOrDefault();
        }


        protected ClientInstrument GetInstrumentByIntInstrumentId(string instrumentId)
        {

            ClientInstrument instr = InstrBatch.messages.Where(x => x.InstrumentId == instrumentId).FirstOrDefault();
            return instr;
        }


        protected ClientInstrument GetInstrumentByServiceKey(string serviceKey)
        {
            if (InstrBatch == null)
                throw new Exception("Initial load for instrument not finished!");

            return GetInstrumentByIntInstrumentId(serviceKey);
        }

        protected void ProcessSubscriptionResponse(IWebSocketConnection socket, string service, string serviceKey, string UUID, bool success = true, string msg = "success")
        {
            DGTLBackendMock.Common.DTO.Subscription.V2.SubscriptionResponse resp = new DGTLBackendMock.Common.DTO.Subscription.V2.SubscriptionResponse()
            {
                Message = msg,
                Success = success,
                Service = service,
                ServiceKey = serviceKey,
                Uuid = UUID,
                Msg = "SubscriptionResponse"

            };

            DoLog(string.Format("SubscriptionResponse UUID:{0} Service:{1} ServiceKey:{2} Success:{3}", resp.Uuid, resp.Service, resp.ServiceKey, resp.Success), MessageType.Information);
            DoSend<DGTLBackendMock.Common.DTO.Subscription.V2.SubscriptionResponse>(socket, resp);
        }

        private void LastSaleThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            Subscribe subscrMsg = (Subscribe)paramArray[1];
            ClientInstrument instr = (ClientInstrument)paramArray[2];

            bool subscResp = false;
      
            try
            {
                int i = 0;
                while (true)
                {
                    LastSale legacyLastSale = LastSales.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault();
                    TranslateAndSendOldSale(socket, subscrMsg.Uuid, legacyLastSale, instr);
                    Thread.Sleep(3000);//3 seconds
                    if (!subscResp)
                    {
                        ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.Uuid);
                        Thread.Sleep(2000);
                        subscResp = true;
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing last sales message: {0}...", ex.Message), MessageType.Error);
            }
        }

        private void TranslateAndSendOldSale(IWebSocketConnection socket, string UUID, LastSale legacyLastSale, ClientInstrument instr)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            if (legacyLastSale != null)
            {
                ClientLastSale lastSale = new ClientLastSale()
                {
                    Msg = "ClientLastSale",
                    Change = legacyLastSale.Change.HasValue && legacyLastSale.LastPrice.HasValue ?
                            (decimal?)legacyLastSale.Change.Value * legacyLastSale.LastPrice.Value : null,
                    PercChangePrevDay = legacyLastSale.DiffPrevDay,
                    High = legacyLastSale.High,
                    InstrumentId = instr.InstrumentId,
                    LastPrice = legacyLastSale.LastPrice,
                    LastSize = legacyLastSale.LastShares,
                    Low = legacyLastSale.Low,
                    Open = legacyLastSale.Open,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    Uuid = UUID,
                    Volume = legacyLastSale.Volume

                };

                //EmulatePriceChanges(i, lastSale, ref initialPrice);
                DoSend<ClientLastSale>(socket, lastSale);
            }
            else
            {
                ClientLastSale lastSale = new ClientLastSale()
                {
                    Msg = "ClientLastSale",
                    InstrumentId = instr.InstrumentId,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    Uuid = UUID,
                };

                DoSend<ClientLastSale>(socket, lastSale);
            }
        }

        protected ClientMarketActivity TranslateOldLegacyTradeHistoryForMarketActivity(LegacyTradeHistory legacyTradeHistory, ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            ClientMarketActivity trade = new ClientMarketActivity()
            {
                Msg = "ClientMarketActivity",
                InstrumentId = instr.InstrumentId,
                LastPrice = legacyTradeHistory.TradePrice,
                LastSize = legacyTradeHistory.TradeQuantity,
                cMySide = legacyTradeHistory.cMySide,
                Timestamp = legacyTradeHistory.TradeTimeStamp.ToString(),
                TradeId = legacyTradeHistory.TradeId,
                Uuid = UUID
            };

            return trade;
        }


        protected ClientTradeRecord TranslateOldLegacyTradeHistory(LegacyTradeHistory legacyTradeHistory, ClientInstrument instr,string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            ClientTradeRecord trade = new ClientTradeRecord()
            {
                Msg = "ClientTradeRecord",
                ClientOrderId = null,
                TradeId = legacyTradeHistory.TradeId,
                CreatedAt = legacyTradeHistory.TradeTimeStamp.ToString(),
                cSide = legacyTradeHistory.cMySide,
                cStatus = ClientTradeRecord._STATUS_OPEN,
                ExchangeFees = 0.005 * (legacyTradeHistory.TradePrice * legacyTradeHistory.TradeQuantity),
                FirmId = Convert.ToInt64(LoggedFirmId),
                Symbol = instr.InstrumentName,
                InstrumentId = instr.InstrumentId,
                Notional = (legacyTradeHistory.TradePrice * legacyTradeHistory.TradeQuantity),
                OrderId = "0",
                UpdatedAt = legacyTradeHistory.TradeTimeStamp.ToString(),
                TradePrice = legacyTradeHistory.TradePrice,
                TradeQty = legacyTradeHistory.TradeQuantity,
                UserId = LoggedUserId,
                AccountId="test account",
                Uuid = UUID
            };


            return trade;
        }

        private void TranslateAndSendOldLegacyTradeHistory(IWebSocketConnection socket, string UUID, LegacyTradeHistory legacyTradeHistory)
        {
            
            if (legacyTradeHistory != null)
            {
                ClientInstrument instr = GetInstrumentBySymbol(legacyTradeHistory.Symbol);
                ClientTradeRecord trade = TranslateOldLegacyTradeHistory(legacyTradeHistory, instr, UUID);
                DoSend<ClientTradeRecord>(socket, trade);
            }
        }


        private void TranslateAndSendOldLegacyTradeHistoryToMarketActivity(IWebSocketConnection socket, string UUID, LegacyTradeHistory legacyTradeHistory)
        {

            if (legacyTradeHistory != null)
            {
                ClientInstrument instr = GetInstrumentBySymbol(legacyTradeHistory.Symbol);
                ClientMarketActivity trade = TranslateOldLegacyTradeHistoryForMarketActivity(legacyTradeHistory, instr, UUID);
                DoSend<ClientMarketActivity>(socket, trade);
            }
        }


        protected ClientMyOrders TranslateOldLegacyOrderRecordToMyOrders(LegacyOrderRecord legacyOrderRecord, ClientInstrument instr, bool newOrder, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            
            ClientMyOrders order = new ClientMyOrders()
            {
                Msg = "ClientMyOrders",
                Uuid=UUID,
                OrderId = legacyOrderRecord.OrderId,
                InstrumentId = instr.InstrumentId,
                Price = legacyOrderRecord.Price,
                Quantity = legacyOrderRecord.OrdQty,
                cSide = legacyOrderRecord.cSide,
                LeavesQty = legacyOrderRecord.LvsQty,
                CumQty = legacyOrderRecord.FillQty,
                cStatus = legacyOrderRecord.cStatus,//Both systems V1 and V2 keep the same status
                Timestamp = newOrder ? Convert.ToInt64(elapsed.TotalMilliseconds).ToString() : legacyOrderRecord.UpdateTime.ToString(),
            };

            return order;
        }

        protected ClientOrderRecord TranslateOldLegacyOrderRecord(LegacyOrderRecord legacyOrderRecord, ClientInstrument instr, bool newOrder, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            ClientOrderRecord order = new ClientOrderRecord()
            {
                Msg = "ClientOrderRecord",
                AccountId = "testAcc",
                Contract = instr.InstrumentName,
                Symbol = instr.InstrumentName,
                AveragePrice = legacyOrderRecord.Price,
                ClientOrderId = string.IsNullOrEmpty(legacyOrderRecord.ClientOrderId) ? legacyOrderRecord.OrderId : legacyOrderRecord.ClientOrderId,
                CreateAt = newOrder ? Convert.ToInt64(elapsed.TotalMilliseconds).ToString() : legacyOrderRecord.UpdateTime.ToString(),
                UpdatedAt = newOrder ? Convert.ToInt64(elapsed.TotalMilliseconds).ToString() : legacyOrderRecord.UpdateTime.ToString(),
                EndTime = legacyOrderRecord.IsFinisheStatus() ? legacyOrderRecord.UpdateTime.ToString() : "0",
                StartTime = newOrder ? Convert.ToInt64(elapsed.TotalMilliseconds).ToString() : legacyOrderRecord.UpdateTime.ToString(),
                Time = Convert.ToInt64(elapsed.TotalMilliseconds).ToString(),
                Type = ClientOrderRecord._LIMIT_ORDER_TYPE,
                cSide = legacyOrderRecord.cSide,
                cStatus = legacyOrderRecord.cStatus,//Both systems V1 and V2 keep the same status
                CumQty = legacyOrderRecord.FillQty,
                ExchangeFees = 0,
                //Fees = 0,
                //FirmId = Convert.ToInt64(LoggedFirmId),
                UserId = LoggedUserId,
                InstrumentId = instr.InstrumentId,
                LeavesQty = legacyOrderRecord.LvsQty,
                Notional = legacyOrderRecord.Price.HasValue ? legacyOrderRecord.Price.Value * legacyOrderRecord.OrdQty : 0,
                OrderId = legacyOrderRecord.OrderId,
                Price = legacyOrderRecord.Price,
                LimitPrice = legacyOrderRecord.Price,
                Quantity = legacyOrderRecord.OrdQty,

                //OrderQty = legacyOrderRecord.OrdQty,
                Uuid = UUID,
                OrderRejectReason = legacyOrderRecord.OrdRejReson,
                Sender = 0
            };

            return order;
        }

        private void TranslateAndSendOldDailySettlementPrice(IWebSocketConnection socket,
                                                            DGTLBackendMock.Common.DTO.SecurityList.DailySettlementPrice v1DailySettl,
                                                            ClientInstrument instr,string UUID)
        {
            DGTLBackendMock.Common.DTO.MarketData.V2.ClientDSP v2DailySettl = new Common.DTO.MarketData.V2.ClientDSP();

            TimeSpan elapsed= DateTime.Now - new DateTime(1970, 1, 1);
            
            //TKT 1185
            //v2DailySettl.Msg = "DailySettlementPrice";
            v2DailySettl.Msg = "ClientDSP";
            v2DailySettl.InstrumentId = instr.InstrumentId.ToString();
            v2DailySettl.InstrumentName = instr.InstrumentName;
            v2DailySettl.Uuid = UUID;
            v2DailySettl.CalculationDate =Convert.ToInt32( DateTime.Now.ToString("yyyyMMdd"));
            //v2DailySettl.CalculationTime = Convert.ToInt64(DateTime.Now.ToString("hhmmss"));
            v2DailySettl.CalculationTime = Convert.ToInt64(elapsed.TotalMilliseconds).ToString();
            v2DailySettl.DailySettlementPrice = v1DailySettl.Price;

            DoSend<DGTLBackendMock.Common.DTO.MarketData.V2.ClientDSP>(socket, v2DailySettl);
        
        }

        private void TranslateAndSendOldLegacyOrderRecord(IWebSocketConnection socket, string UUID, LegacyOrderRecord legacyOrderRecord, bool newOrder=true)
        {
            TimeSpan startFromToday = DateTime.Now.Date - new DateTime(1970, 1, 1);
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            if (legacyOrderRecord != null)
            {
                ClientInstrument instr = GetInstrumentBySymbol(legacyOrderRecord.InstrumentId);

                ClientOrderRecord order = TranslateOldLegacyOrderRecord(legacyOrderRecord, instr, newOrder, UUID);

                DoSend<ClientOrderRecord>(socket, order);
            }
        }

        private void TranslateAndSendOldLegacyOrderRecordToMyOrders(IWebSocketConnection socket, string UUID, LegacyOrderRecord legacyOrderRecord, bool newOrder = true)
        {
            TimeSpan startFromToday = DateTime.Now.Date - new DateTime(1970, 1, 1);
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            if (legacyOrderRecord != null)
            {
                ClientInstrument instr = GetInstrumentBySymbol(legacyOrderRecord.InstrumentId);

                ClientMyOrders order = TranslateOldLegacyOrderRecordToMyOrders(legacyOrderRecord, instr, newOrder, UUID);

                DoSend<ClientMyOrders>(socket, order);
            }
        }

        private void TranslateAndSendOldQuote(IWebSocketConnection socket, string UUID, Quote legacyLastQuote, ClientInstrument instr)
        {
            if (legacyLastQuote != null)
            {
                ClientBestBidOffer cBidOffer = new ClientBestBidOffer()
                {
                    Msg = "ClientBestBidOffer",
                    Offer = legacyLastQuote.Ask,
                    OfferSize = legacyLastQuote.AskSize,
                    Bid = legacyLastQuote.Bid,
                    BidSize = legacyLastQuote.BidSize,
                    InstrumentId = instr.InstrumentId,
                    MidPrice = legacyLastQuote.MidPoint,
                    Uuid = UUID,
                };

                DoSend<ClientBestBidOffer>(socket, cBidOffer);
            }
            else
            {
                ClientBestBidOffer cBidOffer = new ClientBestBidOffer()
                {
                    Msg = "ClientBestBidOffer",
                    InstrumentId = instr.InstrumentId,
                    Uuid = UUID,
                };

                DoSend<ClientBestBidOffer>(socket, cBidOffer);
            }
        }

        private ClientDepthOfBook TranslateOldDepthOfBook(DepthOfBook legacyDepthOfBook, ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            ClientDepthOfBook depthOfBook = new ClientDepthOfBook()
            {
                Msg = "ClientDepthOfBook",
                cAction = ClientDepthOfBook.TranslateOldAction(legacyDepthOfBook.cAction),
                cSide = ClientDepthOfBook.TranslateOldSide(legacyDepthOfBook.cBidOrAsk),
                InstrumentId = instr.InstrumentId,
                Price = legacyDepthOfBook.Price,
                Size = legacyDepthOfBook.Size,
                Uuid = UUID,
                Timestamp = Convert.ToInt64(elapsed.TotalSeconds)

            };

            return depthOfBook;
        }

        private void TranslateAndSendOldDepthOfBook(IWebSocketConnection socket, DepthOfBook legacyDepthOfBook, ClientInstrument instr,string UUID)
        {
            ClientDepthOfBook depthOfBook = TranslateOldDepthOfBook(legacyDepthOfBook, instr, UUID);
            DoSend<ClientDepthOfBook>(socket, depthOfBook);
        }

        private double GetPotentialxMargin(char side)
        {
            double acumMargin = 0;

            List<UserRecord> usersForFirm = UserRecords.Where(x => x.FirmId == LoggedFirmId).ToList();

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

                DoLog(string.Format("Potential Contracts for Security {0}  after Orders:{1}", security.Symbol, potentialNetContracts), MessageType.Information);

                DailySettlementPrice DSP = DailySettlementPrices.Where(x => x.Symbol == security.Symbol).FirstOrDefault();

                if (DSP != null && DSP.Price.HasValue)
                {
                    acumMargin += Math.Abs(potentialNetContracts) * DSP.Price.Value * Config.MarginPct;
                }
            }
            

            //TODO : implement the calendar spreads margin calculation
            DoLog(string.Format("Acum Margin for FirmId {0} after Orders:{1}", LoggedFirmId, acumMargin), MessageType.Information);
            return acumMargin - GetFundedMargin(LoggedFirmId);
        }

        private void TranslateAndSendOldCreditRecordUpdate(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            if(FirmListResp==null)
                CreateFirmListCreditStructure(subscrMsg.Uuid, 0, 10000);

            FirmsCreditRecord firm = FirmListResp.Firms.Where(x => x.FirmId ==subscrMsg.ServiceKey).FirstOrDefault();

            if (firm != null)
            {
               

                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                ClientCreditUpdate ccUpd = new ClientCreditUpdate()
                {
                    Msg = "ClientCreditUpdate",
                    AccountId = 0,
                    CreditLimit = firm.AvailableCredit + firm.UsedCredit,
                    CreditUsed = firm.UsedCredit,
                    BuyExposure = GetTotalSideExposure(LegacyOrderRecord._SIDE_BUY),
                    SellExposure = GetTotalSideExposure(LegacyOrderRecord._SIDE_SELL),
                    cStatus = firm.cTradingStatus,
                    cUpdateReason = ClientCreditUpdate._UPDATE_REASON_DEFAULT,
                    FirmId = Convert.ToInt32(firm.FirmId),
                    MaxNotional = firm.MaxNotional,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    Uuid = subscrMsg.Uuid
                };

                DoSend<ClientCreditUpdate>(socket, ccUpd);
            }
            else
                throw new Exception(string.Format("Unknown firm {0}", subscrMsg.ServiceKey));
        }

        private void QuoteThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            Subscribe subscrMsg = (Subscribe)paramArray[1];
            ClientInstrument instr = (ClientInstrument)paramArray[2];
            bool subscResp = false;
            //DoLog(string.Format("chkpnt @QuoteThread"), MessageType.Information);
            try
            {
                while (true)
                {
                    //DoLog(string.Format("chkpnt @QuoteThread2"), MessageType.Information);
                    Quote legacyLastQuote  = Quotes.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault();

                    if (legacyLastQuote != null)
                    {
                        DoLog(string.Format("Sending Quote for instrument {0}", instr.InstrumentName), MessageType.Information);

                        TranslateAndSendOldQuote(socket, subscrMsg.Uuid, legacyLastQuote, instr);
                        Thread.Sleep(3000);//3 seconds
                    }
                    //DoLog(string.Format("chkpnt @QuoteThread3"), MessageType.Information);
                    if (!subscResp)
                    {
                        //DoLog(string.Format("chkpnt @QuoteThread4"), MessageType.Information);
                        ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.Uuid);
                        Thread.Sleep(2000);
                        subscResp = true;
                        SubscribedLQ = true;
                    }

                    if (legacyLastQuote == null)
                        break;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing quote message: {0}...", ex.Message), MessageType.Error);
            }
        }

        private void NewQuoteThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            ClientInstrument instr = (ClientInstrument)paramArray[1];
            string UUID = (string)paramArray[2];

            try
            {
                while (true)
                {
                    Quote quote = null;
                    lock (Quotes)
                    {
                        DoLog(string.Format("Searching quotes for symbol {0}. ", instr.InstrumentName), MessageType.Information);
                        quote = Quotes.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault();
                    }

                    if (quote != null)
                    {
                        DoLog(string.Format("Sending LQ for symbol {0}. Best bid={1} Best ask={2}", instr.InstrumentName, quote.Bid, quote.Ask), MessageType.Information);

                        TranslateAndSendOldQuote(socket, UUID, quote,instr);
                        //DoSend<Quote>(socket, quote);

                    }
                    else
                    {
                        DoLog(string.Format("quotes for symbol {0} not found ", instr.InstrumentName), MessageType.Information);
                    }
                    Thread.Sleep(3000);//3 seconds
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing quote message for symbol {1}: {0}...", ex.Message, instr.InstrumentName), MessageType.Error);
            }
        }

        protected void ProcessLastSale(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            if (!ProcessLastSaleThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {

                    try
                    {
                        ClientInstrument instr = GetInstrumentByServiceKey(subscrMsg.ServiceKey);
                        Thread ProcessLastSaleThread = new Thread(LastSaleThread);
                        ProcessLastSaleThread.Start(new object[] { socket, subscrMsg, instr });
                        ProcessLastSaleThreads.Add(subscrMsg.ServiceKey, ProcessLastSaleThread);
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format(ex.Message), MessageType.Error);
                        ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.Uuid, false, ex.Message);
                    }
                }
            }
            else
            {
                DoLog(string.Format("Double subscription for service LS for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.Uuid, false, "Double subscription");

            }

        }

        protected void ProcessQuote(IWebSocketConnection socket, Subscribe subscrMsg)
        {

            if (!ProcessLastQuoteThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {
                    try
                    {
                        //DoLog(string.Format("chkpnt @ProcessQuote"),MessageType.Information);
                        ClientInstrument instr = GetInstrumentByServiceKey(subscrMsg.ServiceKey);
                        Thread ProcessQuoteThread = new Thread(QuoteThread);
                        ProcessQuoteThread.Start(new object[] { socket, subscrMsg, instr });
                        ProcessLastQuoteThreads.Add(subscrMsg.ServiceKey, ProcessQuoteThread);
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format(ex.Message), MessageType.Error);
                        ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.Uuid, false, ex.Message);
                    }
                }
            }
            else
            {
                DoLog(string.Format("Double subscription for service LQ for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.Uuid, false, "Double subscription");
            }
        }

        protected void UpdateQuotes(IWebSocketConnection socket, ClientInstrument instr, string UUID = null)
        {

            DoLog(string.Format("Updating best bid and ask for symbol {0}", instr.InstrumentName), MessageType.Information);
            DepthOfBook bestBid = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName && x.cBidOrAsk == DepthOfBook._BID_ENTRY).OrderByDescending(x => x.Price).FirstOrDefault();
            DepthOfBook bestAsk = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).OrderBy(x => x.Price).FirstOrDefault();

            lock (Quotes)
            {
                Quote quote = Quotes.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault();

                if (quote != null)
                {
                    if (bestBid != null)
                    {
                        quote.BidSize = bestBid.Size;
                        quote.Bid = bestBid.Price;
                    }
                    else
                    {
                        DoLog(string.Format("Erasing best bid for symbol {0}", instr.InstrumentName), MessageType.Information);
                        quote.BidSize = null;
                        quote.Bid = null;
                    }

                    if (bestAsk != null)
                    {
                        quote.AskSize = bestAsk.Size;
                        quote.Ask = bestAsk.Price;
                    }
                    else
                    {
                        DoLog(string.Format("Erasing best ask for symbol {0}", instr.InstrumentName), MessageType.Information);
                        quote.AskSize = null;
                        quote.Ask = null;
                    }


                    quote.RefreshMidPoint(SecurityMasterRecords.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault().MinPriceIncrement);

                }
                else
                {
                    Quote newQuote = new Quote()
                    {
                        Msg = "Quote",
                        Symbol = instr.InstrumentName
                    };

                    if (bestAsk != null)
                    {
                        newQuote.AskSize = bestAsk.Size;
                        newQuote.Ask = bestAsk.Price;
                    }


                    if (bestBid != null)
                    {
                        newQuote.BidSize = bestBid.Size;
                        newQuote.Bid = bestBid.Price;
                    }

                    newQuote.RefreshMidPoint(SecurityMasterRecords.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault().MinPriceIncrement);

                    DoLog(string.Format("Inserting quotes for symbol {0}", instr.InstrumentName), MessageType.Information);
                    Quotes.Add(newQuote);
                    DoLog(string.Format("Quotes for symbol {0} inserted", instr.InstrumentName), MessageType.Information);

                    Thread ProcessQuoteThread = new Thread(NewQuoteThread);
                    ProcessQuoteThread.Start(new object[] { socket, instr, UUID });

                    if (!ProcessLastQuoteThreads.ContainsKey(instr.InstrumentName))
                        ProcessLastQuoteThreads.Add(instr.InstrumentName, ProcessQuoteThread);
                    else
                        ProcessLastQuoteThreads[instr.InstrumentName] = ProcessQuoteThread;

                }
            }

        }

        protected void ProcessOrderBookDepth(IWebSocketConnection socket, Subscribe subscrMsg)
        {

            try
            {
                ClientInstrument instr = GetInstrumentByServiceKey(subscrMsg.ServiceKey);
                List<DepthOfBook> depthOfBooks = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName).ToList();
                if (depthOfBooks != null && depthOfBooks.Count>0)
                {

                    List<ClientDepthOfBook> newDepthOfBooks = new List<ClientDepthOfBook>();
                    depthOfBooks.ForEach(x => newDepthOfBooks.Add(TranslateOldDepthOfBook(x, instr, subscrMsg.Uuid)));


                    ClientDepthOfBookBatch batchResp = new ClientDepthOfBookBatch()
                    {
                        Msg = "ClientDepthOfBookBatch",
                        messages = newDepthOfBooks.ToArray()
                    };

                    DoSend<ClientDepthOfBookBatch>(socket, batchResp);

                    //depthOfBooks.ForEach(x => TranslateAndSendOldDepthOfBook(socket, x, instr, subscrMsg.Uuid));
                    //Thread.Sleep(1000);
                }

                if(SubscribedLQ)
                    UpdateQuotes(socket, instr, subscrMsg.Uuid);
                ProcessSubscriptionResponse(socket, "LD", subscrMsg.ServiceKey, subscrMsg.Uuid, msg: "success");
            }
            catch (Exception ex)
            {

                DoLog(string.Format(ex.Message), MessageType.Error);
                ProcessSubscriptionResponse(socket, "LD", subscrMsg.ServiceKey, subscrMsg.Uuid, false, ex.Message);
            }
        }

        protected void ProcessCreditRecordUpdates(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            try
            {
                TranslateAndSendOldCreditRecordUpdate(socket, subscrMsg);
                ProcessSubscriptionResponse(socket, "CC", subscrMsg.ServiceKey, subscrMsg.Uuid);
            }
            catch (Exception ex)
            {

                ProcessSubscriptionResponse(socket, "CC", subscrMsg.ServiceKey, subscrMsg.Uuid, success: false, msg: ex.Message);
            }
        }

        protected void ProcessNotifications(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            try
            {
                if (subscrMsg.ServiceKey != "*")
                {
                    if (subscrMsg.ServiceKey.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries).Length != 2)
                        throw new Exception(string.Format("Invalid service key {0}", subscrMsg.ServiceKey));



                    string symbol = subscrMsg.ServiceKey.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    DoLog(string.Format("Subscribe to service TN for symbol {0}", symbol), MessageType.Information);
                    NotificationsSubscriptions.Add(symbol);
                    ProcessSubscriptionResponse(socket, "TN", subscrMsg.ServiceKey, subscrMsg.Uuid, true);

                }
                else
                {

                    DoLog(string.Format("Cannot Subscribe to service TN for generic symbol {0}", subscrMsg.ServiceKey), MessageType.Information);

                    ProcessSubscriptionResponse(socket, "TN", subscrMsg.ServiceKey, subscrMsg.Uuid, false, string.Format("Uknown service key {0}", subscrMsg.Service));
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error Subscribing to service TN for  symbol {0}:{1}", subscrMsg.ServiceKey, ex.Message), MessageType.Information);
                ProcessSubscriptionResponse(socket, "TN", subscrMsg.ServiceKey, subscrMsg.Uuid, false, ex.Message);

            }
        }

        protected void ProcessOpenOrderCount(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            try
            {
                DoLog(string.Format("Subscribe to service Ot "), MessageType.Information);
                RefreshOpenOrders(socket, LoggedUserId, subscrMsg.Uuid);
                ProcessSubscriptionResponse(socket, "Ot", subscrMsg.ServiceKey, subscrMsg.Uuid, true);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error Subscribing to service Ot :{0}", ex.Message), MessageType.Information);
                ProcessSubscriptionResponse(socket, "Ot", subscrMsg.ServiceKey, subscrMsg.Uuid, false, ex.Message);

            }
        }

        protected void ProcessDailySettlementPriceThread(object param)
        {
            DailySettlementPrice v1SettlPrice = (DailySettlementPrice)((object[])param)[0];
            IWebSocketConnection socket = (IWebSocketConnection)((object[])param)[1];
            ClientInstrument instr = (ClientInstrument)((object[])param)[2];
            string UUID = (string)((object[])param)[3];

            try
            {
                while (true)
                {
                    Thread.Sleep(10 * 1000);
                    v1SettlPrice.Price += (double?) 0.01;
                    TranslateAndSendOldDailySettlementPrice(socket, v1SettlPrice, instr, UUID);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error updating Daily Settlement Thread:{0}", ex.Message), MessageType.Error);
            }
        }

        protected void ProcessDailySettlementPrice(IWebSocketConnection socket, Subscribe subscrMsg) 
        {
            try
            {
                ClientInstrument instr = GetInstrumentByIntInstrumentId(subscrMsg.ServiceKey);
                DailySettlementPrice v1SettlPrice = DailySettlementPrices.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault();
                if(v1SettlPrice!=null)
                    TranslateAndSendOldDailySettlementPrice(socket, v1SettlPrice, instr, subscrMsg.Uuid);
                ProcessSubscriptionResponse(socket, "DS", subscrMsg.ServiceKey, subscrMsg.Uuid);

                if (v1SettlPrice != null)
                {
                    Thread processDailySettlementPriceThread = new Thread(ProcessDailySettlementPriceThread);
                    processDailySettlementPriceThread.Start(new object[] { v1SettlPrice, socket, instr, subscrMsg.Uuid });
                }
            }
            catch (Exception ex)
            {
                ProcessSubscriptionResponse(socket, "DS", subscrMsg.ServiceKey, subscrMsg.Uuid,false,ex.Message);
            }
        }

        protected void ProcessMyOrders(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            //string instrumentId = "";
            //string[] fields = subscrMsg.ServiceKey.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);

            //if (fields.Length >= 2)
            //    instrumentId = fields[1];
            //else
            //    throw new Exception(string.Format("Invalid format from ServiceKey. Valid format:UserID@Symbol@[OrderID,*]. Received: {1}", subscrMsg.ServiceKey));

            string instrumentId = subscrMsg.ServiceKey;

            List<LegacyOrderRecord> orders = null;
            if (instrumentId != "*")
            {
                ClientInstrument instr = GetInstrumentByServiceKey(instrumentId);
                orders = Orders.Where(x => x.InstrumentId == instr.InstrumentName).ToList();

                DoLog(string.Format("Sending all orders for {0} subscription. Count={1}", subscrMsg.ServiceKey, orders.Count), MessageType.Information);


                foreach (LegacyOrderRecord order in orders)
                {
                    DoLog(string.Format("Sending order for instrument {0} qty={1} price={2} side={3}", instr.InstrumentId, order.OrdQty, order.Price, order.cSide), MessageType.Information);
                    DoSend<ClientMyOrders>(socket, TranslateOldLegacyOrderRecordToMyOrders(order, instr, false, subscrMsg.Uuid));
                }
            }
            
            //Now we have to launch something to create deltas (insert, change, remove)
            //RefreshOpenOrders(socket, LoggedUserId, subscrMsg.Uuid);
            ProcessSubscriptionResponse(socket, "MO", subscrMsg.ServiceKey, subscrMsg.Uuid);
        }

        protected void ProcessFirmsCreditRecord(IWebSocketConnection socket, Subscribe subscrMsg)
        {

            //we just accept the subscription as there will be no external updates using the mock     
            ProcessSubscriptionResponse(socket, "RC", subscrMsg.ServiceKey, subscrMsg.Uuid);
        }

        protected void ProcessAccountBalance(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            ProcessSubscriptionResponse(socket, "SC", subscrMsg.ServiceKey, subscrMsg.Uuid);

            DoSendAccountBalance(socket, subscrMsg.Uuid);
        }

        protected void ProcessMyTrades(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            List<LegacyTradeHistory> trades = null;
            List<ClientMarketActivity> tradeList = new List<ClientMarketActivity>(); 

            if (subscrMsg.ServiceKey != "*")
            {
                ClientInstrument instr = GetInstrumentByServiceKey(subscrMsg.ServiceKey);
                trades = Trades.Where(x => x.Symbol == instr.InstrumentName).ToList();

                trades.ForEach(x => tradeList.Add(TranslateOldLegacyTradeHistoryForMarketActivity(x, instr, subscrMsg.Uuid)));
                DoSend<ClientMarketActivityBatch>(socket, new ClientMarketActivityBatch(tradeList.ToArray()));

                //trades.ForEach(x => TranslateAndSendOldLegacyTradeHistory(socket, subscrMsg.Uuid, x));
                //trades.ForEach(x => TranslateAndSendOldLegacyTradeHistoryToMarketActivity(socket, subscrMsg.Uuid, x));
            }
           
            //Now we have to launch something to create deltas (insert, change, remove)
            //ProcessSubscriptionResponse(socket, "LT", subscrMsg.ServiceKey, subscrMsg.Uuid);
            ProcessSubscriptionResponse(socket, "MA", subscrMsg.ServiceKey, subscrMsg.Uuid);
        }

        protected void ProcessMyTradesForBlotter(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            if (subscrMsg.ServiceKey == "*")
                ProcessSubscriptionResponse(socket, "rt", subscrMsg.ServiceKey, subscrMsg.Uuid);
            else
                ProcessSubscriptionResponse(socket, "rt", subscrMsg.ServiceKey, subscrMsg.Uuid, false, "rt service has to be suscribed with ServiceKey = *");
        }

        protected void ProcessMyOrdersForBlotter(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            if (subscrMsg.ServiceKey == "*")
                ProcessSubscriptionResponse(socket, "ro", subscrMsg.ServiceKey, subscrMsg.Uuid);
            else
                ProcessSubscriptionResponse(socket, "ro", subscrMsg.ServiceKey, subscrMsg.Uuid, false, "ro service has to be suscribed with ServiceKey = *");
        }

        protected void ProcessClientLogoutV2(IWebSocketConnection socket, string m)
        {
            ClientLogoutRequest wsLogout = JsonConvert.DeserializeObject<ClientLogoutRequest>(m);

            ClientLogoutResponse logout = new ClientLogoutResponse()
            {
                Msg = "ClientLogoutResponse",
                JsonWebToken = wsLogout.JsonWebToken,
                Uuid = wsLogout.Uuid,
                Time = wsLogout.Time,
                Success = true,
                Message = "Successfully logged out"

            };


            DoSend<ClientLogoutResponse>(socket, logout);
            socket.Close();

            if(HeartbeatThread!=null)
                HeartbeatThread.Abort();
        }

        protected  override void OnOpen(IWebSocketConnection socket)
        {
            try
            {
                if (!Connected)
                {
                    DoLog(string.Format("Connecting for the first time to client {0}", socket.ConnectionInfo.ClientPort), MessageType.Information);

                    Connected = true;

                    DoLog(string.Format("Connected for the first time to client {0}", socket.ConnectionInfo.ClientPort), MessageType.Information);

                }
                else
                {
                    DoLog(string.Format("Closing previous client "), MessageType.Information);

                    DoClose(); //We close previous client
                }
            }
            catch (Exception ex)
            {
                if (socket != null && socket.ConnectionInfo.ClientPort != null && socket.ConnectionInfo != null)
                    DoLog(string.Format("Exception at  OnOpen for client {0}: {1}", socket.ConnectionInfo.ClientPort, ex.Message), MessageType.Error);
                else
                    DoLog(string.Format("Exception at  OnOpen for unknown client {0}", ex.Message), MessageType.Error);
            }
        }

        protected override void OnClose(IWebSocketConnection socket)
        {
            try
            {
                DoLog(string.Format(" OnClose for client {0}", socket.ConnectionInfo.ClientPort), MessageType.Information);
                Connected = false;
                DoClose(); 
            }
            catch (Exception ex)
            {
                if (socket != null && socket.ConnectionInfo != null && socket.ConnectionInfo.ClientPort != null)
                    DoLog(string.Format("Exception at  OnClose for client {0}: {1}", socket.ConnectionInfo.ClientPort, ex.Message), MessageType.Error);
                else
                    DoLog(string.Format("Exception at  OnClose for unknown client: {0}", ex.Message), MessageType.Error);

            }
        }

        protected void ProcessSubscriptions(IWebSocketConnection socket, string m)
        {
            Subscribe subscrMsg = JsonConvert.DeserializeObject<Subscribe>(m);

            DoLog(string.Format("Incoming subscription for serviceXX {0} - ServiceKey:{1}", subscrMsg.Service, subscrMsg.ServiceKey), MessageType.Information);

            if (subscrMsg.SubscriptionType == Subscribe._ACTION_SUBSCRIBE)
            {
              
                if (subscrMsg.Service == "LS")
                {
                    if (subscrMsg.ServiceKey != null)
                        ProcessLastSale(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LQ")
                {
                    DoLog("LQ 1", MessageType.Information);
                    if (subscrMsg.ServiceKey != null)
                        ProcessQuote(socket, subscrMsg);
                    else
                        DoLog("LQ 2", MessageType.Information);
                }
                else if (subscrMsg.Service == "LD")
                {
                    ProcessOrderBookDepth(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "CC")
                {
                    ProcessCreditRecordUpdates(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "DS")
                {
                    ProcessDailySettlementPrice(socket, subscrMsg);
                }
                //else if (subscrMsg.Service == "Oy")
                else if (subscrMsg.Service == "MO")
                {
                    ProcessMyOrders(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "MA")
                //else if (subscrMsg.Service == "LT")
                {
                    ProcessMyTrades(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "SC")
                {
                    ProcessAccountBalance(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "RC")
                {
                    ProcessFirmsCreditRecord(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "rt")
                {
                    ProcessMyTradesForBlotter(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "ro")
                {
                    ProcessMyOrdersForBlotter(socket, subscrMsg);
                }
                //else if (subscrMsg.Service == "FP")
                //{
                //    if (subscrMsg.ServiceKey != null)
                //        ProcessOficialFixingPrice(socket, subscrMsg);
                //}
                else if (subscrMsg.Service == "Ot")
                {
                    ProcessOpenOrderCount(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "NC")
                //else if (subscrMsg.Service == "TN")
                {
                    ProcessNotifications(socket, subscrMsg);
                }
                else
                {
                    DoLog(string.Format("Unknown service --{0}--", subscrMsg.Service), MessageType.Information);
                }
                //else if (subscrMsg.Service == "FD")
                //{
                //    if (subscrMsg.ServiceKey != null)
                //        ProcessDailySettlement(socket, subscrMsg);
                //}
         
                //else if (subscrMsg.Service == "FO")
                //{
                //    ProcessFillOffers(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "PS")
                //{
                //    ProcessPlatformStatus(socket, subscrMsg);
                //}
             
                //else if (subscrMsg.Service == "rt")
                //{
                //    ProcessBlotterTrades(socket, subscrMsg);
                //}
            }
            else if (subscrMsg.SubscriptionType == Subscribe._ACTION_UNSUBSCRIBE)
            {
                ProcessUnsubscriptions(subscrMsg);
            }
        }


        protected override void OnMessage(IWebSocketConnection socket, string m)
        {
            try
            {
                WebSocketMessageV2 wsResp = JsonConvert.DeserializeObject<WebSocketMessageV2>(m);

                DoLog(string.Format("OnMessage {1} from IP -> {0}", socket.ConnectionInfo.ClientIpAddress, wsResp.Msg), MessageType.Information);

                if (wsResp.Msg == "ClientLoginRequest")
                {
                    ProcessClientLoginV2(socket, m);
                }
                else if (wsResp.Msg == "ClientLogoutRequest")
                {
                    ProcessClientLogoutV2(socket, m);
                }
                else if (wsResp.Msg == "TokenRequest")
                {
                    ProcessTokenResponse(socket, m);
                }
                else if (wsResp.Msg == "FirmsListRequest")
                {
                    ProcessFirmsListRequest(socket, m);
                }
                else if (wsResp.Msg == "FirmsCreditLimitUpdateRequest")//
                {
                    ProcessFirmsCreditLimitUpdateRequest(socket, m);
                }
                //else if (wsResp.Msg == "FirmsTradingStatusUpdateRequest")//
                //{
                //    ProcessFirmsTradingStatusUpdateRequest(socket, m);
                //}
                else if (wsResp.Msg == "EmailNotificationsListRequest")
                {
                    ProcessEmailNotificationsListRequest(socket, m);
                }
                else if (wsResp.Msg == "EmailNotificationsCreateRequest")
                {
                    ProcessEmailNotificationsCreateRequest(socket, m);
                }
                else if (wsResp.Msg == "EmailNotificationsUpdateRequest")
                {
                    ProcessEmailNotificationsUpdateRequest(socket, m);
                }
                else if (wsResp.Msg == "EmailNotificationsDeleteRequest")
                {
                    ProcessEmailNotificationsDeleteRequest(socket, m);
                }
                else if (wsResp.Msg == "ClientHeartbeat" || wsResp.Msg == "ClientHeartbeatResponse")
                {
                    //We do nothing//

                }
                else if (wsResp.Msg == "ClientOrderReq")
                {

                    ProcessOrderReqMock(socket, m);

                }
                
                else if (wsResp.Msg == "ClientOrderCancelReq")
                {
                    ProcessClientOrderCancelReq(socket, m);
                }
                else if (wsResp.Msg == "ClientMassCancelReq")
                {
                    ProcessClientMassCancelReq(socket, m);
                }
                else if (wsResp.Msg == "ResetPasswordRequest")
                {

                    ProcessResetPasswordRequest(socket, m);

                }
                else if (wsResp.Msg == "ClientCreditRequest")
                {
                    ProcessClientCreditRequest(socket, m);
                }
                else if (wsResp.Msg == "Subscribe")
                {
                    ProcessSubscriptions(socket, m);
                }
                else if (wsResp.Msg == "ForgotPasswordRequest")
                {
                    ProcessForgotPasswordRequest(socket, m);
                }
                else if (wsResp.Msg == "ResetPasswordRequest")
                {
                    ProcessResetPasswordRequest(socket, m);
                }
                else
                {

                    UnknownMessage unknownMsg = new UnknownMessage()
                    {
                        Msg = "MessageReject",
                        Reason = string.Format("Unknown message type {0}", wsResp.Msg)

                    };

                    DoSend<UnknownMessage>(socket, unknownMsg);
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing onMessage:{0}", ex.Message), MessageType.Error);
                UnknownMessage errorMsg = new UnknownMessage()
                {
                    Msg = "MessageReject",
                    Reason = string.Format("Error processing message: {0}", ex.Message)

                };

                DoSend<UnknownMessage>(socket, errorMsg);
            }

        }

        #endregion
    }
}
