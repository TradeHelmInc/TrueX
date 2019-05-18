using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Subscription;
using DGTLBackendMock.Common.Util;
using DGTLBackendMock.Common.Wrappers;
using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToolsShared.Logging;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;

namespace DGTLBackendMock.DataAccessLayer
{
    public class DGTLWebSocketSimulatedServer : DGTLWebSocketBaseServer
    {
        #region Protected Attributes

        protected SecurityMapping[] SecurityMappings { get; set; }

        protected ICommunicationModule MarketDataModule { get; set; }

        protected ICommunicationModule OrderRoutingModule { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        protected Dictionary<string, Thread> ExecutionReportThreads { get; set; }

        protected Dictionary<string, Thread> OrderCancelReplaceRejectThreads { get; set; }

        protected Dictionary<string, Thread> ProcessLastSaleThreads { get; set; }

        protected Dictionary<string, Thread> ProcessLastQuoteThreads { get; set; }

        protected Dictionary<string, Thread> ProcessOrderBookDepthThreads { get; set; }

        protected Dictionary<string, Queue<ExecutionReport>> ExecutionReports { get; set; }

        protected Dictionary<string, Queue<OrderCancelReplaceReject>> OrderCancelReplaceRejects { get; set; }

        protected ExecutionReportConverter ExecutionReportConverter { get; set; }

        protected int MarketDataRequestCounter { get; set; }

        protected string OySubscriptionUUID { get; set; }

        protected string OyServiceKey { get; set; }

        protected string OyUserId { get; set; }

        protected string LTSubscriptionUUID { get; set; }

        protected string LTServiceKey { get; set; }

        #endregion


        #region Constructors

        public DGTLWebSocketSimulatedServer(string pURL, string pMarketDataModule, 
                                            string pMarketDataModuleConfigFile,
                                            string pOrderRoutingModule,
                                            string pOrderRoutingConfigFile)
        {
            URL = pURL;

            HeartbeatSeqNum = 1;

            MarketDataRequestCounter = 1;

            Connected = false;

            ConnectedClients = new List<int>();

            ExecutionReports = new Dictionary<string, Queue<ExecutionReport>>();

            OrderCancelReplaceRejects = new Dictionary<string, Queue<OrderCancelReplaceReject>>();

            UserLogged = false;

            MarketDataConverter = new MarketDataConverter();

            ExecutionReportConverter = new ExecutionReportConverter();

            ExecutionReportThreads = new Dictionary<string, Thread>();

            OrderCancelReplaceRejectThreads = new Dictionary<string, Thread>();

            ProcessLastSaleThreads = new Dictionary<string, Thread>();

            ProcessLastQuoteThreads = new Dictionary<string, Thread>();

            ProcessOrderBookDepthThreads = new Dictionary<string, Thread>();

            ProcessDailySettlementThreads = new Dictionary<string, Thread>();

            ProcessDailyOfficialFixingPriceThreads = new Dictionary<string, Thread>();

            LoadSecurityMappings();

            LoadDailySettlementPrices();

            LoadOfficialFixingPrices();

            LoadCreditRecordUpdates();

            LoadAccountRecords();

            Logger = new PerDayFileLogSource(Directory.GetCurrentDirectory() + "\\Log", Directory.GetCurrentDirectory() + "\\Log\\Backup")
            {
                FilePattern = "Log.{0:yyyy-MM-dd}.log",
                DeleteDays = 20
            };

            DoLog("Initializing Mock Simulated Server...", MessageType.Information);

            try
            {
                DoLog("Initializing all collections...", MessageType.Information);

                LoadModules(pMarketDataModule, pMarketDataModuleConfigFile, pOrderRoutingModule, pOrderRoutingConfigFile);

                LoadUserRecords();

                LoadSecurityMasterRecords();

                DoLog("Collections Initialized...", MessageType.Information);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error initializing collections: {0}...", ex.Message), MessageType.Error);

            }
        }

        #endregion

        #region Protected Methods

        protected void LoadSecurityMappings()
        {
            string strSecurityMappings = File.ReadAllText(@".\input\SecurityMapping.json");

            //Aca le metemos que serialize el contenido
            SecurityMappings = JsonConvert.DeserializeObject<SecurityMapping[]>(strSecurityMappings);
        }

        private void LoadModules(string pMarketDataModule, string pMarketDataModuleConfigFile, 
                                 string pOrderRoutingModule,string pOrderRoutingConfigFile)
        {
            if (!string.IsNullOrEmpty(pMarketDataModuleConfigFile))
            {
                var typeIncomingModule = Type.GetType(pMarketDataModule);
                if (typeIncomingModule != null)
                {
                    MarketDataModule = (ICommunicationModule)Activator.CreateInstance(typeIncomingModule);
                }
                else
                    DoLog("Assembly not found: " + pMarketDataModule, MessageType.Error);
            }
            else
                DoLog("Incoming Module not found. It will not be initialized", MessageType.Debug);

            MarketDataModule.Initialize(OnMarketDataReceived, OnLogMessage, pMarketDataModuleConfigFile);


            if (!string.IsNullOrEmpty(pOrderRoutingConfigFile))
            {
                var typeIncomingModule = Type.GetType(pOrderRoutingModule);
                if (typeIncomingModule != null)
                {
                    OrderRoutingModule = (ICommunicationModule)Activator.CreateInstance(typeIncomingModule);
                }
                else
                    DoLog("Assembly not found: " + pOrderRoutingModule, MessageType.Error);
            }
            else
                DoLog("Outgoing Module not found. It will not be initialized", MessageType.Debug);

            OrderRoutingModule.Initialize(OnExecutionReportsReceived, OnLogMessage, pOrderRoutingConfigFile);
        
        }

        private void DoSend(IWebSocketConnection socket,string msg)
        {
            if (socket.IsAvailable)
            {
                socket.Send(msg);
            }
            else
                DoClose();
        
        
        }

        private void LoadSecurityMasterRecords()
        {
            string strSecurityMasterRecords = File.ReadAllText(@".\input\SecurityMasterRecord.json");

            //Aca le metemos que serialize el contenido
            SecurityMasterRecords = JsonConvert.DeserializeObject<SecurityMasterRecord[]>(strSecurityMasterRecords);
        }

        private void ProcessSecurityMasterRecord(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            foreach (SecurityMasterRecord sec in SecurityMasterRecords)
            {
                string secMasterRecord = JsonConvert.SerializeObject(sec, Newtonsoft.Json.Formatting.None,
                          new JsonSerializerSettings
                          {
                              NullValueHandling = NullValueHandling.Ignore
                          });


                DoSend(socket,secMasterRecord);
            }
            Thread.Sleep(2000);
            ProcessSubscriptionResponse(socket, "TA", "*", subscrMsg.UUID);
        }

        private void ProcessLastSaleThread(object param)
        {
            object[] parameters = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)parameters[0];
            SecurityMapping secMapping = (SecurityMapping)parameters[1];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)parameters[2];

