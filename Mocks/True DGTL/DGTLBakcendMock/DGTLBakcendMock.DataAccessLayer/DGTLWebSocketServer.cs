using DGTLBackendMock.BusinessEntities;
using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.OrderRouting.Blotters;
using DGTLBackendMock.Common.DTO.Platform;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Subscription;
using DGTLBackendMock.DataAccessLayer.Service;
using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using ToolsShared.Logging;

namespace DGTLBackendMock.DataAccessLayer
{
    
    public class DGTLWebSocketServer : DGTLWebSocketBaseServer
    {
        #region Protected Attributes

        protected List<LegacyOrderRecord> Orders { get; set; }

        protected LegacyTradeHistory[] Trades { get; set; }

        protected List<LastSale> LastSales { get; set; }

        protected List<Quote> Quotes { get; set; }

        protected List<DepthOfBook> DepthOfBooks { get; set; }

        protected SecurityMapping[] SecurityMappings { get; set; }

        protected Dictionary<string, Thread> ProcessLastSaleThreads { get; set; }

        protected Dictionary<string, Thread> ProcessLastQuoteThreads { get; set; }

        protected Dictionary<string, Thread> ProcessSecuritStatusThreads { get; set; }

        protected List<string> NotificationsSubscriptions { get; set; }

        protected List<string> OpenOrderCountSubscriptions { get; set; }

        protected bool Connected { get; set; }

        protected HttpSelfHostServer Server { get; set; }

        
        #endregion

        #region Constructors

        public DGTLWebSocketServer(string pURL, string pRESTAdddress)
        {
            URL = pURL;

            RESTURL = pRESTAdddress;

            HeartbeatSeqNum = 1;

            UserLogged = false;

            Connected = false;

            ConnectedClients = new List<int>();

            ProcessLastSaleThreads = new Dictionary<string, Thread>();

            ProcessLastQuoteThreads = new Dictionary<string, Thread>();

            ProcessDailySettlementThreads = new Dictionary<string, Thread>();

            ProcessSecuritStatusThreads = new Dictionary<string, Thread>();

            ProcessCreditLimitUpdatesThreads = new Dictionary<string, Thread>();

            ProcessDailyOfficialFixingPriceThreads = new Dictionary<string, Thread>();

            NotificationsSubscriptions = new List<string>();

            
            OpenOrderCountSubscriptions = new List<string>();

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

                LoadSecurityMappings();

                LoadDailySettlementPrices();

                LoadOfficialFixingPrices();

                LoadCreditRecordUpdates();

                LoadDepthOfBooks();

                LoadPlatformStatus();

                LoadHistoryService();

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

                ProcessSecuritStatusThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessSecuritStatusThreads.Clear();

                ProcessCreditLimitUpdatesThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessCreditLimitUpdatesThreads.Clear();


                NotificationsSubscriptions.Clear();
                OpenOrderCountSubscriptions.Clear();

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

        protected void LoadSecurityMappings()
        {
            string strSecurityMappings = File.ReadAllText(@".\input\SecurityMapping.json");

            //Aca le metemos que serialize el contenido
            SecurityMappings = JsonConvert.DeserializeObject<SecurityMapping[]>(strSecurityMappings);
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
            LastSales = JsonConvert.DeserializeObject<List<LastSale>>(strLastSales);
        }

        private void LoadOrders()
        {
            string strOrders = File.ReadAllText(@".\input\Orders.json");

            //Aca le metemos que serialize el contenido
            Orders = JsonConvert.DeserializeObject<List<LegacyOrderRecord>>(strOrders);
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
            Quotes = JsonConvert.DeserializeObject<List<Quote>>(strQuotes);
        }

        private void LoadDepthOfBooks()
        {
            string strDepthOfBooks = File.ReadAllText(@".\input\DepthOfBook.json");

            //Aca le metemos que serialize el contenido
            DepthOfBooks = JsonConvert.DeserializeObject<List<DepthOfBook>>(strDepthOfBooks);
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


                        //EmulatePriceChanges(i, lastSale, ref initialPrice);
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
                    Quote quote = null;
                    lock (Quotes)
                    {
                        quote = Quotes.Where(x => x.Symbol == subscrMsg.ServiceKey).FirstOrDefault();
                    }

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
                        ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID);
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


        private void LoadHistoryService()
        {
            string url = RESTURL;

            try
            {

                DoLog(string.Format("Creating history service for controller HistoryServiceController on URL {0}", url),
                      MessageType.Information);

                HttpSelfHostConfiguration config = new HttpSelfHostConfiguration(url);

                config.Routes.MapHttpRoute(name: "DefaultApi",
                                           routeTemplate: "{controller}/{id}",
                                           defaults: new { id = RouteParameter.Optional });

              
                historyController.OnLog += DoLog;
                historyController.OnGetAllTrades += GetAllTrades;
                historyController.OnGetAllOrders += GetAllOrders;

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
                      url, error),MessageType.Error);
            }

   
        }

        private void NewQuoteThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            string symbol = (string)paramArray[1];

