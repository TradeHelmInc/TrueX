using DGTLBackendMock.BusinessEntities;
using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Subscription;
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

namespace DGTLBackendMock.DataAccessLayer
{
    
    public class DGTLWebSocketServer : DGTLWebSocketBaseServer
    {
        #region Protected Attributes

        protected SecurityMasterRecord[] SecurityMasterRecords { get; set; }

        protected LastSale[] LastSales { get; set; }

        protected Quote[] Quotes { get; set; }

        protected DailySettlementPrice[] DailySettlementPrices { get; set; }

        protected OfficialFixingPrice[] OfficialFixingPrices { get; set; }

        protected AccountRecord[] AccountRecords { get; set; }

        protected DepthOfBook[] DepthOfBooks { get; set; }

        protected DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate[] CreditRecordUpdates { get; set; }

        protected Dictionary<string, Thread> ProcessLastSaleThreads { get; set; }

        protected Dictionary<string, Thread> ProcessLastQuoteThreads { get; set; }

        protected Dictionary<string, Thread> ProcessDailySettlementThreads { get; set; }

        protected Dictionary<string, Thread> ProcessDailyOfficialFixingPriceThreads { get; set; }


        protected bool Connected { get; set; }

        protected object tLock = new object();

        #endregion

        #region Constructors

        public DGTLWebSocketServer(string pURL)
        {
            URL = pURL;

            HeartbeatSeqNum = 1;

            UserLogged = false;

            Connected = false;

            ProcessLastSaleThreads = new Dictionary<string, Thread>();

            ProcessLastQuoteThreads = new Dictionary<string, Thread>();

            ProcessDailySettlementThreads = new Dictionary<string, Thread>();

            ProcessDailyOfficialFixingPriceThreads = new Dictionary<string, Thread>();

            Logger = new PerDayFileLogSource(Directory.GetCurrentDirectory() + "\\Log", Directory.GetCurrentDirectory() + "\\Log\\Backup")
            {
                FilePattern = "Log.{0:yyyy-MM-dd}.log",
                DeleteDays = 20
            };

            DoLog("Initializing Mock Server...", MessageType.Information);

            try
            {
                DoLog("Initializing all collections...", MessageType.Information);

                LoadUserRecords();

                LoadAccountRecords();

                LoadSecurityMasterRecords();

                LoadLastSales();

                LoadQuotes();

                LoadDailySettlementPrices();

                LoadOfficialFixingPrices();

                LoadCreditRecordUpdates();

                LoadDepthOfBooks();

                DoLog("Collections Initialized...", MessageType.Information);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error initializing collections: {0}...",ex.Message), MessageType.Error);
            }

        }

        #endregion

        #region Private Methods

        private void DoCLoseThread(object p)
        {
            lock (tLock)
            {

                ProcessLastSaleThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessLastSaleThreads.Clear();

                ProcessLastQuoteThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessLastQuoteThreads.Clear();

                ProcessDailyOfficialFixingPriceThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessDailyOfficialFixingPriceThreads.Clear();

                ProcessDailySettlementThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessDailySettlementThreads.Clear();

                Connected = false;

                DoLog("Turning threads off on socket disconnection", MessageType.Information);
            }

        }

        private void DoClose()
        {
            Thread doCloseThread = new Thread(DoCLoseThread);
            doCloseThread.Start();
        }

        protected  void DoSend<T>(IWebSocketConnection socket, T entity)
        {
            string strMsg = JsonConvert.SerializeObject(entity, Newtonsoft.Json.Formatting.None,
                              new JsonSerializerSettings
                              {
                                  NullValueHandling = NullValueHandling.Ignore
                              });

            if (socket.IsAvailable)
            {
                socket.Send(strMsg);
            }
            else
                DoClose();

        }

        private void LoadAccountRecords()
        {
            string strAccountRecords = File.ReadAllText(@".\input\AccountRecord.json");

            //Aca le metemos que serialize el contenido
            AccountRecords = JsonConvert.DeserializeObject<AccountRecord[]>(strAccountRecords);
        }