            try
            {
                while(true)
                {
                    lock (SecurityMappings)
                    {
                        if (secMapping.PublishedMarketDataTrades != null)
                        {
                            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                            LastSale lastSale = new LastSale()
                            {
                                Msg = "LastSale",
                                Sender = 0,
                                LastPrice = Convert.ToDecimal(secMapping.PublishedMarketDataTrades.Trade),
                                LastShares = Convert.ToDecimal(secMapping.PublishedMarketDataTrades.MDTradeSize),
                                Open = Convert.ToDecimal(secMapping.PublishedMarketDataTrades.OpeningPrice),
                                Change = Convert.ToDecimal(secMapping.PublishedMarketDataTrades.PercentageChange),
                                High = Convert.ToDecimal(secMapping.PublishedMarketDataTrades.TradingSessionHighPrice),
                                Low = Convert.ToDecimal(secMapping.PublishedMarketDataTrades.TradingSessionLowPrice),
                                LastTime = Convert.ToInt64(elapsed.TotalSeconds),
                                Symbol = secMapping.IncomingSymbol,
                                Volume = secMapping.PublishedMarketDataTrades.TradeVolume.HasValue ? Convert.ToDecimal(secMapping.PublishedMarketDataTrades.TradeVolume) : 0
                            };

                             string strLastSale = JsonConvert.SerializeObject(lastSale, Newtonsoft.Json.Formatting.None,
                                    new JsonSerializerSettings
                                    {
                                        NullValueHandling = NullValueHandling.Ignore
                                    });

                             DoSend(socket,strLastSale);

                       

                            if (secMapping.PendingLSResponse)
                            {
                                ProcessSubscriptionResponse(socket, "LS", secMapping.IncomingSymbol, subscrMsg.UUID);
                                secMapping.PendingLSResponse = false;
                            }
                    
                        }
                        else if (secMapping.SubscriptionError != null)
                        {
                            if (secMapping.PendingLSResponse)
                            {
                                ProcessSubscriptionResponse(socket, "LS", secMapping.IncomingSymbol, subscrMsg.UUID,false, secMapping.SubscriptionError);
                                secMapping.PendingLSResponse = false;
                            }
                        
                        }
                    
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error publishingLastSale thread:{0}", ex.Message),MessageType.Error);
            
            }
        }

        private void ProcessLastQuoteThread(object param)
        {
            object[] parameters = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)parameters[0];
            SecurityMapping secMapping = (SecurityMapping)parameters[1];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)parameters[2];

            try
            {
                while (true)
                {
                    lock (SecurityMappings)
                    {
                        if (secMapping.PublishedMarketDataQuotes != null)
                        {
                            Quote quote = new Quote()
                            {
                                Msg = "Quote",
                                Symbol=secMapping.IncomingSymbol,
                                Sender = 0,
                                Ask = secMapping.PublishedMarketDataQuotes.BestAskPrice.HasValue ? (decimal?)Convert.ToDecimal(secMapping.PublishedMarketDataQuotes.BestAskPrice) : null,
                                AskSize = secMapping.PublishedMarketDataQuotes.BestAskSize,
                                Bid = secMapping.PublishedMarketDataQuotes.BestBidPrice.HasValue ? (decimal?)Convert.ToDecimal(secMapping.PublishedMarketDataQuotes.BestBidPrice) : null,
                                BidSize = secMapping.PublishedMarketDataQuotes.BestBidSize
                            };

                            string strQuote = JsonConvert.SerializeObject(quote, Newtonsoft.Json.Formatting.None,
                                   new JsonSerializerSettings
                                   {
                                       NullValueHandling = NullValueHandling.Ignore
                                   });

                            DoSend(socket,strQuote);



                            if (secMapping.PendingLQResponse)
                            {
                                ProcessSubscriptionResponse(socket, "LQ", secMapping.IncomingSymbol, subscrMsg.UUID);
                                secMapping.PendingLQResponse = false;
                            }

                        }
                        else if (secMapping.SubscriptionError != null)
                        {
                            if (secMapping.PendingLQResponse)
                            {
                                ProcessSubscriptionResponse(socket, "LQ", secMapping.IncomingSymbol, subscrMsg.UUID, false, secMapping.SubscriptionError);
                                secMapping.PendingLQResponse = false;
                            }

                        }

                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error publishing LastQuote thread:{0}", ex.Message), MessageType.Error);

            }
        }

        private void ProcessOrderBookDepthThread(object param)
        {
            object[] parameters = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)parameters[0];
            SecurityMapping secMapping = (SecurityMapping)parameters[1];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)parameters[2];
            bool firstSent = false;