            try
            {
                while (true)
                {
                    Quote quote = null;
                    lock (Quotes)
                    {
                        DoLog(string.Format("Searching quotes for symbol {0}. ", symbol), MessageType.Information);
                        quote = Quotes.Where(x => x.Symbol == symbol).FirstOrDefault();
                    }

                    if (quote != null)
                    {
                        DoLog(string.Format("Sending LQ for symbol {0}. Best bid={1} Best ask={2}", symbol, quote.Bid, quote.Ask), MessageType.Information);
                        DoSend<Quote>(socket, quote);
                        
                    }
                    else
                    {
                        DoLog(string.Format("quotes for symbol {0} not found ", symbol), MessageType.Information);

                        //DoLog(string.Format("Quotes not found for symbol {0}...", symbol), MessageType.Information);
                        //break;
                    }
                    Thread.Sleep(3000);//3 seconds
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing quote message for symbol {1}: {0}...", ex.Message, symbol), MessageType.Error);
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
            else
            {
                DoLog(string.Format("Double subscription for service LS for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.UUID,false,"Double subscription");

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
            else
            {
                DoLog(string.Format("Double subscription for service LQ for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID, false, "Double subscription");
            }
        }


        private void UpdateQuotes(IWebSocketConnection socket, string symbol)
        {

            DoLog(string.Format("Updating best bid and ask for symbol {0}", symbol), MessageType.Information);
            DepthOfBook bestBid = DepthOfBooks.Where(x => x.Symbol == symbol && x.cBidOrAsk == DepthOfBook._BID_ENTRY).OrderByDescending(x => x.Price).FirstOrDefault();
            DepthOfBook bestAsk = DepthOfBooks.Where(x => x.Symbol == symbol && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).OrderBy(x => x.Price).FirstOrDefault();

            
            lock (Quotes)
            {
                Quote quote = Quotes.Where(x => x.Symbol == symbol).FirstOrDefault();

                if (quote != null)
                {
                    if (bestBid != null)
                    {
                        quote.BidSize = bestBid.Size;
                        quote.Bid = bestBid.Price;
                    }
                    else
                    {
                        DoLog(string.Format("Erasing best bid for symbol {0}", symbol), MessageType.Information);
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
                        DoLog(string.Format("Erasing best ask for symbol {0}", symbol), MessageType.Information);
                        quote.AskSize = null;
                        quote.Ask = null;
                    }


                    quote.RefreshMidPoint(SecurityMasterRecords.Where(x => x.Symbol == symbol).FirstOrDefault().MinPriceIncrement);
                 
                }
                else
                {
                    Quote newQuote = new Quote()
                    {
                        Msg = "Quote",
                        Symbol=symbol
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

                    newQuote.RefreshMidPoint(SecurityMasterRecords.Where(x => x.Symbol == symbol).FirstOrDefault().MinPriceIncrement);

                    DoLog(string.Format("Inserting quotes for symbol {0}",symbol),MessageType.Information);
                    Quotes.Add(newQuote);
                    DoLog(string.Format("Quotes for symbol {0} inserted",symbol),MessageType.Information);

                    Thread ProcessQuoteThread = new Thread(NewQuoteThread);
                    ProcessQuoteThread.Start(new object[] { socket, symbol });

                    if (!ProcessLastQuoteThreads.ContainsKey(symbol))
                        ProcessLastQuoteThreads.Add(symbol, ProcessQuoteThread);
                    else
                        ProcessLastQuoteThreads[symbol] = ProcessQuoteThread;
                    
                }
            }

        }

        private void ProcessOrderBookDepth(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            List<DepthOfBook> depthOfBooks = DepthOfBooks.Where(x => x.Symbol == subscrMsg.ServiceKey).ToList();
            if (depthOfBooks != null)
            {
                depthOfBooks.ForEach(x => DoSend<DepthOfBook>(socket, x));
                //Now we have to launch something to create deltas (insert, change, remove)
                Thread.Sleep(1000);
                UpdateQuotes(socket, subscrMsg.ServiceKey);
            }
            ProcessSubscriptionResponse(socket, "LD", subscrMsg.ServiceKey, subscrMsg.UUID);
        }

        protected void ProcessFillOffers(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                string[] fields = subscrMsg.ServiceKey.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);


                if (fields.Length != 5)
                    throw new Exception(string.Format("Invalid fields length: {0} for ServiceKey {1}", fields.Length, subscrMsg.ServiceKey));


                int qty = Convert.ToInt32(fields[0]);
                char side = Convert.ToChar(fields[1]);
                string symbol = fields[2];
                decimal initialPrice = Convert.ToDecimal(fields[3]);
                decimal step = Convert.ToDecimal(fields[4]);

                DoLog(string.Format("Processing fill offers for Qty {0} Side {1} Initial Price {2} and Step {3} for Symbol {4}",
                                     qty, side, initialPrice.ToString("0.##"), step, symbol), MessageType.Information);


                for (int i = 0; i < qty; i++)
                {

                    LegacyOrderReq req = new LegacyOrderReq()
                    {
                        AccountId = "",
                        ClOrderId = Guid.NewGuid().ToString(),
                        cOrderType = LegacyOrderReq._ORD_TYPE_LIMIT,
                        cSide = side,
                        cTimeInForce = LegacyOrderReq._TIF_DAY,
                        InstrumentId = symbol,
                        Msg = "LegacyOrderReq",
                        Quantity = 1,
                        Price = initialPrice,
                        UserId = subscrMsg.UserId
                    };

                    if (side == LegacyOrderReq._SIDE_BUY)
                        initialPrice -= step;
                    else if (side == LegacyOrderReq._SIDE_SELL)
                        initialPrice += step;
                    else
                        throw new Exception(string.Format("Invalid Side for FO Request: {0}", side));

                    string msg = JsonConvert.SerializeObject(req);

                    DoLog(string.Format("Sending specific offer for Price {0} Qty {1}", req.Price, req.Quantity), MessageType.Information);
                    ProcessLegacyOrderReqMock(socket, msg);
                }


                ProcessSubscriptionResponse(socket, "FO", subscrMsg.ServiceKey, subscrMsg.UUID);
            }
            catch (Exception ex)
            {
                ProcessSubscriptionResponse(socket, "FO", subscrMsg.ServiceKey, subscrMsg.UUID,false,ex.Message);
            }
        }


        private LegacyOrderBlotter[] GetLegacyOrderBlotters(List<LegacyOrderRecord> orders)
        {

            List<LegacyOrderBlotter> ordersBlotters = new List<LegacyOrderBlotter>();

            foreach (LegacyOrderRecord order in orders)
            {
                LegacyOrderBlotter orderBlotter = new LegacyOrderBlotter();
                orderBlotter.Account = "TEST ACCOUNT";
                orderBlotter.AgentId = "TEST AGENT ID";
                orderBlotter.AvgPrice = order.FillQty > 0 ? Convert.ToDecimal(order.Price.Value) : 0;
                orderBlotter.ClOrderId = order.ClientOrderId;
                orderBlotter.cSide = order.cSide;
                orderBlotter.cStatus = order.cStatus;
                orderBlotter.EndTime = order.UpdateTime;
                orderBlotter.ExecNotional = 0;
                orderBlotter.Fees = 0;
                orderBlotter.FillQty = order.FillQty;
                orderBlotter.LeavesQty = order.LvsQty;
                orderBlotter.LimitPrice = order.Price;
                orderBlotter.Msg = "LegacyOrderBlotter";
                orderBlotter.OrderId = order.OrderId;
                orderBlotter.OrderQty = order.OrdQty;
                orderBlotter.OrderType = LegacyOrderRecord._ORDER_TYPE_LIMIT;
                orderBlotter.RejectReason = order.cStatus == LegacyOrderRecord._STATUS_REJECTED ? "TEST Rej Msg" : null;
                orderBlotter.Sender = 0;
                orderBlotter.StartTime = order.UpdateTime;
                orderBlotter.Symbol = order.InstrumentId;
                orderBlotter.Time = 0;
                ordersBlotters.Add(orderBlotter);
            }

            return ordersBlotters.ToArray();
        }

        private LegacyTradeBlotter[] GetLegacyTradeBlotters(LegacyTradeHistory[] trades)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);


            List<LegacyTradeBlotter> tradesBlotters = new List<LegacyTradeBlotter>();

            foreach (LegacyTradeHistory trade in trades)
            {
                LegacyTradeBlotter tradeBlotter = new LegacyTradeBlotter();
                tradeBlotter.Account = "TEST ACCOUNT";
                tradeBlotter.AgentId = "TEST AGENT ID";
                tradeBlotter.Symbol = trade.Symbol;
                tradeBlotter.cSide = trade.cMySide;
                tradeBlotter.ExecPrice = trade.TradePrice;
                tradeBlotter.ExecQty = trade.TradeQuantity;
                tradeBlotter.ExecutionTime = trade.TradeTimeStamp;
                tradeBlotter.Msg = "LegacyTradeBlotter";
                tradeBlotter.Notional = trade.TradePrice * trade.TradeQuantity;
                tradeBlotter.OrderId = "TEST ORDERID";
                tradeBlotter.Sender = 0;
                tradeBlotter.cStatus = LegacyOrderRecord._STATUS_FILLED;
                //tradeBlotter.Time = Convert.ToInt64(elapsed.TotalMilliseconds);
                tradeBlotter.TradeId = trade.TradeId;

                tradesBlotters.Add(tradeBlotter);
            }

            return tradesBlotters.ToArray();
        }

        private GetOrdersBlotterFulFilled GetAllOrders()
        {
            GetOrdersBlotterFulFilled ordersFullFilled = new GetOrdersBlotterFulFilled() 
                                                                                    {
                                                                                        Msg = "GetOrdersBlotterFulFilled", 
                                                                                        data = GetLegacyOrderBlotters(Orders) 
                                                                                    };
            return ordersFullFilled;
        }

        private GetExecutionsBlotterFulFilled GetAllTrades()
        {

            GetExecutionsBlotterFulFilled executionsFullFilled = new GetExecutionsBlotterFulFilled()
            {
                Msg = "GetExecutionsBlotterFulFilled",
                data = GetLegacyTradeBlotters(Trades)
            };
            return executionsFullFilled;
        }

        private void ProcessMyOrders(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            string symbol = "";
            string[] symbolFields = subscrMsg.ServiceKey.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);

            if (symbolFields.Length >= 2)
                symbol = symbolFields[1];
            else
                throw new Exception(string.Format("Invalid format from ServiceKey. Valid format:UserID@Symbol@[OrderID,*]. Received: {1}", subscrMsg.ServiceKey));

            List<LegacyOrderRecord> orders = null;
            if (symbol != "*")
                orders = Orders.Where(x => x.InstrumentId == symbol).ToList();
            else
                orders = Orders.ToList();

            DoLog(string.Format("Sending all orders for {0} subscription. Count={1}", subscrMsg.ServiceKey, orders.Count), MessageType.Information);

            orders.ForEach(x => DoSend<LegacyOrderRecord>(socket, x));
            //Now we have to launch something to create deltas (insert, change, remove)
            RefreshOpenOrders(socket, subscrMsg.ServiceKey, subscrMsg.UserId);
            ProcessSubscriptionResponse(socket, "Oy", subscrMsg.ServiceKey, subscrMsg.UUID);

            //if (symbol == "*")
            //{
            //    Thread.Sleep(4000);
            //    GetOrdersBlotterFulFilled ordersFullFilled = new GetOrdersBlotterFulFilled() { Msg = "GetOrdersBlotterFulFilled", data = GetLegacyOrderBlotters(orders) };
            //    DoSend<GetOrdersBlotterFulFilled>(socket, ordersFullFilled);

            //    GetExecutionsBlotterFulFilled executionsFullFilled = new GetExecutionsBlotterFulFilled() { Msg = "GetExecutionsBlotterFulFilled" };
            //    DoSend<GetExecutionsBlotterFulFilled>(socket, executionsFullFilled);

            //}
        }


        private void ProcessBlotterTrades(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {

            if (!subscrMsg.ServiceKey.Contains("*"))
            {
                ProcessSubscriptionResponse(socket, "rt", subscrMsg.ServiceKey, subscrMsg.UUID,success : false,msg:  "You can only subscribe to Symbol=* for rt service");
            }
            else
            {
                
                ProcessSubscriptionResponse(socket, "LT", subscrMsg.ServiceKey, subscrMsg.UUID);
            }
        }

        private void ProcessMyTrades(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            List<LegacyTradeHistory> trades = null;

            if(subscrMsg.ServiceKey!="*")
                trades = Trades.Where(x => x.Symbol == subscrMsg.ServiceKey).ToList();
            else
                trades = Trades.ToList();

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

        private void RefreshOpenOrders(IWebSocketConnection socket, string symbol,string userId)
        {
            TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

            try
            {
                if (OpenOrderCountSubscriptions.Contains("*"))
                {

                    int openOrdersCount = Orders.Where(x => x.cStatus == LegacyOrderRecord._STATUS_OPEN /*|| x.cStatus == LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/).ToList().Count;
                    
                    DoLog(string.Format("Sending open order count for all symbols : {0}", openOrdersCount), MessageType.Information);

                    OpenOrdersCount openOrders = new OpenOrdersCount()
                    {
                        Msg = "OpenOrdersCount",
                        Sender = 0,
                        Symbol = "*",
                        Time = Convert.ToInt64(elaped.TotalMilliseconds),
                        UserId = userId,
                        Count = openOrdersCount
                    };

                    DoSend<OpenOrdersCount>(socket, openOrders);
                
                }
                else if (symbol.Contains("@"))
                {
                    DoLog(string.Format("Symbol has special format that hast to be cleaned : {0}", symbol), MessageType.Information);

                    string[] fields = symbol.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);

                    if (fields.Length >= 2)
                        symbol = fields[1];


                    DoLog(string.Format("Symbol cleaned : {0}", symbol), MessageType.Information);


                    if (OpenOrderCountSubscriptions.Contains(symbol))
                    {
                        int openOrdersCount = Orders.Where(x => x.InstrumentId == symbol && (x.cStatus == LegacyOrderRecord._STATUS_OPEN /* || x.cStatus == LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/)).ToList().Count;

                        DoLog(string.Format("Sending open order count for symbol {0} : {1}", symbol, openOrdersCount), MessageType.Information);


                        OpenOrdersCount openOrders = new OpenOrdersCount()
                        {
                            Msg = "OpenOrdersCount",
                            Sender = 0,
                            Symbol = symbol,
                            Time = Convert.ToInt64(elaped.TotalMilliseconds),
                            UserId = userId,
                            Count = openOrdersCount
                        };

                        DoSend<OpenOrdersCount>(socket, openOrders);

                    }
                    else
                    {
                        DoLog(string.Format("Not Sending open order count for symbol {0} because it was not subscribed", symbol), MessageType.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception on RefreshOpenOrders for symbol {0}: {1}", symbol, ex.Message), MessageType.Error);
            
            }
        }

        private void EvalNewOrder(IWebSocketConnection socket, LegacyOrderReq legOrdReq,
                                 char cStatus,double fillQty)
        {
            lock (Orders)
            {
                TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

                LegacyOrderRecord OyMsg = new LegacyOrderRecord();

                OyMsg.ClientOrderId = legOrdReq.ClOrderId;
                OyMsg.cSide = legOrdReq.cSide;
                OyMsg.cStatus = cStatus;
                OyMsg.cTimeInForce = LegacyOrderRecord._TIMEINFORCE_DAY;
                OyMsg.FillQty = fillQty;
                OyMsg.InstrumentId = legOrdReq.InstrumentId;
                OyMsg.LvsQty = Convert.ToDouble(legOrdReq.Quantity) - fillQty;
                OyMsg.Msg = "LegacyOrderRecord";
                //OyMsg.OrderId = Guid.NewGuid().ToString();
                OyMsg.OrderId = legOrdReq.ClOrderId;
                OyMsg.OrdQty = Convert.ToDouble(legOrdReq.Quantity);
                OyMsg.Price = (double?)legOrdReq.Price;
                OyMsg.Sender = 0;
                OyMsg.UpdateTime = Convert.ToInt64(elaped.TotalMilliseconds);
                OyMsg.UserId = legOrdReq.UserId;

                DoSend<LegacyOrderRecord>(socket, OyMsg);

                DoLog(string.Format("Creating new order in Orders collection for ClOrderId = {0}", OyMsg.ClientOrderId), MessageType.Information);
                Orders.Add(OyMsg);

                RefreshOpenOrders(socket, OyMsg.InstrumentId,OyMsg.UserId);

            }
        }

        private void EvalPriceLevels(IWebSocketConnection socket, LegacyOrderRecord order)
        {

            if (!order.Price.HasValue)
                return;

            TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

            if (order.cSide == LegacyOrderReq._SIDE_BUY)
            {
                DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._BID_ENTRY
                                                                       && x.Symbol==order.InstrumentId
                                                                       && x.Price == Convert.ToDecimal(order.Price.Value)).FirstOrDefault();

                if (existingPriceLevel != null)
                {
                    decimal newSize= existingPriceLevel.Size - Convert.ToDecimal( order.LvsQty);

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
                    DoSend<DepthOfBook>(socket, updPriceLevel);
                }
                else
                {

                    throw new Exception(string.Format("Critical Error: Cancelling an order for an unexisting bid price level: {0}", order.Price.Value));
                }

            }
            else if (order.cSide == LegacyOrderReq._SIDE_SELL)
            {
                DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._ASK_ENTRY
                                                                       && x.Symbol == order.InstrumentId
                                                                       && x.Price == Convert.ToDecimal(order.Price.Value)).FirstOrDefault();

                if (existingPriceLevel != null)
                {
                    decimal newSize = existingPriceLevel.Size - Convert.ToDecimal(order.LvsQty);

                    DepthOfBook updPriceLevel = new DepthOfBook();
                    updPriceLevel.cAction = newSize > 0 ? DepthOfBook._ACTION_CHANGE : DepthOfBook._ACTION_REMOVE;
                    updPriceLevel.cBidOrAsk = DepthOfBook._ASK_ENTRY;
                    updPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                    updPriceLevel.Index = existingPriceLevel.Index;
                    updPriceLevel.Msg = existingPriceLevel.Msg;
                    updPriceLevel.Price = existingPriceLevel.Price;
                    updPriceLevel.Sender = existingPriceLevel.Sender;
                    updPriceLevel.Size = existingPriceLevel.Size - Convert.ToDecimal(order.LvsQty);
                    updPriceLevel.Symbol = existingPriceLevel.Symbol;

                    existingPriceLevel.Size = updPriceLevel.Size;

                    if (updPriceLevel.cAction == DepthOfBook._ACTION_REMOVE)
                        DepthOfBooks.Remove(existingPriceLevel);

                    DoLog(string.Format("Sending upd ask entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
                    DoSend<DepthOfBook>(socket, updPriceLevel);
                }
                else
                {
                    throw new Exception(string.Format("Critical Error: Cancelling an order for an unexisting ask price level: {0}", order.Price.Value));
                }
            }
        }

        private void EvalPriceLevelsIfNotTrades(IWebSocketConnection socket, LegacyOrderReq legOrdReq)
        {
            lock (Orders)
            {
                TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

                if (legOrdReq.cSide == LegacyOrderReq._SIDE_BUY)
                {
                    DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._BID_ENTRY
                                                                           && x.Symbol == legOrdReq.InstrumentId
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
                        DoLog(string.Format("Sending upd bid entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
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

                        DepthOfBooks.Add(newPriceLevel);
                        DoLog(string.Format("Sending new bid entry: Price={0} Size={1} ...", newPriceLevel.Price, newPriceLevel.Size), MessageType.Information);
                        DoSend<DepthOfBook>(socket, newPriceLevel);
                    }

                }
                else if (legOrdReq.cSide == LegacyOrderReq._SIDE_SELL)
                {
                    DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._ASK_ENTRY
                                                                           && x.Symbol == legOrdReq.InstrumentId
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
                        DoLog(string.Format("Sending upd ask entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
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
                        newPriceLevel.Sender = 0;
                        newPriceLevel.Size = legOrdReq.Quantity;
                        newPriceLevel.Symbol = legOrdReq.InstrumentId;

                        DepthOfBooks.Add(newPriceLevel);
                        DoLog(string.Format("Sending new ask entry: Price={0} Size={1} ...", newPriceLevel.Price, newPriceLevel.Size), MessageType.Information);
                        DoSend<DepthOfBook>(socket, newPriceLevel);
                    }

                }
            }
        }

        protected void ProcessChangePlatformStatusRequest(IWebSocketConnection socket, string m)
        {
            ChangePlatformStatusRequest platStatusChangeReq = JsonConvert.DeserializeObject<ChangePlatformStatusRequest>(m);

            PlatformStatus.cState = Convert.ToChar(platStatusChangeReq.Status);

            DoSend<PlatformStatus>(socket, PlatformStatus);
        }

        protected void ProcessLegacyOrderMassCancelMock(IWebSocketConnection socket, string m)
        {
            DoLog(string.Format("Processing ProcessLegacyOrderMassCancelMock"), MessageType.Information);
            LegacyOrderMassCancelReq legOrdMassCxlReq = JsonConvert.DeserializeObject<LegacyOrderMassCancelReq>(m);
            try
            {
                lock (Orders)
                {
                    TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

                    foreach (LegacyOrderRecord order in Orders.Where(x => x.cStatus == LegacyOrderRecord._STATUS_OPEN /*|| x.cStatus == LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/).ToList())
                    {
                        //1-Manamos el CancelAck
                        LegacyOrderCancelAck ack = new LegacyOrderCancelAck();
                        ack.OrigClOrderId = order.ClientOrderId;
                        ack.CancelReason = "Massive cancel cancelled @ mock";
                        ack.Msg = "LegacyOrderCancelAck";
                        ack.OrderId = order.OrderId;
                        ack.UserId = legOrdMassCxlReq.UserId;
                        ack.ClOrderId = order.OrderId;
                        ack.cSide = order.cSide;
                        ack.InstrumentId = order.InstrumentId;
                        ack.cStatus = LegacyOrderRecord._STATUS_CANCELED;
                        ack.Price = order.Price.HasValue ? (decimal?)Convert.ToDecimal(order.Price) : null;
                        ack.LeftQty = 0;
                        ack.UUID = "";
                        ack.Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds);
                        DoLog(string.Format("Sending cancellation ack for ClOrdId: {0}", order.ClientOrderId), MessageType.Information);
                        DoSend<LegacyOrderCancelAck>(socket, ack);


                        //2-Actualizamos el PL
                        DoLog(string.Format("Evaluating price levels for ClOrdId: {0}", order.ClientOrderId), MessageType.Information);
                        EvalPriceLevels(socket, order);

                        //3-Upd Quotes
                        UpdateQuotes(socket, order.InstrumentId);

                    }

                    DoLog(string.Format("Updating orders in mem on massive cancellation"), MessageType.Information);
                    Orders.Clear();

                    RefreshOpenOrders(socket, "*", legOrdMassCxlReq.UserId);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing LegacyOrderMassCancelReq: {0}", ex.Message), MessageType.Error);
            }

        }

        private void ProcessFakeCancellationRejection(IWebSocketConnection socket, LegacyOrderCancelReq legOrdCxlReq)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            DoLog(string.Format("Processing Fake cancellation Rejection for security {0}",legOrdCxlReq.InstrumentId), MessageType.Information);

            LegacyOrderCancelRejAck legOrdCancelRejAck = new LegacyOrderCancelRejAck();
            legOrdCancelRejAck.ClOrderId = legOrdCxlReq.ClOrderId;
            legOrdCancelRejAck.cSide = legOrdCxlReq.cSide;
            legOrdCancelRejAck.cStatus = LegacyOrderCancelRejAck._STATUS_REJECTED;
            legOrdCancelRejAck.InstrumentId = legOrdCxlReq.InstrumentId;
            legOrdCancelRejAck.LeftQty = 0;
            legOrdCancelRejAck.Msg = "LegacyOrderCancelRejAck";
            legOrdCancelRejAck.OrderId = "";
            legOrdCancelRejAck.OrderRejectReason = "Testing rejection...";
            legOrdCancelRejAck.OrigClOrderId = legOrdCxlReq.OrigClOrderId;
            legOrdCancelRejAck.Price = null;
            legOrdCancelRejAck.Sender = 0;
            legOrdCancelRejAck.Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds);
            legOrdCancelRejAck.UserId = legOrdCxlReq.UserId;
            DoSend<LegacyOrderCancelRejAck>(socket, legOrdCancelRejAck);
        }

        protected void ProcessLegacyOrderCancelMock(IWebSocketConnection socket, string m)
        {

            DoLog(string.Format("Processing ProcessLegacyOrderCancelMock"), MessageType.Information);
            LegacyOrderCancelReq legOrdCxlReq = JsonConvert.DeserializeObject<LegacyOrderCancelReq>(m);

            if (legOrdCxlReq.InstrumentId == "SWP-XBT-USD-Z19")
            {
                ProcessFakeCancellationRejection(socket, legOrdCxlReq);
                return;
            }
            
            try
            {
                lock (Orders)
                {
                    TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

                    DoLog(string.Format("Searching order by OrigClientOrderId={0} (ClOrderId={1})", legOrdCxlReq.OrigClOrderId, legOrdCxlReq.ClOrderId), MessageType.Information); 
                    LegacyOrderRecord order = Orders.Where(x => x.ClientOrderId == legOrdCxlReq.OrigClOrderId).FirstOrDefault();

                    if (order != null)
                    {
                        //1-Manamos el CancelAck
                        LegacyOrderCancelAck ack = new LegacyOrderCancelAck();
                        ack.OrigClOrderId = legOrdCxlReq.OrigClOrderId;
                        ack.CancelReason = "Just cancelled @ mock";
                        ack.Msg = "LegacyOrderCancelAck";
                        ack.OrderId = order.OrderId;
                        ack.UserId = order.UserId;
                        ack.ClOrderId = order.OrderId;
                        ack.cSide = order.cSide;
                        ack.InstrumentId = order.InstrumentId;
                        ack.cStatus = LegacyOrderRecord._STATUS_CANCELED;
                        ack.Price = order.Price.HasValue ? (decimal?)Convert.ToDecimal(order.Price) : null;
                        ack.LeftQty = 0;
                        ack.UUID = "";
                        ack.Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds) ;
                        DoLog(string.Format("Sending cancellation ack for ClOrdId: {0}", legOrdCxlReq.OrigClOrderId), MessageType.Information);
                        DoSend<LegacyOrderCancelAck>(socket, ack);


                        //2-Actualizamos el PL
                        DoLog(string.Format("Evaluating price levels for ClOrdId: {0}", legOrdCxlReq.OrigClOrderId), MessageType.Information);
                        EvalPriceLevels(socket, order);

                        //3-Upd orders in mem
                        DoLog(string.Format("Updating orders in mem"), MessageType.Information);
                        Orders.Remove(order);

                        //4- Update Quotes
                        DoLog(string.Format("Updating quotes on order cancelation"), MessageType.Information);
                        UpdateQuotes(socket, order.InstrumentId);

                        //5-
                        RefreshOpenOrders(socket, legOrdCxlReq.InstrumentId,legOrdCxlReq.UserId);

                    }
                    else
                    { 
                        //3-Mandamos el cancelRej
                        LegacyOrderCancelRejAck legOrdCancelRejAck = new LegacyOrderCancelRejAck()
                        {
                            Msg = "LegacyOrderCancelRejAck",
                            OrigClOrderId = legOrdCxlReq.OrigClOrderId,//we want the initial id
                            ClOrderId = legOrdCxlReq.ClOrderId,
                            UserId = legOrdCxlReq.UserId,
                            InstrumentId = legOrdCxlReq.InstrumentId,
                            OrderId = null,
                            Price = null,
                            cStatus = LegacyOrderCancelRejAck._STATUS_REJECTED,
                            cSide = legOrdCxlReq.cSide,
                            OrderRejectReason = string.Format("Order {0} not found!", legOrdCxlReq.OrigClOrderId)
                        };
                        DoLog(string.Format("Updating orders in mem"), MessageType.Information);
                        DoSend<LegacyOrderCancelRejAck>(socket, legOrdCancelRejAck);

                        RefreshOpenOrders(socket, legOrdCxlReq.InstrumentId,legOrdCxlReq.UserId);
                    }
                }


            }
            catch (Exception ex)
            {
                DoLog(string.Format("Order rejected because it was not found"), MessageType.Information);
                //4-Mandamos el cancelRej
                LegacyOrderCancelRejAck legOrdCancelRejAck = new LegacyOrderCancelRejAck()
                {
                    Msg = "LegacyOrderCancelRejAck",
                    OrigClOrderId = legOrdCxlReq.OrigClOrderId,//we want the initial id
                    ClOrderId = legOrdCxlReq.ClOrderId,
                    UserId = legOrdCxlReq.UserId,
                    InstrumentId = legOrdCxlReq.InstrumentId,
                    OrderId = null,
                    Price = null,
                    cStatus = LegacyOrderCancelRejAck._STATUS_REJECTED,
                    cSide = legOrdCxlReq.cSide,
                    OrderRejectReason = ex.Message
                };
                DoSend<LegacyOrderCancelRejAck>(socket, legOrdCancelRejAck);
                DoLog(string.Format("Exception processing LegacyOrderCancelReq: {0}", ex.Message), MessageType.Error);
            }
        
        }

        private bool ProcessRejectionsForNewOrders(LegacyOrderReq legOrdReq, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            if (legOrdReq.cSide == LegacyOrderReq._SIDE_BUY && legOrdReq.InstrumentId=="ETH-USD" && legOrdReq.Price.Value < 6000)
            {
                //We reject the messageas a convention, we cannot send messages lower than 6000 USD
                LegacyOrderRejAck reject = new LegacyOrderRejAck()
                {
                    Msg = "LegacyOrderRejAck",
                    OrderId = legOrdReq.OrderId,
                    UserId = legOrdReq.UserId,
                    ClOrderId = legOrdReq.ClOrderId,
                    InstrumentId = legOrdReq.InstrumentId,
                    cStatus = LegacyOrderAck._STATUS_REJECTED,
                    Side = legOrdReq.Side,
                    Price = legOrdReq.Price,
                    LeftQty = 0,
                    //Quantity = legOrdReq.Quantity,
                    //AccountId = legOrdReq.AccountId,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    OrderRejectReason = "You cannot send orders lower than 6000 USD for security ETH-USD"
                };

                DoSend<LegacyOrderRejAck>(socket, reject);

                return true;

            }

            return false;
        
        }

        //1.1-We eliminate the price level (@DepthOfBook and @Orders and we send the message)
        private void RemovePriceLevel(DepthOfBook bestOffer, char bidOrAsk, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            DoLog(string.Format("We eliminate the {2} price level {0} for symbol{1} (@DepthOfBook and @Orders and we send the message)",
                bestOffer.Price, bestOffer.Symbol, bidOrAsk == DepthOfBook._BID_ENTRY ? "bid" : "ask"), MessageType.Information);


            List<LegacyOrderRecord> updOrders = Orders.Where(x => x.InstrumentId==bestOffer.Symbol
                                                                  && x.Price.Value == Convert.ToDouble(bestOffer.Price)
                                                                  && x.cSide == (bidOrAsk == DepthOfBook._BID_ENTRY ? LegacyOrderRecord._SIDE_BUY : LegacyOrderRecord._SIDE_SELL)).ToList();

            foreach(LegacyOrderRecord order in updOrders)
            {
                order.SetFilledStatus();
                DoSend<LegacyOrderRecord>(socket, order);

                if (order.Price.HasValue)
                {
                    DoLog(string.Format("We send the Trade for my other affected filled order for symbol {0} Side={1} Price={2} ",order.InstrumentId, order.cSide == LegacyOrderRecord._SIDE_BUY ? "buy" : "sell", order.Price.Value), MessageType.Information);
                    SendNewTrade(new LegacyOrderReq() { cSide = order.cSide, InstrumentId = order.InstrumentId }, Convert.ToDecimal(order.Price.Value), Convert.ToDecimal(order.OrdQty), socket);
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
            DoSend<DepthOfBook>(socket, remPriceLevel);
        }

        private void CreatePriceLevel(char side,decimal price,decimal size,string symbol, IWebSocketConnection socket) 
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
            newPriceLevel.Symbol = symbol;


            DepthOfBooks.Add(newPriceLevel);

            DoSend<DepthOfBook>(socket, newPriceLevel);
        }

        //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
        private void UpdatePriceLevel(ref DepthOfBook bestOffer, char bidOrAsk,double tradeSize, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            string symbol = bestOffer.Symbol;
            double bestOfferPrice = Convert.ToDouble(bestOffer.Price);

            DoLog(string.Format("We update the {2} price level {0} for symbol {1} (@DepthOfBook and @Orders and we send the message)",
                                bestOffer.Price, bestOffer.Symbol, bidOrAsk == DepthOfBook._BID_ENTRY ? "bid" : "ask"), MessageType.Information);

            bestOffer.Size -= Convert.ToDecimal(tradeSize);
            bestOffer.cAction = DepthOfBook._ACTION_CHANGE;

            DoLog(string.Format("Searching for affecter orders for symbol {0}",bestOffer.Symbol), MessageType.Information);

            List<LegacyOrderRecord> updOrders = Orders.Where(x =>   x.InstrumentId == symbol
                                                                 && x.Price.Value == bestOfferPrice
                                                                 && x.cSide == (bidOrAsk == DepthOfBook._BID_ENTRY ? LegacyOrderRecord._SIDE_BUY : LegacyOrderRecord._SIDE_SELL)).ToList();

            foreach (LegacyOrderRecord order in updOrders)
            {
                DoLog(string.Format("Updating order status for price level {0}", order.Price.Value), MessageType.Information);

                double prevFillQty = order.FillQty;
                order.SetPartiallyFilledStatus(ref tradeSize);
                DoSend<LegacyOrderRecord>(socket, order);

                if (order.Price.HasValue)
                {
                    DoLog(string.Format("We send the Trade for my other affected {3} order for symbol {0} Side={1} Price={2} Trade Size={4} ", order.InstrumentId, order.cSide == LegacyOrderRecord._SIDE_BUY ? "buy" : "sell", order.Price.Value, order.cStatus, Convert.ToDecimal(order.FillQty - prevFillQty)), MessageType.Information);
                    SendNewTrade(new LegacyOrderReq() { cSide = order.cSide, InstrumentId = order.InstrumentId }, Convert.ToDecimal(order.Price.Value), Convert.ToDecimal(order.FillQty-prevFillQty), socket);
                }
            }

            
            DoLog(string.Format("We send the DepthOfBook Message updating the ask price level {0} for symbol {1} (@DepthOfBook and @Orders and we send the message)", bestOffer.Price, bestOffer.Symbol), MessageType.Information);

            DoSend<DepthOfBook>(socket, bestOffer);
        }

        private void SendTradeNotification(LegacyTradeHistory trade,string userId, IWebSocketConnection socket)
        {
            if (NotificationsSubscriptions.Contains(trade.Symbol))
            {
                DoLog(string.Format("We send a Trade Notification for Size={0} and Price={1} for symbol {2}", trade.TradeQuantity, trade.TradePrice, trade.Symbol), MessageType.Information);
                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);


                TradeNotification notif = new TradeNotification()
                {
                    Msg = "TradeNotification",
                    Sender = 0,
                   
                    Price = trade.TradePrice,
                    Size = trade.TradeQuantity,
                    Symbol = trade.Symbol,
                    Time = Convert.ToInt64(elapsed.TotalMilliseconds),
                    cSide=trade.cMySide,
                    UserId = userId

                };

                DoSend<TradeNotification>(socket, notif);
            }
            else
                DoLog(string.Format("Not sending Trade Notification for Size={0} and Price={1} for symbol {2} because it was not subscribed", trade.TradeQuantity, trade.TradePrice, trade.Symbol), MessageType.Information);


        }

        //1.2- We send a Trade by <size>
        private void SendNewTrade(LegacyOrderReq legOrdReq,decimal price, decimal size, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            DoLog(string.Format("We send a Trade for Size={0} and Price={1} for symbol {2}", size, price, legOrdReq.InstrumentId), MessageType.Information);
            
            //If we got a new trade --> we are in the Trading TAB as the mock doesn't allow to have multiple users
            //we must be subscribed to service LT
            //LegacyTradeBlotter tradeBlotter= new LegacyTradeBlotter()
            //{
            //    Account="TEST ACCOUNT",
            //    AgentId="TEST AGENT ID",
            //    cSide= legOrdReq.cSide,
            //    cStatus='c',//TODO complete status
            //    ExecPrice= Convert.ToDouble(price),
            //    ExecQty=Convert.ToDouble(size),
            //    ExecutionTime=Convert.ToInt64(elapsed.TotalMilliseconds),
            //    Msg="LegacyTradeBlotter",
            //    Notional=Convert.ToDouble(price)*Convert.ToDouble(size)
            //    OrderId=Guid.NewGuid().ToString(),
            //    Sender=0,
            //    Symbol=legOrdReq.InstrumentId,
            //    TradeId=Guid.NewGuid().ToString(),
            //};
            //DoSend<LegacyTradeBlotter>(socket, tradeBlotter);
            
            LegacyTradeHistory newTrade = new LegacyTradeHistory()
            {
                cMySide = legOrdReq.cSide,
                Msg = "LegacyTradeHistory",
                MyTrade = true,
                Sender = 0,
                Symbol = legOrdReq.InstrumentId,
                TradeId = Guid.NewGuid().ToString(),
                TradePrice = Convert.ToDouble(price),
                TradeQuantity = Convert.ToDouble(size),
                TradeTimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds)

            };
            DoSend<LegacyTradeHistory>(socket, newTrade);

            //1.2.1-We update market data for a new trade
            EvalMarketData(socket,newTrade);

            //1.2.2-We send a trade notification for the new trade
            SendTradeNotification(newTrade, legOrdReq.UserId, socket);
           
        }

        //1.2.1-We update market data for a new trade
        private void EvalMarketData( IWebSocketConnection socket,LegacyTradeHistory newTrade)
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

            ls.LastPrice = Convert.ToDecimal(newTrade.TradePrice);
            ls.LastShares = Convert.ToDecimal(newTrade.TradeQuantity);
            ls.LastTime = newTrade.TradeTimeStamp;
            ls.Volume += Convert.ToDecimal(newTrade.TradeQuantity);
            ls.Change = ls.LastPrice - ls.FirstPrice;
            ls.DiffPrevDay = ((ls.LastPrice / ls.FirstPrice) - 1) * 100;

            if (!ls.High.HasValue || Convert.ToDecimal(newTrade.TradePrice) > ls.High)
                ls.High = Convert.ToDecimal(newTrade.TradePrice);

            if (! ls.Low.HasValue || Convert.ToDecimal(newTrade.TradePrice) < ls.Low)
                ls.Low = Convert.ToDecimal(newTrade.TradePrice);

            DoLog(string.Format("LastSale updated..."), MessageType.Information);


            DoLog(string.Format("We udate Quotes MD for Size={0} and Price={1} for symbol {2}", newTrade.TradeQuantity, newTrade.TradePrice, newTrade.Symbol), MessageType.Information);
            UpdateQuotes(socket,newTrade.Symbol);
            DoLog(string.Format("Quotes updated..."), MessageType.Information);
        }

 

        private bool EvalTrades(LegacyOrderReq legOrdReq, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            lock (Orders)
            {
                //1- Main algo for updating price levels and publish the trades
                if (legOrdReq.cSide == LegacyOrderReq._SIDE_BUY)
                {
                    DepthOfBook bestAsk = DepthOfBooks.Where(x =>x.Symbol==legOrdReq.InstrumentId &&
                                                                 x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();

                    if (bestAsk == null)
                        return false;

                    DoLog(string.Format("Best ask for buy order (Limit Price={1}) found at price {0}", bestAsk.Price, legOrdReq.Price.Value), MessageType.Information);
                    if (legOrdReq.Price.HasValue && legOrdReq.Price.Value >= bestAsk.Price)
                    {
                        //we had a trade
                        decimal leftQty = legOrdReq.Quantity;

                        bool fullFill = false;
                        while (leftQty > 0 && bestAsk != null && legOrdReq.Price.Value >= bestAsk.Price)
                        {
                            decimal prevLeftQty = leftQty;
                            leftQty -= bestAsk.Size;

                            if (leftQty >= 0)
                            {
                                //1.1-We eliminate the price level (@DepthOfBook and @Orders and we send the message)
                                RemovePriceLevel(bestAsk, DepthOfBook._ASK_ENTRY, socket);

                                //1.2- We send a Trade by bestAsk.Size
                                SendNewTrade(legOrdReq, bestAsk.Price, bestAsk.Size, socket);

                                //1.3-Calculamos el nuevo bestAsk
                                bestAsk = DepthOfBooks.Where(x =>   x.Symbol == legOrdReq.InstrumentId 
                                                                 && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();

                                fullFill = leftQty == 0;

                            }
                            else//order fully filled
                            {
                                //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
                                UpdatePriceLevel(ref bestAsk, DepthOfBook._ASK_ENTRY, Convert.ToDouble(prevLeftQty), socket);

                                //1.2- We send a trade by prevLeftQty
                                SendNewTrade(legOrdReq, bestAsk.Price, prevLeftQty, socket);

                                fullFill = true;
                            }

                            bestAsk = DepthOfBooks.Where(x => x.Symbol == legOrdReq.InstrumentId && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();
                        }

                        if (leftQty > 0)
                        {
                            DoLog(string.Format("We create the remaining buy price level {1}  for size {0}", leftQty, legOrdReq.Price.Value), MessageType.Information);

                            //2-We send the new price level for the remaining order
                            CreatePriceLevel(DepthOfBook._BID_ENTRY, legOrdReq.Price.Value, leftQty, legOrdReq.InstrumentId, socket);
                        }
                        else
                        {
                            DoLog(string.Format("Final leftQty=0 out of loop for buy order at price level {0}",legOrdReq.Price.Value), MessageType.Information);
                        }


                        //3-We send the full fill/partiall fill for the order --> Oy:LegacyOrderRecord
                        EvalNewOrder(socket, legOrdReq,
                                     fullFill ? LegacyOrderRecord._STATUS_FILLED : /*LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/LegacyOrderRecord._STATUS_OPEN,
                                     Convert.ToDouble(fullFill ? legOrdReq.Quantity : legOrdReq.Quantity - leftQty));

                        //4-We update the final quotes state
                        UpdateQuotes(socket, legOrdReq.InstrumentId);

                        return true;
                    }
                    else
                    {
                        DoLog(string.Format("Could not find matching sell price for symbol {0} and order price {1}", legOrdReq.InstrumentId, legOrdReq.Price), MessageType.Information);
                        return false;
                    }
                }
                else if (legOrdReq.cSide == LegacyOrderReq._SIDE_SELL)
                {
                    DepthOfBook bestBid = DepthOfBooks.Where(x =>    x.Symbol == legOrdReq.InstrumentId
                                                                  && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();

                    if (bestBid == null)
                        return false;

                    DoLog(string.Format("Best bid for sell order (Limit Price={1}) found at price {0}", bestBid.Price,legOrdReq.Price.Value), MessageType.Information);
                    if (legOrdReq.Price.HasValue && legOrdReq.Price.Value <= bestBid.Price)
                    {
                        //we had a trade
                        decimal leftQty = legOrdReq.Quantity;

                        bool fullFill = false;
                        while (leftQty > 0 && bestBid!=null && legOrdReq.Price.Value <= bestBid.Price)
                        {
                            decimal prevLeftQty = leftQty;
                            leftQty -= bestBid.Size;

                            if (leftQty >= 0)
                            {
                                //1.1-We eliminate the price level (@DepthOfBook and @Orders and we send the message)
                                RemovePriceLevel(bestBid, DepthOfBook._BID_ENTRY, socket);

                                //1.2- We send a Trade by bestBid.Size
                                SendNewTrade(legOrdReq, bestBid.Price, bestBid.Size, socket);

                                //1.3-Calculamos el nuevo bestBid
                                bestBid = DepthOfBooks.Where(x =>   x.Symbol == legOrdReq.InstrumentId 
                                                                 && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();

                                fullFill = leftQty == 0;

                            }
                            else//order fully filled
                            {
                                //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
                                UpdatePriceLevel(ref bestBid, DepthOfBook._BID_ENTRY, Convert.ToDouble(prevLeftQty), socket);

                                //1.2- We send a trade by prevLeftQty
                                SendNewTrade(legOrdReq, bestBid.Price, prevLeftQty, socket);

                                fullFill = true;
                            }

                            bestBid = DepthOfBooks.Where(x => x.Symbol == legOrdReq.InstrumentId  && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();
                        }

                        if (leftQty > 0)
                        {
                            DoLog(string.Format("We create the remaining buy price level {1}  for size {0}", leftQty, legOrdReq.Price.Value), MessageType.Information);

                            //2-We send the new price level for the remaining order
                            CreatePriceLevel(DepthOfBook._ASK_ENTRY, legOrdReq.Price.Value, leftQty, legOrdReq.InstrumentId, socket);
                        }
                        else
                        {
                            DoLog(string.Format("Final leftQty=0 out of loop for sell order at price level {0}", legOrdReq.Price.Value), MessageType.Information);
                        }

                        //3-We send the full fill/partiall fill for the order --> Oy:LegacyOrderRecord
                        EvalNewOrder(socket, legOrdReq,
                                     fullFill ? LegacyOrderRecord._STATUS_FILLED : /* LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/ LegacyOrderRecord._STATUS_OPEN,
                                     Convert.ToDouble(fullFill ? legOrdReq.Quantity : legOrdReq.Quantity - leftQty));

                        //4-We update the final quotes state
                        UpdateQuotes(socket, legOrdReq.InstrumentId);
                        return true;
                    }
                    else
                    {
                        DoLog(string.Format("Could not find matching buy price for symbol {0} and order price {1}", legOrdReq.InstrumentId, legOrdReq.Price), MessageType.Information);
                        return false;
                    }
                }
                else
                {
                    DoLog(string.Format("Could not process side {1} for symbol {0} ", legOrdReq.InstrumentId, legOrdReq.cSide), MessageType.Information);
                    return false;
                }
            }
        
        }

        //this will update the security status into halted every 10 seconds 
        protected void ProcessSecuritStatusThread(object param)
        {
            object[] paramArr = (object[])param;
            string symbol = (string)paramArr[0];
            IWebSocketConnection socket = (IWebSocketConnection)paramArr[1];
            char status;

            try
            {
                
                SecurityMapping secMapping = SecurityMappings.Where(x => x.IncomingSymbol == symbol).FirstOrDefault();
                OfficialFixingPrice officialFixingPrice = OfficialFixingPrices.Where(x => x.Symbol == symbol).FirstOrDefault();


                if (secMapping != null)
                {
                    DoLog(string.Format("Alternating security status for symbol {0}...", symbol), MessageType.Information);

                    decimal initialRefPrice = officialFixingPrice != null ? officialFixingPrice.Price.Value : 2000;

                    while (true)
                    {

                        int totalSeconds = DateTime.Now.Second;

                        if (totalSeconds < 10 || (20 < totalSeconds && totalSeconds < 30) || (40 < totalSeconds && totalSeconds < 50))
                            status = SecurityStatus._SEC_STATUS_TRADING;
                        else
                            status = SecurityStatus._SEC_STATUS_HALTING;

                        SecurityStatus secStatus = new SecurityStatus();
                        secStatus.Msg = "SecurityStatus";
                        secStatus.Symbol = symbol;
                        secStatus.cStatus = status;//: SecurityStatus._SEC_STATUS_HALTING;
                        secStatus.ReferencePrice = initialRefPrice;
                        secStatus.IsOrdersPostingEnabled = true;
                        DoSend<SecurityStatus>(socket, secStatus);

                        initialRefPrice += 0.01m;
                        Thread.Sleep(10000);
                    }
                }
                else
                {
                    DoLog(string.Format("Mapping not found for symbol {0}. We will not play with halting/not halting for that security ...",symbol), MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error looking mapping for symbol {0}:{1}", symbol,ex.Message), MessageType.Information);
            }
        }


        protected void ProcessSecurityStatus(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                if (subscrMsg.ServiceKey != "*")
                {


                    SecurityStatus secStatus = new SecurityStatus();
                    secStatus.Msg = "SecurityStatus";
                    secStatus.Symbol = subscrMsg.ServiceKey;
                    secStatus.cStatus = SecurityStatus._SEC_STATUS_TRADING;//: SecurityStatus._SEC_STATUS_HALTING;
                    secStatus.IsOrdersPostingEnabled = (subscrMsg.ServiceKey == "NDF-XBT-USD-N19") ? false : true;
                    DoSend<SecurityStatus>(socket, secStatus);
                    ProcessSubscriptionResponse(socket, "TI", subscrMsg.ServiceKey, subscrMsg.UUID, true);

                    Thread processSecuritStatusThread = new Thread(ProcessSecuritStatusThread);
                    processSecuritStatusThread.Start(new object[] { subscrMsg.ServiceKey, socket });
                    ProcessSecuritStatusThreads.Add(subscrMsg.ServiceKey, processSecuritStatusThread);

                }
                else
                    ProcessSubscriptionResponse(socket, "TI", subscrMsg.ServiceKey, subscrMsg.UUID, false, string.Format("Uknown service key {0}", subscrMsg.Service));
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error Subscribing to service TI for  symbol {0}:{1}", subscrMsg.ServiceKey, ex.Message), MessageType.Information);
                ProcessSubscriptionResponse(socket, "TI", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);

            
            }
        }

        protected void ProcessNotifications(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
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
                    ProcessSubscriptionResponse(socket, "TN", subscrMsg.ServiceKey, subscrMsg.UUID, true);

                }
                else
                {

                    DoLog(string.Format("Cannot Subscribe to service TN for generic symbol {0}", subscrMsg.ServiceKey), MessageType.Information);

                    ProcessSubscriptionResponse(socket, "TN", subscrMsg.ServiceKey, subscrMsg.UUID, false, string.Format("Uknown service key {0}", subscrMsg.Service));
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error Subscribing to service TN for  symbol {0}:{1}", subscrMsg.ServiceKey,ex.Message), MessageType.Information);
                ProcessSubscriptionResponse(socket, "TN", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
            
            }
        }

        protected void ProcessOpenOrderCount(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            try
            {
                if (!subscrMsg.ServiceKey.Contains("*"))
                {
                    if (subscrMsg.ServiceKey.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries).Length != 2)
                        throw new Exception(string.Format("Invalid service key {0}", subscrMsg.ServiceKey));

                    string symbol = subscrMsg.ServiceKey.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    DoLog(string.Format("Subscribe to service Ot for symbol {0}", symbol), MessageType.Information);
                    OpenOrderCountSubscriptions.Add(symbol);
                    ProcessSubscriptionResponse(socket, "Ot", subscrMsg.ServiceKey, subscrMsg.UUID, true);

                }
                else
                {
                    OpenOrderCountSubscriptions.Add("*");
                    DoLog(string.Format("Subscribed for all securities in service Qt"), MessageType.Information);
                    RefreshOpenOrders(socket, "*", subscrMsg.UserId);
                    ProcessSubscriptionResponse(socket, "Ot", subscrMsg.ServiceKey, subscrMsg.UUID, true);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error Subscribing to service Ot for  symbol {0}:{1}", subscrMsg.ServiceKey, ex.Message), MessageType.Information);
                ProcessSubscriptionResponse(socket, "Ot", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);

            }
        }

        protected void ProcessResetPasswordRequest(IWebSocketConnection socket, string m)
        {
            try
            {
                ResetPasswordRequest resetPwdReq = JsonConvert.DeserializeObject<ResetPasswordRequest>(m);

                ClientReject clientRej = new ClientReject();

                clientRej.Msg = "ClientReject";
                clientRej.RejectReason = "mfalcone test";
                clientRej.Sender = 0;
                clientRej.UserId = resetPwdReq.UserId;
                clientRej.UUID = clientRej.UUID;

                DoLog(string.Format("Sending ClientReject for incoming ResetPasswordReject: {0}", clientRej.RejectReason), MessageType.Error);

                DoSend<ClientReject>(socket, clientRej);


            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing ProcessResetPasswordRequest: {0}", ex.Message), MessageType.Error);
            }
        
        }

        protected void ProcessLegacyOrderReqMock(IWebSocketConnection socket, string m)
        {
            try
            {
                lock (Orders)
                {
                    TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                    LegacyOrderReq legOrdReq = JsonConvert.DeserializeObject<LegacyOrderReq>(m);

                    if (!ProcessRejectionsForNewOrders(legOrdReq,socket))
                    {
                        //We send the mock ack
                        LegacyOrderAck legOrdAck = new LegacyOrderAck()
                        {
                            Msg = "LegacyOrderAck",
                            //OrderId = Guid.NewGuid().ToString(),
                            OrderId = legOrdReq.ClOrderId,
                            UserId = legOrdReq.UserId,
                            ClOrderId = legOrdReq.ClOrderId,
                            InstrumentId = legOrdReq.InstrumentId,
                            cStatus = LegacyOrderAck._STATUS_OPEN,
                            cSide = legOrdReq.cSide,
                            Price = legOrdReq.Price,
                            Quantity = legOrdReq.Quantity,
                            AccountId = legOrdReq.AccountId,
                            LeftQty = legOrdReq.Quantity,
                            Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                        };

                        DoLog(string.Format("Sending LgacyOrderAck ..."), MessageType.Information);


                        DoSend<LegacyOrderAck>(socket, legOrdAck);

                        if (!EvalTrades(legOrdReq,socket))
                        {
                            DoLog(string.Format("Evaluating price levels ..."), MessageType.Information);
                            EvalPriceLevelsIfNotTrades(socket, legOrdReq);
                            DoLog(string.Format("Evaluating LegacyOrderRecord ..."), MessageType.Information);
                            EvalNewOrder(socket, legOrdReq, LegacyOrderRecord._STATUS_OPEN, 0);
                            DoLog(string.Format("Updating quotes ..."), MessageType.Information);
                            UpdateQuotes(socket,legOrdReq.InstrumentId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing LegactyOrderReq: {0}",ex.Message), MessageType.Error);
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
                else if (subscrMsg.Service == "FP")
                {
                    if (ProcessDailyOfficialFixingPriceThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessDailyOfficialFixingPriceThreads.Remove(subscrMsg.ServiceKey);
                    }
                }
                else if (subscrMsg.Service == "TI")
                {
                    if (ProcessSecuritStatusThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessSecuritStatusThreads[subscrMsg.ServiceKey].Abort();
                        ProcessSecuritStatusThreads.Remove(subscrMsg.ServiceKey);
                    }
                }
                else if (subscrMsg.Service == "Ot")
                {
                    if (OpenOrderCountSubscriptions.Contains(subscrMsg.ServiceKey))
                        OpenOrderCountSubscriptions.Add(subscrMsg.ServiceKey);
                }
                else if (subscrMsg.Service == "TN")
                {
                    if (NotificationsSubscriptions.Contains(subscrMsg.ServiceKey))
                        NotificationsSubscriptions.Add(subscrMsg.ServiceKey);
                }
                else if (subscrMsg.Service == "FD")
                {
                    if (ProcessDailySettlementThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessDailySettlementThreads[subscrMsg.ServiceKey].Abort();
                        ProcessDailySettlementThreads.Remove(subscrMsg.ServiceKey);
                    }
                }
                else if (subscrMsg.Service == "Cm")
                {
                    if (ProcessCreditLimitUpdatesThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessCreditLimitUpdatesThreads[subscrMsg.ServiceKey].Abort();
                        ProcessCreditLimitUpdatesThreads.Remove(subscrMsg.ServiceKey);
                    }
                }
                else if (subscrMsg.Service == "CU")
                {
                    //ProcessCreditRecordUpdates(socket, subscrMsg);
                }
            }

        }

        protected void ProcessSubscriptions(IWebSocketConnection socket,string m)
        {
            WebSocketSubscribeMessage subscrMsg = JsonConvert.DeserializeObject<WebSocketSubscribeMessage>(m);

            DoLog(string.Format("Incoming subscription for service {0} - ServiceKey:{1}", subscrMsg.Service,subscrMsg.ServiceKey), MessageType.Information);

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
                        ProcessQuote(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "FP")
                {
                    if (subscrMsg.ServiceKey != null)
                        ProcessOficialFixingPrice(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "TI")
                {
                    ProcessSecurityStatus(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "Ot")
                {
                    ProcessOpenOrderCount(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "TN")
                {
                    ProcessNotifications(socket, subscrMsg);
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
                else if (subscrMsg.Service == "Cm")
                {
                    ProcessCreditLimitUpdates(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LD")
                {
                    ProcessOrderBookDepth(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "Oy")
                {
                    ProcessMyOrders(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "FO")
                {
                    ProcessFillOffers(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "PS")
                {
                    ProcessPlatformStatus(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LT")
                {
                    ProcessMyTrades(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "rt")
                {
                    ProcessBlotterTrades(socket, subscrMsg);
                }
            }
            else if (subscrMsg.SubscriptionType == WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_UNSUBSCRIBE)
            {
                ProcessUnsubscriptions(subscrMsg);
            }
        }

        #endregion

        #region Protected Methods

        protected override void OnMessage(IWebSocketConnection socket, string m)
        {
            try
            {

                WebSocketMessage wsResp = JsonConvert.DeserializeObject<WebSocketMessage>(m);

                DoLog(string.Format("OnMessage {1} from IP -> {0}", socket.ConnectionInfo.ClientIpAddress, wsResp.Msg), MessageType.Information);

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
                else if (wsResp.Msg == "ResetPasswordRequest")
                {

                    ProcessResetPasswordRequest(socket, m);

                }
                else if (wsResp.Msg == "Subscribe")
                {

                    ProcessSubscriptions(socket, m);

                }
                else if (wsResp.Msg == "LegacyOrderCancelReq")
                {
                    ProcessLegacyOrderCancelMock(socket, m);
                }
                else if (wsResp.Msg == "LegacyOrderMassCancelReq")
                {
                    ProcessLegacyOrderMassCancelMock(socket, m);
                }
                else if (wsResp.Msg == "ChangePlatformStatusRequest")
                {
                    ProcessChangePlatformStatusRequest(socket, m);
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
                DoLog(string.Format("Exception processing onMessage:{0}",ex.Message), MessageType.Error);
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