        private void LoadCreditRecordUpdates()
        {
            string strCreditRecordUpdate = File.ReadAllText(@".\input\CreditRecordUpdate.json");

            //Aca le metemos que serialize el contenido
            CreditRecordUpdates = JsonConvert.DeserializeObject<DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate[]>(strCreditRecordUpdate);
        }

        private void LoadSecurityMasterRecords()
        {
            string strSecurityMasterRecords = File.ReadAllText(@".\input\SecurityMasterRecord.json");

            //Aca le metemos que serialize el contenido
            SecurityMasterRecords = JsonConvert.DeserializeObject<SecurityMasterRecord[]>(strSecurityMasterRecords);
        }

        private void LoadLastSales()
        {
            string strLastSales = File.ReadAllText(@".\input\LastSales.json");

            //Aca le metemos que serialize el contenido
            LastSales = JsonConvert.DeserializeObject<LastSale[]>(strLastSales);
        }

        private void LoadQuotes()
        {
            string strQuotes = File.ReadAllText(@".\input\Quotes.json");

            //Aca le metemos que serialize el contenido
            Quotes = JsonConvert.DeserializeObject<Quote[]>(strQuotes);
        }

        private void LoadDepthOfBooks()
        {
            string strDepthOfBooks = File.ReadAllText(@".\input\DepthOfBook.json");

            //Aca le metemos que serialize el contenido
            DepthOfBooks = JsonConvert.DeserializeObject<DepthOfBook[]>(strDepthOfBooks);
        }

        private void LoadDailySettlementPrices()
        {
            string strDaylySettlementPrices = File.ReadAllText(@".\input\DailySettlementPrice.json");

            //Aca le metemos que serialize el contenido
            DailySettlementPrices = JsonConvert.DeserializeObject<DailySettlementPrice[]>(strDaylySettlementPrices);
        }

        private void LoadOfficialFixingPrices()
        {
            string strOfficialFixingPrices = File.ReadAllText(@".\input\OfficialFixingPrice.json");

            //Aca le metemos que serialize el contenido
            OfficialFixingPrices = JsonConvert.DeserializeObject<OfficialFixingPrice[]>(strOfficialFixingPrices);
        }

