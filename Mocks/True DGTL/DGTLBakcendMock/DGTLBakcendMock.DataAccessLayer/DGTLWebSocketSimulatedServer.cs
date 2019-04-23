﻿using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Subscription;
using DGTLBackendMock.Common.Util;
using DGTLBackendMock.Common.Wrappers;
using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        protected SecurityMasterRecord[] SecurityMasterRecords { get; set; }

        protected SecurityMapping[] SecurityMappings { get; set; }

        protected ICommunicationModule MarketDataModule { get; set; }

        protected ICommunicationModule OrderRoutingModule { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        protected ExecutionReportConverter ExecutionReportConverter { get; set; }

        protected Thread ExecutionReportThread { get; set; }

        protected Queue<ExecutionReport> ExecutionReports { get; set; }

        protected int MarketDataRequestCounter { get; set; }

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

            ExecutionReports = new Queue<ExecutionReport>();

            UserLogged = false;

            MarketDataConverter = new MarketDataConverter();

            ExecutionReportConverter = new ExecutionReportConverter();

            LoadSecurityMappings();

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


                socket.Send(secMasterRecord);
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
                    lock (secMapping)
                    {
                        if (secMapping.PublishedMarketData != null)
                        {
                            LastSale lastSale = new LastSale()
                            {
                                Msg = "LastSale",
                                Sender = 0,
                                LastPrice = Convert.ToDecimal(secMapping.PublishedMarketData.Trade),
                                LastShares = Convert.ToDecimal(secMapping.PublishedMarketData.MDTradeSize),
                                LastTime = 0,
                                Symbol = secMapping.IncomingSymbol,
                                Volume = secMapping.PublishedMarketData.NominalVolume.HasValue ? Convert.ToDecimal(secMapping.PublishedMarketData.NominalVolume) : 0
                            };

                             string strLastSale = JsonConvert.SerializeObject(lastSale, Newtonsoft.Json.Formatting.None,
                                    new JsonSerializerSettings
                                    {
                                        NullValueHandling = NullValueHandling.Ignore
                                    });

                            socket.Send(strLastSale);

                       

                            if (secMapping.PendingLSResponse)
                            {
                                ProcessSubscriptionResponse(socket, "LS", secMapping.IncomingSymbol, subscrMsg.UUID);
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
                    lock (secMapping)
                    {
                        if (secMapping.PublishedMarketData != null)
                        {
                            Quote quote = new Quote()
                            {
                                Msg = "Quote",
                                Sender = 0,
                                Ask = secMapping.PublishedMarketData.BestAskPrice.HasValue ? (decimal?)Convert.ToDecimal(secMapping.PublishedMarketData.BestAskPrice) : null,
                                AskSize = secMapping.PublishedMarketData.BestAskSize,
                                Bid = secMapping.PublishedMarketData.BestBidPrice.HasValue ? (decimal?)Convert.ToDecimal(secMapping.PublishedMarketData.BestBidPrice) : null,
                                BidSize = secMapping.PublishedMarketData.BestBidSize
                            };

                            string strQuote = JsonConvert.SerializeObject(quote, Newtonsoft.Json.Formatting.None,
                                   new JsonSerializerSettings
                                   {
                                       NullValueHandling = NullValueHandling.Ignore
                                   });

                            socket.Send(strQuote);



                            if (secMapping.PendingLQResponse)
                            {
                                ProcessSubscriptionResponse(socket, "LQ", secMapping.IncomingSymbol, subscrMsg.UUID);
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

        private void RejectNewOrder(LegacyOrderReq legOrdReq, string rejReason, IWebSocketConnection socket)
        {
            try
            {
                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

                LegacyOrderAck legOrdAck = new LegacyOrderAck()
                {
                    Msg = "LegacyOrderAck",
                    OrderId = legOrdReq.OrderId,
                    UserId = legOrdReq.UserId,
                    ClOrderId = legOrdReq.ClOrderId,
                    InstrumentId = legOrdReq.InstrumentId,
                    Status = LegacyOrderAck._ORD_sTATUS_REJECTED,
                    Price = legOrdReq.Price,
                    LeftQty = 0,
                    Timestamp = Convert.ToInt64(elapsed.TotalSeconds),
                };

                string strLegacyOrderAck = JsonConvert.SerializeObject(legOrdAck, Newtonsoft.Json.Formatting.None,
                      new JsonSerializerSettings
                      {
                          NullValueHandling = NullValueHandling.Ignore
                      });

                socket.Send(strLegacyOrderAck);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error rejecting new order {0}:{1}", legOrdReq.ClOrderId, ex.Message),MessageType.Error);
            }
        }

        private void ProcessExecutionReportsThread(object param)
        {
            object[] parameters = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)parameters[0];
            string userId = (string)parameters[1];

            try
            {
                while (true)
                {
                    lock (ExecutionReports)
                    {
                        while (ExecutionReports.Count > 0)
                        {
                            ExecutionReport execReport = ExecutionReports.Dequeue();

                            SecurityMapping secMapping = SecurityMappings.Where(x => x.OutgoingSymbol == execReport.Order.Symbol).FirstOrDefault();

                            long timestamp = 0;

                            if (execReport.TransactTime.HasValue)
                            {
                                TimeSpan elapsed = execReport.TransactTime.Value - new DateTime(1970, 1, 1);
                                timestamp = Convert.ToInt64(elapsed.TotalSeconds);
                            }

                            LegacyOrderAck legOrdAck = new LegacyOrderAck()
                            {
                                Msg = "LegacyOrderAck",
                                OrderId = execReport.Order.OrderId,
                                UserId = userId,
                                ClOrderId =execReport.Order.ClOrdId,
                                InstrumentId = secMapping.IncomingSymbol,
                                Status = AttributeConverter.GetExecReportStatus(execReport),
                                Price = execReport.Order.Price.HasValue?(decimal?)Convert.ToDecimal(execReport.Order.Price):null,
                                LeftQty = Convert.ToDecimal(execReport.LeavesQty),
                                Timestamp = timestamp,
                            };

                            string strLegacyOrderAck = JsonConvert.SerializeObject(legOrdAck, Newtonsoft.Json.Formatting.None,
                                  new JsonSerializerSettings
                                  {
                                      NullValueHandling = NullValueHandling.Ignore
                                  });

                            socket.Send(strLegacyOrderAck);
                        
                        }
                    }
                    Thread.Sleep(1000);
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical Error publishing Execution Report:{0}", ex.Message), MessageType.Error);
            }
        }

        private void ProcessOrderBookDepthThread(object param)
        {
            object[] parameters = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)parameters[0];
            SecurityMapping secMapping = (SecurityMapping)parameters[1];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)parameters[2];

            try
            {
                while (true)
                {
                    lock (secMapping)
                    {

                        while (secMapping.OrderBookEntriesToPublish.Count > 0)
                        {

                            OrderBookEntry obe =   secMapping.OrderBookEntriesToPublish.Dequeue();

                            DepthOfBook depthOfBook = new DepthOfBook()
                            {
                                Msg = "DepthOfBook",
                                Sender = 0,
                                cAction = AttributeConverter.GetAction(obe.MDUpdateAction),
                                cBidOrAsk = AttributeConverter.GetBidOrAsk(obe.MDEntryType),
                                DepthTime = 0,
                                Symbol = secMapping.IncomingSymbol,
                                Size = obe.MDEntrySize,
                                Price = obe.MDEntryPx
                            };

                            string strDepthOfBook = JsonConvert.SerializeObject(depthOfBook, Newtonsoft.Json.Formatting.None,
                                   new JsonSerializerSettings
                                   {
                                       NullValueHandling = NullValueHandling.Ignore
                                   });

                            socket.Send(strDepthOfBook);

                            if (secMapping.PendingLDResponse)
                            {
                                ProcessSubscriptionResponse(socket, "LD", secMapping.IncomingSymbol, subscrMsg.UUID);
                                secMapping.PendingLDResponse = false;
                            }

                        
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

        private void SubscribeMarketData(SecurityMapping secMapping)
        {

            Security sec = new Security()
            {
                Symbol = secMapping.OutgoingSymbol,
                SecType = Security.GetSecurityType("FUT"),
                Currency = "USD",
            };

            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
            MarketDataRequestCounter++;
            MarketDataModule.ProcessMessage(wrapper);

        }

        private void ProcessLastSale(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                if (SecurityMappings.Any(x => x.IncomingSymbol == subscrMsg.ServiceKey))
                {
                    SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == subscrMsg.ServiceKey).FirstOrDefault();

                    if (!secMapping.SubscribedMarketData())
                    {
                        SubscribeMarketData(secMapping);
                        secMapping.SubscribedLS = true;
                        secMapping.PendingLSResponse = true;
                    }

                    Thread processLastSaleThread = new Thread(ProcessLastSaleThread);
                    processLastSaleThread.Start(new object[] { socket, secMapping, subscrMsg });
                  
                }
            }
            catch (Exception ex)
            {
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

                    if (!secMapping.SubscribedMarketData())
                    {
                        SubscribeMarketData(secMapping);
                        secMapping.SubscribedLS = true;
                        secMapping.PendingLSResponse = true;
                    
                    }

                    Thread processLastQuoteThread = new Thread(ProcessLastQuoteThread);
                    processLastQuoteThread.Start(new object[] { socket, secMapping, subscrMsg });

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


                    if (ExecutionReportThread == null)
                    {
                        ExecutionReportThread = new Thread(ProcessExecutionReportsThread);
                        ExecutionReportThread.Start(new object[] { socket , legOrdReq.UserId });
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

        private void ProcessOrderBookDepth(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                if (SecurityMappings.Any(x => x.IncomingSymbol == subscrMsg.ServiceKey))
                {
                    SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == subscrMsg.ServiceKey).FirstOrDefault();

                    if (!secMapping.SubscribedMarketData())
                    {
                        SubscribeMarketData(secMapping);
                        secMapping.SubscribedLD = true;
                        secMapping.PendingLDResponse = true;

                    }

                    Thread processOrderBookDepthThread = new Thread(ProcessOrderBookDepthThread);
                    processOrderBookDepthThread.Start(new object[] { socket, secMapping, subscrMsg });

                }


            }
            catch (Exception ex)
            {
                ProcessSubscriptionResponse(socket, "LD", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
            }
        }

        private void ProcessSubscriptions(IWebSocketConnection socket, string m)
        {
            WebSocketSubscribeMessage subscrMsg = JsonConvert.DeserializeObject<WebSocketSubscribeMessage>(m);

            DoLog(string.Format("Incoming subscription for service {0}", subscrMsg.Service), MessageType.Information);


            if (subscrMsg.Service == "TA")
            {
                ProcessSecurityMasterRecord(socket,subscrMsg);

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
                //if (subscrMsg.ServiceKey != null)
                //    ProcessOficialFixingPrice(socket, subscrMsg);
            }
            else if (subscrMsg.Service == "FD")
            {
                //if (subscrMsg.ServiceKey != null)
                //    ProcessDailySettlement(socket, subscrMsg);
            }
            else if (subscrMsg.Service == "TB")
            {
                ProcessUserRecord(socket, subscrMsg);
            }
            else if (subscrMsg.Service == "TD")
            {
                //ProcessAccountRecord(socket, subscrMsg);
            }
            else if (subscrMsg.Service == "CU")
            {
                //ProcessCreditRecordUpdates(socket, subscrMsg);
            }
            else if (subscrMsg.Service == "LD")
            {
                ProcessOrderBookDepth(socket, subscrMsg);
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
            else if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
            {
                ExecutionReport execReport = ExecutionReportConverter.GetExecutionReport(wrapper);

                lock (ExecutionReports)
                {
                    ExecutionReports.Enqueue(execReport);
                }

                return CMState.BuildSuccess();
            }
            else
                return CMState.BuildSuccess();
        }

        private CMState OnMarketDataReceived(Wrapper wrapper)
        {

            if (wrapper.GetAction() == Actions.MARKET_DATA)
            {

                MarketData md = MarketDataConverter.GetMarketData(wrapper);

                try
                {
                    if (SecurityMappings.Any(x => x.OutgoingSymbol == md.Security.Symbol))
                    {
                        SecurityMapping secMapping = (SecurityMapping)SecurityMappings.Where(x => x.OutgoingSymbol == md.Security.Symbol)
                                                                                      .FirstOrDefault();
                        if (secMapping.SubscribedLS || secMapping.SubscribedLQ || secMapping.SubscribedFP || secMapping.SubscribedFD)
                        {
                            lock (secMapping)
                            {
                                secMapping.PublishedMarketData = md;
                            }
                        }
                    }

                    //Well, here we have to publish whatever we got as MD!
                    return CMState.BuildSuccess();

                }
                catch (Exception ex)
                {
                    return CMState.BuildFail(ex);
                }
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
                            lock (secMapping)
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

            else
                return CMState.BuildSuccess();
        }

        #endregion

        #region Protected Overriden Methods

        protected override void OnOpen(IWebSocketConnection socket)
        {
            //socket.Send("Connection Opened");
            Thread heartbeatThread = new Thread(ClientHeartbeatThread);

            heartbeatThread.Start(socket);
        }

        protected override void OnClose(IWebSocketConnection socket)
        {
            ExecutionReportThread.Abort();
            ExecutionReportThread = null;
        }

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
                    socket.Send(strUnknownMsg);
                }

            }
            catch (Exception ex)
            {
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
                socket.Send(strErrorMsg);
            }

        }

        #endregion

    
    }
}
