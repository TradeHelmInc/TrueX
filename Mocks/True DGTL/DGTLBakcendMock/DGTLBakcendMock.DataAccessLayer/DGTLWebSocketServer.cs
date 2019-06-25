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

        protected LegacyOrderRecord[] Orders { get; set; }

        protected LegacyTradeHistory[] Trades { get; set; }

        protected LastSale[] LastSales { get; set; }

        protected Quote[] Quotes { get; set; }

        protected DepthOfBook[] DepthOfBooks { get; set; }

        

        protected Dictionary<string, Thread> ProcessLastSaleThreads { get; set; }

        protected Dictionary<string, Thread> ProcessLastQuoteThreads { get; set; }

        protected bool Connected { get; set; }

        

        #endregion

        #region Constructors

        public DGTLWebSocketServer(string pURL)
        {
            URL = pURL;

            HeartbeatSeqNum = 1;

            UserLogged = false;

            Connected = false;

            ConnectedClients = new List<int>();

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

                LoadOrders();

                LoadTrades();

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

        protected override void DoCLoseThread(object p)
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

        protected override void DoClose()
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

        private void LoadOrders()
        {
            string strOrders = File.ReadAllText(@".\input\Orders.json");

            //Aca le metemos que serialize el contenido
            Orders = JsonConvert.DeserializeObject<LegacyOrderRecord[]>(strOrders);
        }

        private void LoadTrades()
        {
            string strTrades = File.ReadAllText(@".\input\Trades.json");

            //Aca le metemos que serialize el contenido
            Trades = JsonConvert.DeserializeObject<LegacyTradeHistory[]>(strTrades);
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


        private void EmulatePriceChanges(int i, LastSale lastSale,ref  decimal? initialPrice)
        {

            if (initialPrice == null)
                initialPrice = lastSale.LastPrice;

            if (i % 2 == 0)
            {
                if (lastSale.High.HasValue)
                    lastSale.High += 0.05m;

                lastSale.LastPrice = lastSale.High;
            }
            else
            {

                if (lastSale.Low.HasValue && lastSale.Low>1)
                    lastSale.Low -= 0.01m;

                lastSale.LastPrice = lastSale.Low;
            }

            if (initialPrice != null)
                lastSale.Change = ((lastSale.LastPrice / initialPrice.Value) - 1) * 100;
            else
                lastSale.Change = 0;
        }

        private void LastSaleThread(object param) 
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)paramArray[1];
            bool subscResp = false;
            decimal? initialPrice=null;
            try
            {
                int i = 0;
                while (true)
                {
                    LastSale lastSale = LastSales.Where(x => x.Symbol == subscrMsg.ServiceKey).FirstOrDefault();
                    if (lastSale != null)
                    {


                        EmulatePriceChanges(i, lastSale, ref initialPrice);
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
                    i++;
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

       

        private void ProcessOrderBookDepth(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            List<DepthOfBook> depthOfBooks = DepthOfBooks.Where(x => x.Symbol == subscrMsg.ServiceKey).ToList();
            depthOfBooks.ForEach(x => DoSend<DepthOfBook>(socket, x));
            //Now we have to launch something to create deltas (insert, change, remove)
            Thread.Sleep(1000);
            ProcessSubscriptionResponse(socket, "LD", subscrMsg.ServiceKey, subscrMsg.UUID);
        }


        private void ProcessMyOrders(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            string symbol = "";
            string[] symbolFields = subscrMsg.ServiceKey.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);

            if (symbolFields.Length >= 2)
                symbol = symbolFields[1];
            else
                throw new Exception(string.Format("Invalid format from ServiceKey. Valid format:UserID@Symbol@[OrderID,*]. Received: {1}", subscrMsg.ServiceKey));


            List<LegacyOrderRecord> orders = Orders.Where(x => x.InstrumentId == symbol).ToList();
            orders.ForEach(x => DoSend<LegacyOrderRecord>(socket, x));
            //Now we have to launch something to create deltas (insert, change, remove)
            ProcessSubscriptionResponse(socket, "Oy", subscrMsg.ServiceKey, subscrMsg.UUID);
        }

        private void ProcessMyTrades(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            List<LegacyTradeHistory> trades = Trades.Where(x => x.Symbol == subscrMsg.ServiceKey).ToList();
            trades.ForEach(x => DoSend<LegacyTradeHistory>(socket, x));
            //Now we have to launch something to create deltas (insert, change, remove)
            ProcessSubscriptionResponse(socket, "LT", subscrMsg.ServiceKey, subscrMsg.UUID);
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

        private void EvalNewOrder(IWebSocketConnection socket, LegacyOrderReq legOrdReq)
        {
            
            TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

            LegacyOrderRecord OyMsg = new LegacyOrderRecord();

            OyMsg.ClientOrderId = legOrdReq.ClOrderId;
            OyMsg.cSide = legOrdReq.cSide;
            OyMsg.cStatus = LegacyOrderRecord._STATUS_OPEN;
            OyMsg.cTimeInForce = LegacyOrderRecord._TIMEINFORCE_DAY;
            OyMsg.FillQty = 0;
            OyMsg.InstrumentId = legOrdReq.InstrumentId;
            OyMsg.LvsQty = Convert.ToDouble(legOrdReq.Quantity) ;
            OyMsg.Msg = "LegacyOrderRecord";
            OyMsg.OrderId = Guid.NewGuid().ToString();
            OyMsg.OrdQty = Convert.ToDouble(legOrdReq.Quantity);
            OyMsg.Price = (double?) legOrdReq.Price;
            OyMsg.Sender = 0;
            OyMsg.UpdateTime = Convert.ToInt64(elaped.TotalMilliseconds);
            OyMsg.UserId = legOrdReq.UserId;

            DoSend<LegacyOrderRecord>(socket, OyMsg);


            //TODO: Add order to memory for cancellations - populate that sctructure with all open orders
        }

        private void EvalPriceLevels(IWebSocketConnection socket, LegacyOrderReq legOrdReq)
        {
            TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

            if (legOrdReq.cSide == LegacyOrderReq._SIDE_BUY)
            {
                DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._BID_ENTRY
                                                                       && x.Price == legOrdReq.Price).FirstOrDefault();

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
                    updPriceLevel.Size = existingPriceLevel.Size + legOrdReq.Quantity;
                    updPriceLevel.Symbol = existingPriceLevel.Symbol;

                    existingPriceLevel.Size = updPriceLevel.Size;
                    DoSend<DepthOfBook>(socket, updPriceLevel);
                }
                else
                {

                    DepthOfBook newPriceLevel = new DepthOfBook();
                    newPriceLevel.cAction = DepthOfBook._ACTION_INSERT;
                    newPriceLevel.cBidOrAsk = DepthOfBook._BID_ENTRY;
                    newPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                    newPriceLevel.Index = 0;
                    newPriceLevel.Msg = "DepthOfBook";
                    newPriceLevel.Price = legOrdReq.Price.HasValue ? legOrdReq.Price.Value : 0;
                    newPriceLevel.Sender = 0;
                    newPriceLevel.Size = legOrdReq.Quantity;
                    newPriceLevel.Symbol = legOrdReq.InstrumentId;

                    List<DepthOfBook> tempList = DepthOfBooks.ToList<DepthOfBook>();
                    tempList.Add(newPriceLevel);
                    DepthOfBooks=tempList.ToArray();

                    DoSend<DepthOfBook>(socket, newPriceLevel);
                }

            }
            else if (legOrdReq.cSide == LegacyOrderReq._SIDE_SELL)
            {
                DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._ASK_ENTRY
                                                                       && x.Price == legOrdReq.Price).FirstOrDefault();

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
                    updPriceLevel.Size = existingPriceLevel.Size + legOrdReq.Quantity;
                    updPriceLevel.Symbol = existingPriceLevel.Symbol;

                    existingPriceLevel.Size = updPriceLevel.Size;
                    DoSend<DepthOfBook>(socket, updPriceLevel);
                }
                else
                {

                    DepthOfBook newPriceLevel = new DepthOfBook();
                    newPriceLevel.cAction = DepthOfBook._ACTION_INSERT;
                    newPriceLevel.cBidOrAsk = DepthOfBook._ASK_ENTRY;
                    newPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                    newPriceLevel.Index = 0;
                    newPriceLevel.Msg = "DepthOfBook";
                    newPriceLevel.Price = legOrdReq.Price.HasValue ? legOrdReq.Price.Value : 0;
                    newPriceLevel.Sender = existingPriceLevel.Sender;
                    newPriceLevel.Size = legOrdReq.Quantity;
                    newPriceLevel.Symbol = legOrdReq.InstrumentId;

                    List<DepthOfBook> tempList = DepthOfBooks.ToList<DepthOfBook>();
                    tempList.Add(newPriceLevel);
                    DepthOfBooks=tempList.ToArray();

                    DoSend<DepthOfBook>(socket, newPriceLevel);
                }

            }

        
        
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
                cStatus = LegacyOrderAck._STATUS_OPEN,
                Price = legOrdReq.Price,
                LeftQty = legOrdReq.Quantity,
                Timestamp = Convert.ToInt64(elaped.TotalMilliseconds),
            };

            DoSend<LegacyOrderAck>(socket, legOrdAck);

            EvalPriceLevels(socket, legOrdReq);

            EvalNewOrder(socket, legOrdReq);

          
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
            else if (subscrMsg.Service == "Oy")
            {
                ProcessMyOrders(socket, subscrMsg);
            }
            else if (subscrMsg.Service == "LT")
            {
                ProcessMyTrades(socket, subscrMsg);
            }

        }

        #endregion

        #region Protected Methods

       
        protected override void OnMessage(IWebSocketConnection socket, string m)
        {
            try
            {
                DoLog(string.Format("OnMessage from IP -> {0}", socket.ConnectionInfo.ClientIpAddress), MessageType.Information);

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
                else if (wsResp.Msg == "LegacyOrderCancelReq")
                {
                    //ProcessLegacyOrderCancelMock(socket, m);
                }
                else if (wsResp.Msg == "LegacyOrderMassCancelReq")
                {
                    //ProcessLegacyOrderMassCancelMock(socket, m);
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