        private void LastSaleThread(object param) 
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    LastSale lastSale = LastSales.Where(x => x.Symbol == subscrMsg.ServiceKey).FirstOrDefault();
                    if (lastSale != null)
                    {
                        DoSend<LastSale>(socket, lastSale);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.UUID);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Last Sales not found for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing last sales message: {0}...", ex.Message), MessageType.Error);
            }
        }

        private void QuoteThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    Quote quote = Quotes.Where(x => x.Symbol == subscrMsg.ServiceKey).FirstOrDefault();
                    if (quote != null)
                    {
                        DoSend<Quote>(socket, quote);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Quotes not found for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing quote message: {0}...", ex.Message), MessageType.Error);
            }
        }

        private void DailySettlementThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    DailySettlementPrice dailySettl= DailySettlementPrices.Where(x => x.Symbol == subscrMsg.ServiceKey).FirstOrDefault();
                    if (dailySettl != null)
                    {
                        DoSend<DailySettlementPrice>(socket, dailySettl);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "FP", subscrMsg.ServiceKey, subscrMsg.UUID);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Daily Settlement Price not found for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing daily settlement price message: {0}...", ex.Message), MessageType.Error);

            }
        }

        private void EvalDailyOfficialFixingPriceWarnings(string symbol)
        {
            SecurityMasterRecord security = SecurityMasterRecords.Where(x => x.Symbol == symbol).FirstOrDefault();

            if (security.AssetClass == Security._SPOT)
            { 
                //WE shouldn't have this service requested for a spot currency
                DoLog(string.Format("WARNING1 - Daily Official Fixing Price requested for spot currency! : {0}", symbol), MessageType.Error);
            }
        
        }

        private void EvalDailySettlementPriceWarnings(string symbol)
        {
            SecurityMasterRecord security = SecurityMasterRecords.Where(x => x.Symbol == symbol).FirstOrDefault();

            if (security.AssetClass == Security._SPOT)
            {
                //WE shouldn't have this service requested for a spot currency
                DoLog(string.Format("WARNING2 - Daily Settlement Price requested for spot currency! : {0}", symbol), MessageType.Error);
            }

        }

        private void DailyOfficialFixingPriceThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    OfficialFixingPrice officialFixingPrice = OfficialFixingPrices.Where(x => x.Symbol == subscrMsg.ServiceKey).FirstOrDefault();
                    if (officialFixingPrice != null)
                    {
                        DoSend<OfficialFixingPrice>(socket, officialFixingPrice);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "FD", subscrMsg.ServiceKey, subscrMsg.UUID);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Official Fixing Price not found for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing daily settlement price message: {0}...", ex.Message), MessageType.Error);

            }
        }

        private void ProcessLastSale(IWebSocketConnection socket,WebSocketSubscribeMessage subscrMsg)
        {
            if (!ProcessLastSaleThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {
                    Thread ProcessLastSaleThread = new Thread(LastSaleThread);
                    ProcessLastSaleThread.Start(new object[] { socket, subscrMsg });
                    ProcessLastSaleThreads.Add(subscrMsg.ServiceKey, ProcessLastSaleThread);    
                }
            }
        
        }

        private void ProcessQuote(IWebSocketConnection socket,WebSocketSubscribeMessage subscrMsg)
        {

            if (!ProcessLastQuoteThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {
                    Thread ProcessQuoteThread = new Thread(QuoteThread);
                    ProcessQuoteThread.Start(new object[] { socket, subscrMsg });
                    ProcessLastQuoteThreads.Add(subscrMsg.ServiceKey, ProcessQuoteThread);
                }
            }

        }

        private void ProcessDailySettlement(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            EvalDailySettlementPriceWarnings(subscrMsg.ServiceKey);


            if (!ProcessDailySettlementThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {
                    Thread ProcessDailySettlementThread = new Thread(DailySettlementThread);
                    ProcessDailySettlementThread.Start(new object[] { socket, subscrMsg });
                    ProcessDailySettlementThreads.Add(subscrMsg.ServiceKey, ProcessDailySettlementThread);
                }
            }

        }



        private void ProcessAccountRecord(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            if (subscrMsg.ServiceKey != "*")
            {
                if (subscrMsg.ServiceKey.EndsWith("@*"))
                    subscrMsg.ServiceKey = subscrMsg.ServiceKey.Replace("@*", "");

                List<AccountRecord> accountRecords = AccountRecords.Where(x => x.EPFirmId == subscrMsg.ServiceKey).ToList();

                accountRecords.ForEach(x => DoSend<AccountRecord>(socket,x));
            }
            else
                AccountRecords.ToList().ForEach(x => DoSend<AccountRecord>(socket, x));

            ProcessSubscriptionResponse(socket, "TD", subscrMsg.ServiceKey, subscrMsg.UUID);
        }

        private void ProcessOrderBookDepth(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            List<DepthOfBook> depthOfBooks = DepthOfBooks.Where(x => x.Symbol == subscrMsg.ServiceKey).ToList();
            depthOfBooks.ForEach(x => DoSend<DepthOfBook>(socket, x));
            //Now we have to launch something to create deltas (insert, change, remove)
            Thread.Sleep(1000);
            ProcessSubscriptionResponse(socket, "LD", subscrMsg.ServiceKey, subscrMsg.UUID);
        }


        private void ProcessCreditRecordUpdates(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            if (subscrMsg.ServiceKey != "*")
            {
                if (subscrMsg.ServiceKey.EndsWith("@*"))
                    subscrMsg.ServiceKey = subscrMsg.ServiceKey.Replace("@*", "");

                DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate creditRecordUpdate = CreditRecordUpdates.Where(x => x.FirmId == subscrMsg.ServiceKey).FirstOrDefault();

                if (creditRecordUpdate != null)
                    DoSend<DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate>(socket, creditRecordUpdate);
            }


            ProcessSubscriptionResponse(socket, "CU", subscrMsg.ServiceKey, subscrMsg.UUID);
        }

        private void ProcessOficialFixingPrice(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            EvalDailyOfficialFixingPriceWarnings(subscrMsg.ServiceKey);
            if (!ProcessDailyOfficialFixingPriceThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {
                    Thread ProcessDailyOfficialFixingPriceThread = new Thread(DailyOfficialFixingPriceThread);
                    ProcessDailyOfficialFixingPriceThread.Start(new object[] { socket, subscrMsg });
                    ProcessDailyOfficialFixingPriceThreads.Add(subscrMsg.ServiceKey, ProcessDailyOfficialFixingPriceThread);
                }
            }
        }

        private void ProcessSecurityMasterRecord(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            foreach (SecurityMasterRecord sec in SecurityMasterRecords)
            {
                DoSend<SecurityMasterRecord>(socket, sec);
            }
            Thread.Sleep(2000);
            ProcessSubscriptionResponse(socket, "TA", "*", subscrMsg.UUID);
        }

        private void ProcessLegacyOrderReqMock(IWebSocketConnection socket, string m)
        {

            LegacyOrderReq legOrdReq = JsonConvert.DeserializeObject<LegacyOrderReq>(m);

            TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

            //We send the mock ack
            LegacyOrderAck legOrdAck = new LegacyOrderAck()
            {
                Msg = "LegacyOrderAck",
                OrderId = elaped.TotalSeconds.ToString(),
                UserId = legOrdReq.UserId,
                ClOrderId = legOrdReq.ClOrderId,
                InstrumentId = legOrdReq.InstrumentId,
                Status = "new",
                Price = legOrdReq.Price,
                LeftQty = legOrdReq.Quantity,
                Timestamp = Convert.ToInt64(elaped.TotalSeconds),
            };

            DoSend<LegacyOrderAck>(socket, legOrdAck);
        }

        private void ProcessSubscriptions(IWebSocketConnection socket,string m)
        {
            WebSocketSubscribeMessage subscrMsg = JsonConvert.DeserializeObject<WebSocketSubscribeMessage>(m);

            DoLog(string.Format("Incoming subscription for service {0}", subscrMsg.Service), MessageType.Information);


            if (subscrMsg.Service == "TA")
            {
                ProcessSecurityMasterRecord(socket, subscrMsg);
                
            }
            else if (subscrMsg.Service == "LS")
            {
                if(subscrMsg.ServiceKey!=null)
                    ProcessLastSale(socket,subscrMsg);
            }
            else if (subscrMsg.Service == "LQ")
            {
                if (subscrMsg.ServiceKey != null)
                    ProcessQuote(socket, subscrMsg);
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
            else if (subscrMsg.Service == "TB")
            {
                ProcessUserRecord(socket, subscrMsg);
            }
            else if (subscrMsg.Service == "TD")
            {
                ProcessAccountRecord(socket, subscrMsg);
            }
            else if (subscrMsg.Service == "CU")
            {
                ProcessCreditRecordUpdates(socket, subscrMsg);
            }
            else if (subscrMsg.Service == "LD")
            {
                ProcessOrderBookDepth(socket, subscrMsg);
            }

        }

        #endregion

        #region Protected Methods

        protected override  void OnOpen(IWebSocketConnection socket)
        {
            if (!Connected)
            {
                //socket.Send("Connection Opened");
                Thread heartbeatThread = new Thread(ClientHeartbeatThread);
                heartbeatThread.Start(socket);

                Connected = true;
            }
            else
                socket.Send("Only 1 connection at a time allowed");
        }

        protected override void OnClose(IWebSocketConnection socket)
        {

            DoClose();
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
                else if (wsResp.Msg == "LegacyOrderReq")
                {

                    ProcessLegacyOrderReqMock(socket,m);

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

                  
                    DoSend<UnknownMessage>(socket, unknownMsg);
                }

            }
            catch (Exception ex)
            {
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