            try
            {
                while (true)
                {
                    lock (SecurityMappings)
                    {

                        while (secMapping.OrderBookEntriesToPublish.Count > 0)
                        {

                            OrderBookEntry obe = secMapping.OrderBookEntriesToPublish.Dequeue();

                            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

                            DepthOfBook depthOfBook = new DepthOfBook()
                            {
                                Msg = "DepthOfBook",
                                Sender = 0,
                                cAction = AttributeConverter.GetAction(obe.MDUpdateAction),
                                cBidOrAsk = AttributeConverter.GetBidOrAsk(obe.MDEntryType),
                                DepthTime = Convert.ToInt64(elapsed.TotalSeconds),
                                Symbol = secMapping.IncomingSymbol,
                                Size = obe.MDEntrySize,
                                Price = obe.MDEntryPx
                            };


                            DoLog(string.Format("DepthOfBook-> Msg={0} Sender={1}  Action={2}  BidOrAsk={3} DepthTime={4} Symbol={5} Size={6} Price={7}",
                                                 depthOfBook.Msg, depthOfBook.Sender, depthOfBook.Action, depthOfBook.BidOrAsk, depthOfBook.DepthTime, depthOfBook.Symbol, depthOfBook.Size, depthOfBook.Price),
                                                 MessageType.Information);

                            string strDepthOfBook = JsonConvert.SerializeObject(depthOfBook, Newtonsoft.Json.Formatting.None,
                                   new JsonSerializerSettings
                                   {
                                       NullValueHandling = NullValueHandling.Ignore
                                   });
                            firstSent = true;
                            DoSend(socket, strDepthOfBook);


                        }
                    }

                    if (secMapping.PendingLDResponse && firstSent)
                    {
                        ProcessSubscriptionResponse(socket, "LD", secMapping.IncomingSymbol, subscrMsg.UUID);
                        secMapping.PendingLDResponse = false;
                    }

                    if (secMapping.SubscriptionError != null)
                    {
                        if (secMapping.PendingLDResponse)
                        {
                            ProcessSubscriptionResponse(socket, "LD", secMapping.IncomingSymbol, subscrMsg.UUID, false, secMapping.SubscriptionError);
                            secMapping.PendingLDResponse = false;
                        }
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error publishing OrderBookDepth thread:{0}", ex.Message), MessageType.Error);

            }
        }

        private void RejectNewOrder(LegacyOrderReq legOrdReq, string rejReason, IWebSocketConnection socket)
        {
            try
            {
                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

                LegacyOrderRejAck legOrdAck = new LegacyOrderRejAck()
                {
                    Msg = "LegacyOrderRejAck",
                    OrderId = legOrdReq.OrderId,
                    UserId = legOrdReq.UserId,
                    ClOrderId = legOrdReq.ClOrderId,
                    InstrumentId = legOrdReq.InstrumentId,
                    cStatus = LegacyOrderAck._STATUS_REJECTED,
                    Side = legOrdReq.Side,
                    Price = legOrdReq.Price,
                    Quantity = legOrdReq.Quantity,
                    LeftQty = 0,
                    AccountId = legOrdReq.AccountId,
                    Timestamp = Convert.ToInt64(elapsed.TotalSeconds),
                    OrderRejectReason= rejReason
                };

                string strLegacyOrderAck = JsonConvert.SerializeObject(legOrdAck, Newtonsoft.Json.Formatting.None,
                      new JsonSerializerSettings
                      {
                          NullValueHandling = NullValueHandling.Ignore
                      });

                DoSend(socket,strLegacyOrderAck);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error rejecting new order {0}:{1}", legOrdReq.ClOrderId, ex.Message),MessageType.Error);
            }
        }


        private void ProcessResponseMessage(ExecutionReport execReport, SecurityMapping secMapping, string userId,
                                            IWebSocketConnection socket)
        {

            long timestamp = 0;

            if (execReport.TransactTime.HasValue)
            {
                TimeSpan elapsed = execReport.TransactTime.Value - new DateTime(1970, 1, 1);
                timestamp = Convert.ToInt64(elapsed.TotalSeconds);
            }

            if (execReport.OrdStatus == OrdStatus.Rejected || execReport.OrdStatus == OrdStatus.Canceled
                || execReport.OrdStatus == OrdStatus.New)
            {

                LegacyOrderExecutionReport report = null;

                if (execReport.OrdStatus == OrdStatus.Rejected)
                {
                    report = new LegacyOrderRejAck();
                    report.Msg = "LegacyOrderRejAck";
                    ((LegacyOrderRejAck)report).OrderRejectReason = execReport.OrdRejReason + "-" + execReport.Text;

                }
                else if (execReport.OrdStatus == OrdStatus.Canceled)
                {
                    report = new LegacyOrderCancelAck();
                    report.Msg = "LegacyOrderCancelAck";
                    ((LegacyOrderCancelAck)report).OrigClOrderId = execReport.Order.ClOrdId;

                }
                else if (execReport.OrdStatus == OrdStatus.New)
                {
                    report = new LegacyOrderAck();
                    report.Msg = "LegacyOrderAck";

                }

                report.OrderId = execReport.Order.OrderId;
                report.UserId = userId;
                report.ClOrderId = execReport.Order.ClOrdId;
                report.cSide = execReport.Order.Side == Side.Buy ? LegacyOrderAck._SIDE_BUY : LegacyOrderAck._SIDE_SELL;
                report.InstrumentId = secMapping.IncomingSymbol;
                report.cStatus = AttributeConverter.GetExecReportStatus(execReport);
                report.Price = execReport.Order.Price.HasValue ? (decimal?)Convert.ToDecimal(execReport.Order.Price) : null;
                report.Quantity = execReport.Order.OrderQty.HasValue ? Convert.ToDecimal(execReport.Order.OrderQty.Value) : 0;
                report.LeftQty = Convert.ToDecimal(execReport.LeavesQty);
                report.AccountId = execReport.Account;
                report.Timestamp = timestamp;


                string strMsg = JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.None,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });

                DoSend(socket, strMsg);
            }
            else
            {

                if (execReport.OrdStatus == OrdStatus.PartiallyFilled || execReport.OrdStatus == OrdStatus.Filled)
                {

                    if (secMapping.SubscribedOy)
                    {
                        LegacyOrderRecord legRecord = new LegacyOrderRecord();
                        legRecord.ClientOrderId = execReport.Order.ClOrdId;
                        legRecord.cSide = execReport.Order.Side == Side.Buy ? LegacyOrderRecord._SIDE_BUY : LegacyOrderRecord._SIDE_SELL;
                        legRecord.cStatus = AttributeConverter.GetExecReportStatus(execReport);
                        legRecord.FillQty = execReport.CumQty;
                        legRecord.InstrumentId = secMapping.IncomingSymbol;
                        legRecord.LvsQty = execReport.LeavesQty;
                        legRecord.Msg = "LegacyOrderRecord";
                        legRecord.OrderId = execReport.Order.OrderId;
                        legRecord.OrdQty = execReport.Order.OrderQty.HasValue ? execReport.Order.OrderQty.Value : 0;
                        legRecord.Price = execReport.Order.Price;
                        legRecord.Sender = 0;
                        legRecord.UpdateTime = timestamp;
                        legRecord.cTimeInForce = LegacyOrderRecord._TIMEINFORCE_DAY;

                        string strMsg = JsonConvert.SerializeObject(legRecord, Newtonsoft.Json.Formatting.None,
                        new JsonSerializerSettings
                         {
                             NullValueHandling = NullValueHandling.Ignore
                         });

                        DoSend(socket, strMsg);
                    }
                    else
                        DoLog(string.Format("Received {0} report for not subscribed security {1} to service Oy", execReport.OrdStatus.ToString(), secMapping.IncomingSymbol),MessageType.Debug);
                
                
                }
            
            
            
            }
            
        
        }

        private void ProcessExecutionReportsThread(object param)
        {
            object[] parameters = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)parameters[0];
            SecurityMapping secMapping = (SecurityMapping)parameters[1];
            string userId = (string)parameters[2];

