using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.MarketData;
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
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace DGTLBackendMock.DataAccessLayer
{
    public class DGTLWebSocketSimulatedServer : DGTLWebSocketBaseServer
    {
        #region Protected Attributes

        protected SecurityMasterRecord[] SecurityMasterRecords { get; set; }

        protected SecurityMapping[] SecurityMappings { get; set; }

        protected ICommunicationModule MarketDataModule { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        protected int MarketDataRequestCounter { get; set; }

        #endregion


        #region Constructors

        public DGTLWebSocketSimulatedServer(string pURL, string pMarketDataModule, string pMarketDataModuleConfigFile)
        {
            URL = pURL;

            HeartbeatSeqNum = 1;

            MarketDataRequestCounter = 1;

            UserLogged = false;

            MarketDataConverter = new MarketDataConverter();

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

                LoadModules(pMarketDataModule, pMarketDataModuleConfigFile);

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

        private void LoadModules(string pMarketDataModule, string pMarketDataModuleConfigFile)
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
        
        }

        private void LoadSecurityMasterRecords()
        {
            string strSecurityMasterRecords = File.ReadAllText(@".\input\SecurityMasterRecord.json");

            //Aca le metemos que serialize el contenido
            SecurityMasterRecords = JsonConvert.DeserializeObject<SecurityMasterRecord[]>(strSecurityMasterRecords);
        }

        private void ProcessSecurityMasterRecord(IWebSocketConnection socket)
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
            ProcessSubscriptionResponse(socket, "SubscriptionResponse", "*");
        }

        private void ProcessLastSaleThread(object param)
        {
            object[] parameters = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)parameters[0];
            SecurityMapping secMapping = (SecurityMapping)parameters[1];

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
                                ProcessSubscriptionResponse(socket, "LS", secMapping.IncomingSymbol);
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
                                ProcessSubscriptionResponse(socket, "LQ", secMapping.IncomingSymbol);
                                secMapping.PendingLQResponse = false;
                            }

                        }

                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error publishingLastSale thread:{0}", ex.Message), MessageType.Error);

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

        private void ProcessLastSale(IWebSocketConnection socket, string symbol)
        {
            try
            {
                if (SecurityMappings.Any(x => x.IncomingSymbol == symbol))
                {
                    SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == symbol).FirstOrDefault();

                    if (!secMapping.SubscribedMarketData())
                    {
                        SubscribeMarketData(secMapping);
                        secMapping.SubscribedLS = true;
                        secMapping.PendingLSResponse = true;
                    }

                    Thread processLastSaleThread = new Thread(ProcessLastSaleThread);
                    processLastSaleThread.Start(new object[] { socket, secMapping });
                  
                }
            }
            catch (Exception ex)
            {
                ProcessSubscriptionResponse(socket, "SubscriptionResponse", "*", false, ex.Message);
            }
        }

        private void ProcessLastQuote(IWebSocketConnection socket, string symbol)
        {
            try
            {
                if (SecurityMappings.Any(x => x.IncomingSymbol == symbol))
                {
                    SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == symbol).FirstOrDefault();

                    if (!secMapping.SubscribedMarketData())
                    {
                        SubscribeMarketData(secMapping);
                        secMapping.SubscribedLS = true;
                        secMapping.PendingLSResponse = true;
                    
                    }

                    Thread processLastQuoteThread = new Thread(ProcessLastQuoteThread);
                    processLastQuoteThread.Start(new object[] { socket, secMapping });

                }


            }
            catch (Exception ex)
            {
                ProcessSubscriptionResponse(socket, "SubscriptionResponse", "*", false, ex.Message);
            }
        }

        private void ProcessSubscriptions(IWebSocketConnection socket, string m)
        {
            SubscriptionMsg subscrMsg = JsonConvert.DeserializeObject<SubscriptionMsg>(m);

            DoLog(string.Format("Incoming subscription for service {0}", subscrMsg.Service), MessageType.Information);


            if (subscrMsg.Service == "TA")
            {
                ProcessSecurityMasterRecord(socket);

            }
            else if (subscrMsg.Service == "LS")
            {
                if (subscrMsg.ServiceKey != null)
                    ProcessLastSale(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "LQ")
            {
                if (subscrMsg.ServiceKey != null)
                    ProcessLastQuote(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "FP")
            {
                //if (subscrMsg.ServiceKey != null)
                //    ProcessOficialFixingPrice(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "FD")
            {
                //if (subscrMsg.ServiceKey != null)
                //    ProcessDailySettlement(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "TB")
            {
                ProcessUserRecord(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "TD")
            {
                //ProcessAccountRecord(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "CU")
            {
                //ProcessCreditRecordUpdates(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "LD")
            {
                //ProcessOrderBookDepth(socket, subscrMsg.ServiceKey);
            }

        }

        #endregion

        #region Event Methods

        private CMState OnMarketDataReceived(Wrapper wrapper)
        {
            MarketData md = MarketDataConverter.GetMarketData(wrapper);

            try
            {
                if (SecurityMappings.Any(x => x.OutgoingSymbol == md.Security.Symbol))
                {
                    SecurityMapping secMapping = (SecurityMapping)SecurityMappings.Where(x => x.OutgoingSymbol == md.Security.Symbol)
                                                                                  .FirstOrDefault();
                    if (secMapping.SubscribedLS)
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