            try
            {
                Queue<ExecutionReport> execReports = ExecutionReports[secMapping.IncomingSymbol];

                while (true)
                {
                    while (execReports.Count > 0)
                    {
                        ExecutionReport execReport = null;
                        lock (ExecutionReports)
                        {
                            execReport = execReports.Dequeue();
                        }


                        ProcessResponseMessage(execReport, secMapping, userId, socket);
                    }
                }
                Thread.Sleep(1000);
             
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical Error publishing Execution Report:{0}", ex.Message), MessageType.Error);
            }
        }

        private void ProcessOrderCancelReplaceRejectsThread(object param)
        {
            object[] parameters = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)parameters[0];
            SecurityMapping secMapping = (SecurityMapping)parameters[1];
            string userId = (string)parameters[2];

            try
            {
                Queue<OrderCancelReplaceReject> orderCancelReplaceRejects = OrderCancelReplaceRejects[secMapping.IncomingSymbol];

                while (true)
                {
                    while (orderCancelReplaceRejects.Count > 0)
                    {
                        OrderCancelReplaceReject reject = null;
                        lock (OrderCancelReplaceRejects)
                        {
                            reject = orderCancelReplaceRejects.Dequeue();
                        }

                        long timestamp = 0;


                        LegacyOrderCancelRejAck legOrdCancelRejAck = new LegacyOrderCancelRejAck()
                        {
                            Msg = "LegacyOrderCancelRejAck",
                            ClOrderId = reject.OrigClOrdId,//we want the initial id
                            UserId=userId,
                            InstrumentId = secMapping.IncomingSymbol,
                            OrderId = reject.OrderId,
                            OrderRejectReason = reject.Text,
                        };

                        string strLegacyOrderCancelRejAck = JsonConvert.SerializeObject(legOrdCancelRejAck, Newtonsoft.Json.Formatting.None,
                                new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                });

                        DoSend(socket, strLegacyOrderCancelRejAck);

                    }
                }
                Thread.Sleep(1000);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical Error publishing OrderCancelReplaceReject:{0}", ex.Message), MessageType.Error);
            }
        }

        private void SubscribeMarketDataQuotes(SecurityMapping secMapping)
        {
            DoLog(string.Format("Subscribing market data quotes for outgoing Symbol={0}", secMapping.OutgoingSymbol), MessageType.Information);
            Security sec = new Security()
            {
                Symbol = secMapping.OutgoingSymbol,
                SecType = Security.GetSecurityType("FUT"),
                Currency = "USD",
            };

            MarketDataQuotesRequestWrapper wrapper = new MarketDataQuotesRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
            MarketDataRequestCounter++;
            MarketDataModule.ProcessMessage(wrapper);

            DoLog(string.Format("Quotes Subscription sent for outgoing Symbol={0}", secMapping.OutgoingSymbol), MessageType.Information);

        }

        private void SubscribeMarketDataTrades(SecurityMapping secMapping)
        {
            DoLog(string.Format("Subscribing market data trades for outgoing Symbol={0}", secMapping.OutgoingSymbol), MessageType.Information);
            Security sec = new Security()
            {
                Symbol = secMapping.OutgoingSymbol,
                SecType = Security.GetSecurityType("FUT"),
                Currency = "USD",
            };

            MarketDataTradesRequestWrapper wrapper = new MarketDataTradesRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
            MarketDataRequestCounter++;
            MarketDataModule.ProcessMessage(wrapper);

            DoLog(string.Format("Trades Subscription sent for outgoing Symbol={0}", secMapping.OutgoingSymbol), MessageType.Information);

        }

        private void SubscribeMarketDataOrderBook(SecurityMapping secMapping)
        {
            DoLog(string.Format("Subscribing market data order book for outgoing Symbol={0}", secMapping.OutgoingSymbol), MessageType.Information);
            Security sec = new Security()
            {
                Symbol = secMapping.OutgoingSymbol,
                SecType = Security.GetSecurityType("FUT"),
                Currency = "USD",
            };

            MarketDataOrderBookRequestWrapper wrapper = new MarketDataOrderBookRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
            MarketDataRequestCounter++;
            MarketDataModule.ProcessMessage(wrapper);

            DoLog(string.Format("Order Book Subscription sent for outgoing Symbol={0}", secMapping.OutgoingSymbol), MessageType.Information);

        }

        private void ProcessLastSale(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                if (SecurityMappings.Any(x => x.IncomingSymbol == subscrMsg.ServiceKey))
                {
                    SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == subscrMsg.ServiceKey).FirstOrDefault();

                    SubscribeMarketDataTrades(secMapping);
                    secMapping.SubscribedLS = true;
                    secMapping.PendingLSResponse = true;

                    if (!ProcessLastSaleThreads.ContainsKey(subscrMsg.ServiceKey))
                    {

                        lock (ProcessLastSaleThreads)
                        {
                            Thread processLastSaleThread = new Thread(ProcessLastSaleThread);
                            processLastSaleThread.Start(new object[] { socket, secMapping, subscrMsg });
                            ProcessLastSaleThreads.Add(subscrMsg.ServiceKey, processLastSaleThread);
                        }
                    }
                  
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error subscribing for market data: {0}", ex.Message),MessageType.Information);
                ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
            }
        }

        private void ProcessLastQuote(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                if (SecurityMappings.Any(x => x.IncomingSymbol == subscrMsg.ServiceKey))
                {
                    SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == subscrMsg.ServiceKey).FirstOrDefault();

                    SubscribeMarketDataQuotes(secMapping);
                    secMapping.SubscribedLQ = true;
                    secMapping.PendingLQResponse = true;

                    if (!ProcessLastQuoteThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        lock (ProcessLastQuoteThreads)
                        {
                            Thread processLastQuoteThread = new Thread(ProcessLastQuoteThread);
                            processLastQuoteThread.Start(new object[] { socket, secMapping, subscrMsg });
                            ProcessLastQuoteThreads.Add(subscrMsg.ServiceKey, processLastQuoteThread);
                        }
                    }

                }


            }
            catch (Exception ex)
            {
                ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
            }
        }

        private void ProcessLegacyOrderReqMock(IWebSocketConnection socket, string m)
        {
            LegacyOrderReq legOrdReq = JsonConvert.DeserializeObject<LegacyOrderReq>(m);

            try
            {
                if (SecurityMappings.Any(x => x.IncomingSymbol == legOrdReq.InstrumentId))
                {
                    SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == legOrdReq.InstrumentId).FirstOrDefault();

                    if (!ExecutionReportThreads.ContainsKey(legOrdReq.InstrumentId))
                    {
                        lock (ExecutionReports)
                        {
                            if (!ExecutionReports.ContainsKey(secMapping.IncomingSymbol))
                                ExecutionReports.Add(secMapping.IncomingSymbol, new Queue<ExecutionReport>());
                        }

                        lock (OrderCancelReplaceRejects)
                        {
                            if (!OrderCancelReplaceRejects.ContainsKey(secMapping.IncomingSymbol))
                                OrderCancelReplaceRejects.Add(secMapping.IncomingSymbol, new Queue<OrderCancelReplaceReject>());
                        }

                        lock (ExecutionReportThreads)
                        {
                            Thread execReportThread = new Thread(ProcessExecutionReportsThread);
                            execReportThread.Start(new object[] { socket, secMapping, legOrdReq.UserId });
                            ExecutionReportThreads.Add(legOrdReq.InstrumentId, execReportThread);
                        }

                        lock (OrderCancelReplaceRejects)
                        {
                            Thread orderCancelReplaceRejectsThread = new Thread(ProcessOrderCancelReplaceRejectsThread);
                            orderCancelReplaceRejectsThread.Start(new object[] { socket, secMapping, legOrdReq.UserId });
                            OrderCancelReplaceRejectThreads.Add(legOrdReq.InstrumentId, orderCancelReplaceRejectsThread);
                        }
                    }

                    NewOrderSingleWrapper wrapper = new NewOrderSingleWrapper(legOrdReq, secMapping.OutgoingSymbol);

                    OrderRoutingModule.ProcessMessage(wrapper);

                }
                else
                {
                    RejectNewOrder(legOrdReq, 
                                   string.Format("Error sending new order. Could not find mapping for symbol {0}", legOrdReq.InstrumentId), 
                                   socket);
                
                }

            }
            catch (Exception ex)
            {
                RejectNewOrder(legOrdReq, ex.Message, socket);
            }
        }

        private void ProcessLegacyOrderCancelMock(IWebSocketConnection socket, string m)
        {
            LegacyOrderCancelReq legOrderCancel = JsonConvert.DeserializeObject<LegacyOrderCancelReq>(m);

            if (SecurityMappings.Any(x => x.IncomingSymbol == legOrderCancel.InstrumentId))
            {
                SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == legOrderCancel.InstrumentId).FirstOrDefault();

                OrderCancelRequestWrapper wrapper = new OrderCancelRequestWrapper(legOrderCancel.OrigClOrderId, legOrderCancel.ClOrderId, secMapping.OutgoingSymbol);
                OrderRoutingModule.ProcessMessage(wrapper);
            }
        }

        private void ProcessMyOrders(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                DoLog(string.Format("Entering ProcessMyOrders for ServiceKey={0}", subscrMsg.ServiceKey), MessageType.Information);
                string symbol = subscrMsg.ServiceKey != "*" ? subscrMsg.ServiceKey : null;

                GetOrdersRequestWrapper rq = null;
                if (symbol != null)
                {
                    string[] symbolFields = symbol.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);

                    if (symbolFields.Length >= 2)
                        symbol = symbolFields[1];
                    else
                        throw new Exception(string.Format("Invalid format from ServiceKey. Valid format:UserID@Symbol@[OrderID,*]. Received: {1}", subscrMsg.ServiceKey));



                    SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == symbol).FirstOrDefault();

                    if (secMapping != null)
                    {
                        rq = new GetOrdersRequestWrapper(secMapping.OutgoingSymbol);
                        OrderRoutingModule.ProcessMessage(rq);
                        OySubscriptionUUID = subscrMsg.UUID;
                        OyServiceKey = subscrMsg.ServiceKey;
                        OyUserId = subscrMsg.UserId;
                        secMapping.SubscribedOy = true;
                    }
                    else
                        ProcessSubscriptionResponse(socket, "Oy", subscrMsg.ServiceKey, subscrMsg.UUID, false, string.Format("Unknown symbol {0}", subscrMsg.ServiceKey));
                }
                else
                {
                    rq = new GetOrdersRequestWrapper();
                    OrderRoutingModule.ProcessMessage(rq);
                    OySubscriptionUUID = subscrMsg.UUID;
                    OyServiceKey = subscrMsg.ServiceKey;
                    OyUserId = subscrMsg.UserId;
                }

            }
            catch (Exception ex)
            {
                ProcessSubscriptionResponse(socket, "Oy", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
            }
        }

        private void ProcessMyTrades(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                DoLog(string.Format("Entering ProcessMyTrades for ServiceKey={0}", subscrMsg.ServiceKey), MessageType.Information);
                string symbol = subscrMsg.ServiceKey != "*" ? subscrMsg.ServiceKey : null;

                if (symbol != null)
                {

                    SecurityMapping mapping = SecurityMappings.Where(x => x.IncomingSymbol == symbol).FirstOrDefault();

                    if (mapping == null)
                        throw new Exception(string.Format("Unknown symbol {0}", symbol));
                    else
                        symbol = mapping.OutgoingSymbol;
                }

                GetTradesRequestWrapper rq = new GetTradesRequestWrapper(symbol, SubscriptionRequestType.Snapshot);
                MarketDataModule.ProcessMessage(rq);
                LTSubscriptionUUID = subscrMsg.UUID;
                LTServiceKey = subscrMsg.ServiceKey;

            }
            catch (Exception ex)
            {
                ProcessSubscriptionResponse(socket, "LT", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
            }
        }

        private void ProcessOrderBookDepth(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                DoLog(string.Format("Entering ProcessOrderBookDepth for ServiceKey={0}",subscrMsg.ServiceKey), MessageType.Information);
                if (SecurityMappings.Any(x => x.IncomingSymbol == subscrMsg.ServiceKey))
                {
                    DoLog(string.Format("Searching for mappings for ServiceKey={0}",subscrMsg.ServiceKey), MessageType.Information);
                    SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == subscrMsg.ServiceKey).FirstOrDefault();
                    DoLog(string.Format("Mapping found for Outgoing Symbol={0}", secMapping.OutgoingSymbol), MessageType.Information);

                    SubscribeMarketDataOrderBook(secMapping);
                    secMapping.SubscribedLD = true;
                    secMapping.PendingLDResponse = true;


                    if (!ProcessOrderBookDepthThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        lock (ProcessOrderBookDepthThreads)
                        {
                            Thread processOrderBookDepthThread = new Thread(ProcessOrderBookDepthThread);
                            processOrderBookDepthThread.Start(new object[] { socket, secMapping, subscrMsg });
                            ProcessOrderBookDepthThreads.Add(subscrMsg.ServiceKey, processOrderBookDepthThread);
                        }
                    }

                }


            }
            catch (Exception ex)
            {
                ProcessSubscriptionResponse(socket, "LD", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
            }
        }

        private void ProcessUnsubscriptions(WebSocketSubscribeMessage subscrMsg)
        {
            SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == subscrMsg.ServiceKey).FirstOrDefault();

            if (secMapping == null)
                return;

            lock (SecurityMappings)
            {
              
                if (subscrMsg.Service == "LS")
                {
                    if (ProcessLastSaleThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessLastSaleThreads[subscrMsg.ServiceKey].Abort();
                        ProcessLastSaleThreads.Remove(subscrMsg.ServiceKey);
                        secMapping.SubscriptionError = null;
                        if (secMapping.SubscribedLS)
                        {
                            DoUnsubscribeTrades(secMapping.OutgoingSymbol);
                            secMapping.SubscribedLS = false;
                        }
                    }
                }
                else if (subscrMsg.Service == "LQ")
                {
                    if (ProcessLastQuoteThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessLastQuoteThreads[subscrMsg.ServiceKey].Abort();
                        ProcessLastQuoteThreads.Remove(subscrMsg.ServiceKey);
                        secMapping.SubscriptionError = null;
                        if (secMapping.SubscribedLQ)
                        {
                            DoUnsubscribeQuotes(secMapping.OutgoingSymbol);
                            secMapping.SubscribedLQ = false;
                        }
                    }
                }
              
                else if (subscrMsg.Service == "CU")
                {
                    //ProcessCreditRecordUpdates(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LD")
                {
                    if (ProcessOrderBookDepthThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessOrderBookDepthThreads[subscrMsg.ServiceKey].Abort();
                        ProcessOrderBookDepthThreads.Remove(subscrMsg.ServiceKey);
                        
                        secMapping.SubscriptionError = null;
                        if (secMapping.SubscribedLD)
                        {
                            DoUnsubscribeOrderBook(secMapping.OutgoingSymbol);
                            secMapping.SubscribedLD = false;
                        }
                    }
                }
                else if (subscrMsg.Service == "Oy")
                {
                    secMapping.SubscribedOy = false;
                }
                else if (subscrMsg.Service == "LT")
                {


                }

            }
        
        }

        private void ProcessSubscriptions(IWebSocketConnection socket, string m)
        {
            WebSocketSubscribeMessage subscrMsg = JsonConvert.DeserializeObject<WebSocketSubscribeMessage>(m);

            DoLog(string.Format("Incoming subscription for Service: {0} - Service key: {1} - UUID: {2} SubscriptionType: {3} UserId: {4} JsonToken:{5}", 
                                subscrMsg.Service,subscrMsg.ServiceKey,
                                subscrMsg.UUID, subscrMsg.SubscriptionType,
                                subscrMsg.UserId,subscrMsg.JsonWebToken), MessageType.Information);

            if (subscrMsg.SubscriptionType == WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_SUBSCRIBE)
            {
                if (subscrMsg.Service == "TA")
                {
                    ProcessSecurityMasterRecord(socket, subscrMsg);

                }
                else if (subscrMsg.Service == "LS")
                {
                    if (subscrMsg.ServiceKey != null)
                        ProcessLastSale(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LQ")
                {
                    if (subscrMsg.ServiceKey != null)
                        ProcessLastQuote(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "FP")
                {
                    if (subscrMsg.ServiceKey != null)
                        ProcessOficialFixingPrice(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "FD")
                {
                    if (subscrMsg.ServiceKey != null)
                        ProcessDailySettlement(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "TD")
                {
                    ProcessAccountRecord(socket, subscrMsg);
                }
          
                else if (subscrMsg.Service == "TB")
                {
                    ProcessUserRecord(socket, subscrMsg);
                }
              
                else if (subscrMsg.Service == "CU")
                {
                    ProcessCreditRecordUpdates(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LD")
                {
                    ProcessOrderBookDepth(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "Oy")
                {
                    ProcessMyOrders(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LT")
                {
                    ProcessMyTrades(socket, subscrMsg);
                }
            }
            else if (subscrMsg.SubscriptionType == WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_UNSUBSCRIBE)
            {
                ProcessUnsubscriptions(subscrMsg);
            
            }

        }

        private void ProcessLegacyOrderRecordMessage(ExecutionReport execReport)
        {
            SecurityMapping secMapping = SecurityMappings.Where(x => x.OutgoingSymbol == execReport.Order.Security.Symbol).FirstOrDefault();

            if (secMapping != null)
            {
                TimeSpan elapsed = (execReport.TransactTime.HasValue ? execReport.TransactTime.Value : DateTime.Now) - new DateTime(1970, 1, 1);


                LegacyOrderRecord legOrdRecordMsg = new LegacyOrderRecord();
                legOrdRecordMsg.ClientOrderId = execReport.Order.ClOrdId;
                legOrdRecordMsg.FillQty = execReport.CumQty;
                legOrdRecordMsg.InstrumentId = secMapping.IncomingSymbol;
                legOrdRecordMsg.LvsQty = execReport.LeavesQty;
                legOrdRecordMsg.Msg = "LegacyOrderRecord";
                legOrdRecordMsg.OrderId = execReport.Order.OrderId;
                legOrdRecordMsg.OrdQty = execReport.Order.OrderQty.HasValue ? execReport.Order.OrderQty.Value : 0;
                legOrdRecordMsg.Price = execReport.Order.Price;
                legOrdRecordMsg.Sender = 0;
                legOrdRecordMsg.UserId = OyUserId;
                legOrdRecordMsg.UpdateTime = Convert.ToInt64(elapsed.TotalSeconds);
                legOrdRecordMsg.cSide = execReport.Order.Side == Side.Buy ? LegacyOrderReq._SIDE_BUY : LegacyOrderReq._SIDE_SELL;
                legOrdRecordMsg.cStatus = LegacyOrderRecord.GetStatus(execReport.OrdStatus);
                legOrdRecordMsg.cTimeInForce = LegacyOrderRecord._TIMEINFORCE_DAY;


                string strLegacyOrderRecordMsg = JsonConvert.SerializeObject(legOrdRecordMsg, Newtonsoft.Json.Formatting.None,
                          new JsonSerializerSettings
                          {
                              NullValueHandling = NullValueHandling.Ignore
                          });
                DoSend(ConnectionSocket, strLegacyOrderRecordMsg);
               
            }

            if (execReport.LastReport)
            {
                ProcessSubscriptionResponse(ConnectionSocket, "Oy", OyServiceKey, OySubscriptionUUID);

            }
        
        }

        private void ProcessLegacyTradeHistoryMessage(Trade  trade)
        {
            SecurityMapping secMapping = SecurityMappings.Where(x => x.OutgoingSymbol == trade.Symbol).FirstOrDefault();

            if (secMapping != null)
            {
                LegacyTradeHistory legacyTradeHistoryMsg = new LegacyTradeHistory();
                legacyTradeHistoryMsg.Msg = "LegacyTradeHistory";
                legacyTradeHistoryMsg.Sender = 0;
                legacyTradeHistoryMsg.cMySide = LegacyTradeHistory.GetMySide(trade.Side);
                legacyTradeHistoryMsg.Symbol = secMapping.IncomingSymbol;
                legacyTradeHistoryMsg.TradeId = trade.TradeId;
                legacyTradeHistoryMsg.TradePrice = Convert.ToDouble(trade.Price) ;
                legacyTradeHistoryMsg.TradeQuantity =Convert.ToDouble(trade.Size);
                legacyTradeHistoryMsg.TradeTimeStamp = trade.Timestamp;

                string strLegacyTradeHistoryMsg = JsonConvert.SerializeObject(legacyTradeHistoryMsg, Newtonsoft.Json.Formatting.None,
                          new JsonSerializerSettings
                          {
                              NullValueHandling = NullValueHandling.Ignore
                          });
                DoSend(ConnectionSocket, strLegacyTradeHistoryMsg);

            }

            if (trade.LastTrade)
            {
                ProcessSubscriptionResponse(ConnectionSocket, "LT", LTServiceKey, LTSubscriptionUUID);

            }

        }

        #endregion

        #region Event Methods

        private CMState OnExecutionReportsReceived(Wrapper wrapper)
        {

            if(wrapper.GetAction()==Actions.SECURITY_LIST_REQUEST)
            {
                //We won't return any security list anyway
                return CMState.BuildSuccess();
            
            
            }
            else if (wrapper.GetAction() == Actions.EXECUTION_REPORT )
            {
                ExecutionReport execReport = ExecutionReportConverter.GetExecutionReport(wrapper);

                SecurityMapping secMapping = SecurityMappings.Where(x => x.OutgoingSymbol == execReport.Order.Security.Symbol).FirstOrDefault() ;

                if (secMapping != null)
                {
                    lock (ExecutionReports)
                    {
                        ExecutionReports[secMapping.IncomingSymbol].Enqueue(execReport);
                    }
                }
                return CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.EXECUTION_REPORT_INITIAL_LIST)
            {
                ExecutionReport execReport = ExecutionReportConverter.GetExecutionReport(wrapper);

                ProcessLegacyOrderRecordMessage(execReport);

                return CMState.BuildSuccess();
            
            }
            else if (wrapper.GetAction() == Actions.ORDER_CANCEL_REJECT)
            {
                OrderCancelReplaceReject reject = ExecutionReportConverter.GetOrderCancelReplaceReject(wrapper);

                SecurityMapping secMapping = SecurityMappings.Where(x => x.OutgoingSymbol == reject.Symbol).FirstOrDefault();

                if (secMapping != null)
                {
                    lock (OrderCancelReplaceRejects)
                    {
                        OrderCancelReplaceRejects[secMapping.IncomingSymbol].Enqueue(reject);
                    }
                }
                return CMState.BuildSuccess();
            }

            else
                return CMState.BuildSuccess();
        }

        private void DoUnsubscribeTrades(string symbol)
        {
            Security sec = new Security() { Symbol = symbol, Currency = "USD", SecType = SecurityType.CC };
            MarketDataTradesRequestWrapper wrapper = new MarketDataTradesRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.Unsuscribe);
            MarketDataRequestCounter++;
            MarketDataModule.ProcessMessage(wrapper);

        }

        private void DoUnsubscribeQuotes(string symbol)
        {
            Security sec = new Security() { Symbol = symbol, Currency = "USD", SecType = SecurityType.CC };
            MarketDataQuotesRequestWrapper wrapper = new MarketDataQuotesRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.Unsuscribe);
            MarketDataRequestCounter++;
            MarketDataModule.ProcessMessage(wrapper);

        }

        private void DoUnsubscribeOrderBook(string symbol)
        {
            Security sec = new Security() { Symbol = symbol, Currency = "USD", SecType = SecurityType.CC };
            MarketDataOrderBookRequestWrapper wrapper = new MarketDataOrderBookRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.Unsuscribe);
            MarketDataRequestCounter++;
            MarketDataModule.ProcessMessage(wrapper);

        }

        private void MarkToUnsubscribe(List<string> keys, List<string> symbolsToUnsuscribe)
        {
            foreach (string symbol in keys)
                if (!symbolsToUnsuscribe.Contains(symbol))
                    symbolsToUnsuscribe.Add(symbol);
        
        }

        protected override void DoCLoseThread(object p)
        {
            lock (SecurityMappings)
            {
                try
                {
                    ConnectedClients.Clear();

                    List<string> symbolsToUnsuscribe = new List<string>();

                    ExecutionReportThreads.Values.ToList().ForEach(x => x.Abort());
                    ExecutionReportThreads.Clear();


                    MarkToUnsubscribe(ProcessLastSaleThreads.Keys.ToList(), symbolsToUnsuscribe);
                    ProcessLastSaleThreads.Values.ToList().ForEach(x => x.Abort());
                    ProcessLastSaleThreads.Clear();

                    MarkToUnsubscribe(ProcessLastQuoteThreads.Keys.ToList(), symbolsToUnsuscribe);
                    ProcessLastQuoteThreads.Values.ToList().ForEach(x => x.Abort());
                    ProcessLastQuoteThreads.Clear();

                    MarkToUnsubscribe(ProcessOrderBookDepthThreads.Keys.ToList(), symbolsToUnsuscribe);
                    ProcessOrderBookDepthThreads.Values.ToList().ForEach(x => x.Abort());
                    ProcessOrderBookDepthThreads.Clear();

                    MarkToUnsubscribe(ProcessDailyOfficialFixingPriceThreads.Keys.ToList(), symbolsToUnsuscribe);
                    ProcessDailyOfficialFixingPriceThreads.Values.ToList().ForEach(x => x.Abort());
                    ProcessDailyOfficialFixingPriceThreads.Clear();

                    MarkToUnsubscribe(ProcessDailySettlementThreads.Keys.ToList(), symbolsToUnsuscribe);
                    ProcessDailySettlementThreads.Values.ToList().ForEach(x => x.Abort());
                    ProcessDailySettlementThreads.Clear();

                    MarkToUnsubscribe(ExecutionReportThreads.Keys.ToList(), symbolsToUnsuscribe);
                    ExecutionReportThreads.Values.ToList().ForEach(x => x.Abort());
                    ExecutionReportThreads.Clear();

                    MarkToUnsubscribe(OrderCancelReplaceRejectThreads.Keys.ToList(), symbolsToUnsuscribe);
                    OrderCancelReplaceRejectThreads.Values.ToList().ForEach(x => x.Abort());
                    OrderCancelReplaceRejectThreads.Clear();

                    if (HeartbeatThread != null)
                    {
                        HeartbeatThread.Abort();
                        HeartbeatThread = null;
                    }


                    ExecutionReports.Clear();


                    foreach (string symbol in symbolsToUnsuscribe)
                    {
                        SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == symbol).FirstOrDefault();
                        DoUnsubscribeTrades(secMapping.OutgoingSymbol);
                        DoUnsubscribeQuotes(secMapping.OutgoingSymbol);
                        DoUnsubscribeOrderBook(secMapping.OutgoingSymbol);
                        secMapping.SubscribedLS = false;
                        secMapping.SubscribedLQ = false;
                        secMapping.SubscribedLD = false;
                        secMapping.SubscribedFP = false;
                        secMapping.SubscribedFD = false;
                        secMapping.SubscriptionError = null;
                        secMapping.OrderBookEntriesToPublish.Clear();
                    }

                    Connected = false;

                    UserLogged = false;

                    DoLog("Turning threads off on socket disconnection", MessageType.Information);
                }
                catch (Exception ex)
                {
                    var st = new StackTrace(ex, true);
                    // Get the top stack frame
                    var frame = st.GetFrame(0);
                    // Get the line number from the stack frame
                    var line = frame.GetFileLineNumber();
                    DoLog(string.Format("Exception @DoCLoseThread {0}-{1}- Line {2}", ex.Message, ex.StackTrace, line), MessageType.Error);

                
                }
            }
        
        }

        protected override void DoClose()
        {
            Thread doCloseThread = new Thread(DoCLoseThread);
            doCloseThread.Start();
        }

        private CMState OnMarketDataReceived(Wrapper wrapper)
        {

            if (wrapper.GetAction() == Actions.MARKET_DATA_TRADES)
            {

                zHFT.Main.BusinessEntities.Market_Data.MarketData md = MarketDataConverter.GetMarketData(wrapper);

                try
                {
                    if (SecurityMappings.Any(x => x.OutgoingSymbol == md.Security.Symbol))
                    {
                        SecurityMapping secMapping = (SecurityMapping)SecurityMappings.Where(x => x.OutgoingSymbol == md.Security.Symbol)
                                                                                      .FirstOrDefault();
                        if (secMapping.SubscribedLS)
                        {
                            lock (SecurityMappings)
                            {
                                secMapping.PublishedMarketDataTrades = md;
                            }
                        }
                    }
                   
                    return CMState.BuildSuccess();

                }
                catch (Exception ex)
                {
                    return CMState.BuildFail(ex);
                }
            }
            if (wrapper.GetAction() == Actions.MARKET_DATA_QUOTES)
            {

                zHFT.Main.BusinessEntities.Market_Data.MarketData md = MarketDataConverter.GetMarketData(wrapper);

                try
                {
                    if (SecurityMappings.Any(x => x.OutgoingSymbol == md.Security.Symbol))
                    {
                        SecurityMapping secMapping = (SecurityMapping)SecurityMappings.Where(x => x.OutgoingSymbol == md.Security.Symbol)
                                                                                      .FirstOrDefault();
                        if (secMapping.SubscribedLQ)
                        {
                            lock (SecurityMappings)
                            {
                                secMapping.PublishedMarketDataQuotes = md;
                            }
                        }
                    }

                    return CMState.BuildSuccess();

                }
                catch (Exception ex)
                {
                    return CMState.BuildFail(ex);
                }
            }
            else if (wrapper.GetAction() == Actions.MARKET_DATA_ERROR)
            {
                string symbol = (string) wrapper.GetField(MarketDataFields.Symbol);
                string error = (string) wrapper.GetField(MarketDataFields.Error);

                if (SecurityMappings.Any(x => x.OutgoingSymbol == symbol))
                {
                    SecurityMapping secMapping = (SecurityMapping)SecurityMappings.Where(x => x.OutgoingSymbol == symbol) .FirstOrDefault();

                    if (secMapping.SubscribedMarketData())
                    {
                        secMapping.SubscriptionError = error;
                    }
                }

                return CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.MARKET_DATA_ORDER_BOOK_ENTRY)
            {
                try
                {
                    OrderBookEntry obe = MarketDataConverter.GetOrderBookEntry(wrapper);
                    if (SecurityMappings.Any(x => x.OutgoingSymbol == obe.Symbol))
                    {
                        SecurityMapping secMapping = (SecurityMapping)SecurityMappings.Where(x => x.OutgoingSymbol == obe.Symbol).FirstOrDefault();

                        if (secMapping.SubscribedLD)
                        {
                            lock (SecurityMappings)
                            {
                                secMapping.OrderBookEntriesToPublish.Enqueue(obe);
                            }
                        }


                    }
                    return CMState.BuildSuccess();
                }
                catch (Exception ex)
                {
                    return CMState.BuildFail(ex);
                }
            }
            else if (wrapper.GetAction() == Actions.MARKET_DATA_HISTORICAL_TRADE)
            {
                Trade trade = MarketDataConverter.GetTrade(wrapper);

                ProcessLegacyTradeHistoryMessage(trade);

                return CMState.BuildSuccess();
            }
            else
                return CMState.BuildSuccess();
        }
        

        #endregion

        #region Protected Overriden Methods

        

        protected override void OnMessage(IWebSocketConnection socket, string m)
        {
            try
            {
                WebSocketMessage wsResp = JsonConvert.DeserializeObject<WebSocketMessage>(m);

                if (wsResp.Msg == "ClientLogin")
                {
                    ProcessClientLoginMock(socket, m);
                }
                else if (wsResp.Msg == "ClientHeartbeatResponse")
                {
                    //We do nothing as the DGTL server does

                }
                else if (wsResp.Msg == "ClientLogout")
                {

                    ProcessClientLogoutMock(socket);

                }
                else if (wsResp.Msg == "Subscribe")
                {

                    ProcessSubscriptions(socket, m);

                }
                else if (wsResp.Msg == "LegacyOrderReq")
                {

                    ProcessLegacyOrderReqMock(socket, m);

                }
                else if (wsResp.Msg == "LegacyOrderCancelReq")
                {
                    ProcessLegacyOrderCancelMock(socket, m);
                }
                else
                {
                    UnknownMessage unknownMsg = new UnknownMessage()
                    {
                        Msg = "MessageReject",
                        Reason = string.Format("Unknown message type {0}", wsResp.Msg)

                    };

                    string strUnknownMsg = JsonConvert.SerializeObject(unknownMsg, Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                    DoSend(socket,strUnknownMsg);
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception at  OnMessage for client {0}: {1}", socket.ConnectionInfo.ClientPort, ex.Message), MessageType.Error);

                UnknownMessage errorMsg = new UnknownMessage()
                {
                    Msg = "MessageReject",
                    Reason = string.Format("Error processing message: {0}", ex.Message)

                };

                string strErrorMsg = JsonConvert.SerializeObject(errorMsg, Newtonsoft.Json.Formatting.None,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                DoSend(socket,strErrorMsg);
            }

        }

        #endregion

    
    }
}
