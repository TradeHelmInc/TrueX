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

        protected List<LegacyOrderRecord> Orders { get; set; }

        protected LegacyTradeHistory[] Trades { get; set; }

        protected List<LastSale> LastSales { get; set; }

        protected List<Quote> Quotes { get; set; }

        protected List<DepthOfBook> DepthOfBooks { get; set; }

        protected SecurityMapping[] SecurityMappings { get; set; }

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

                LoadSecurityMappings();

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

        private void UpdateQuotes(string symbol)
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
                        DoLog(string.Format("Could not find DepthOfBook bid for symbol {0}", symbol), MessageType.Information);

                    }

                    if (bestAsk != null)
                    {
                        quote.AskSize = bestAsk.Size;
                        quote.Ask = bestAsk.Price;
                    }
                    else
                    {
                        DoLog(string.Format("Could not find DepthOfBook ask for symbol {0}", symbol), MessageType.Information);
                    }

                    if (quote.Ask.HasValue && quote.Bid.HasValue)
                        quote.MidPoint = Math.Round((quote.Ask.Value + quote.Bid.Value) / 2, 2);
                    else
                        quote.MidPoint = null;
                }
                else
                {
                    Quote newQuote = new Quote()
                    {
                        Msg = "Quote"
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

                    if (newQuote.Ask.HasValue && newQuote.Bid.HasValue)
                        newQuote.MidPoint = Math.Round((newQuote.Ask.Value + newQuote.Bid.Value) / 2, 2);
                    else
                        newQuote.MidPoint = null;


                    Quotes.Add(newQuote);
                
                }
            }

        }

        private void ProcessOrderBookDepth(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            List<DepthOfBook> depthOfBooks = DepthOfBooks.Where(x => x.Symbol == subscrMsg.ServiceKey).ToList();
            depthOfBooks.ForEach(x => DoSend<DepthOfBook>(socket, x));
            //Now we have to launch something to create deltas (insert, change, remove)
            Thread.Sleep(1000);
            UpdateQuotes(subscrMsg.ServiceKey);
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

        private void ProcessLegacyOrderCancelMock(IWebSocketConnection socket, string m)
        {
            DoLog(string.Format("Processing ProcessLegacyOrderCancelMock"), MessageType.Information);
            LegacyOrderCancelReq legOrdCxlReq = JsonConvert.DeserializeObject<LegacyOrderCancelReq>(m);
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

                        DoLog(string.Format("Updating orders in mem"), MessageType.Information);
                        Orders.Remove(order);

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


            List<LegacyOrderRecord> updOrders = Orders.Where(x =>   x.InstrumentId == symbol
                                                                 && x.Price.Value == Convert.ToDouble(bestOfferPrice)
                                                                 && x.cSide == (bidOrAsk == DepthOfBook._BID_ENTRY ? LegacyOrderRecord._SIDE_BUY : LegacyOrderRecord._SIDE_SELL)).ToList();

            foreach (LegacyOrderRecord order in updOrders)
            {
                order.SetPartiallyFilledStatus(ref tradeSize);
                DoSend<LegacyOrderRecord>(socket, order);
            }

            
            DoLog(string.Format("We send the DepthOfBook Message updating the ask price level {0} for symbol {1} (@DepthOfBook and @Orders and we send the message)", bestOffer.Price, bestOffer.Symbol), MessageType.Information);

            DoSend<DepthOfBook>(socket, bestOffer);
        }

        //1.2- We send a Trade by <size>
        private void SendNewTrade(LegacyOrderReq legOrdReq,DepthOfBook bestBidAsk, decimal size, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            DoLog(string.Format("We send a Trade for Size={0} and Price={1} for symbol {2}", size,bestBidAsk.Price,legOrdReq.InstrumentId), MessageType.Information);
            LegacyTradeHistory newTrade = new LegacyTradeHistory()
            {
                cMySide = legOrdReq.cSide,
                Msg = "LegacyTradeHistory",
                MyTrade = true,
                Sender = 0,
                Symbol = legOrdReq.InstrumentId,
                TradeId = Guid.NewGuid().ToString(),
                TradePrice = Convert.ToDouble(bestBidAsk.Price),
                TradeQuantity = Convert.ToDouble(size),
                TradeTimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds)

            };
            DoSend<LegacyTradeHistory>(socket, newTrade);

            //1.2.1-We update market data for a new trade
            EvalMarketData(newTrade);
           
        }

        //1.2.1-We update market data for a new trade
        private void EvalMarketData(LegacyTradeHistory newTrade)
        {

            DoLog(string.Format("We update LastSale MD for Size={0} and Price={1} for symbol {2}", newTrade.TradeQuantity, newTrade.TradePrice, newTrade.Symbol), MessageType.Information);

            LastSale ls = LastSales.Where(x => x.Symbol == newTrade.Symbol).FirstOrDefault();

            if (ls == null)
            {
                ls = new LastSale() { Change = 0, Msg = "LastSale", Symbol = newTrade.Symbol, Volume = 0 };
                LastSales.Add(ls);
                
            }

            ls.LastPrice = Convert.ToDecimal(newTrade.TradePrice);
            ls.LastShares = Convert.ToDecimal(newTrade.TradeQuantity);
            ls.LastTime = newTrade.TradeTimeStamp;
            ls.Volume += Convert.ToDecimal(newTrade.TradeQuantity);

            if (!ls.High.HasValue || Convert.ToDecimal(newTrade.TradePrice) > ls.High)
                ls.High = Convert.ToDecimal(newTrade.TradePrice);

            if (! ls.Low.HasValue || Convert.ToDecimal(newTrade.TradePrice) < ls.Low)
                ls.Low = Convert.ToDecimal(newTrade.TradePrice);

            DoLog(string.Format("LastSale updated..."), MessageType.Information);


            DoLog(string.Format("We udate Quotes MD for Size={0} and Price={1} for symbol {2}", newTrade.TradeQuantity, newTrade.TradePrice, newTrade.Symbol), MessageType.Information);
            UpdateQuotes(newTrade.Symbol);
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
                                SendNewTrade(legOrdReq, bestAsk, bestAsk.Size, socket);

                                //1.3-Calculamos el nuevo bestAsk
                                bestAsk = DepthOfBooks.Where(x =>   x.Symbol == legOrdReq.InstrumentId 
                                                                 && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();

                            }
                            else//order fully filled
                            {
                                //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
                                UpdatePriceLevel(ref bestAsk, DepthOfBook._ASK_ENTRY, Convert.ToDouble(prevLeftQty), socket);

                                //1.2- We send a trade by prevLeftQty
                                SendNewTrade(legOrdReq, bestAsk, prevLeftQty, socket);

                                fullFill = true;
                            }

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
                                     fullFill ? LegacyOrderRecord._STATUS_FILLED : LegacyOrderRecord._STATUS_PARTIALLY_FILLED,
                                     Convert.ToDouble(fullFill ? legOrdReq.Quantity : legOrdReq.Quantity - leftQty));

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
                    if (legOrdReq.Price.HasValue && legOrdReq.Price.Value < bestBid.Price)
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
                                SendNewTrade(legOrdReq, bestBid, bestBid.Size, socket);

                                //1.3-Calculamos el nuevo bestBid
                                bestBid = DepthOfBooks.Where(x =>   x.Symbol == legOrdReq.InstrumentId 
                                                                 && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();

                            }
                            else//order fully filled
                            {
                                //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
                                UpdatePriceLevel(ref bestBid, DepthOfBook._BID_ENTRY, Convert.ToDouble(prevLeftQty), socket);

                                //1.2- We send a trade by prevLeftQty
                                SendNewTrade(legOrdReq, bestBid, prevLeftQty, socket);

                                fullFill = true;
                            }
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
                                     fullFill ? LegacyOrderRecord._STATUS_FILLED : LegacyOrderRecord._STATUS_PARTIALLY_FILLED,
                                     Convert.ToDouble(fullFill ? legOrdReq.Quantity : legOrdReq.Quantity - leftQty));
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

                if (secMapping != null)
                {
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
                        DoSend<SecurityStatus>(socket, secStatus);
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
            if (subscrMsg.ServiceKey != "*")
            {


                SecurityStatus secStatus = new SecurityStatus();
                secStatus.Msg = "SecurityStatus";
                secStatus.Symbol = subscrMsg.ServiceKey;
                secStatus.cStatus = SecurityStatus._SEC_STATUS_TRADING;//: SecurityStatus._SEC_STATUS_HALTING;
                DoSend<SecurityStatus>(socket, secStatus);
                ProcessSubscriptionResponse(socket, "TI", subscrMsg.ServiceKey, subscrMsg.UUID, true);

                Thread processSecuritStatusThread = new Thread(ProcessSecuritStatusThread);
                processSecuritStatusThread.Start(new object[] { subscrMsg.ServiceKey, socket });

            }
            else
                ProcessSubscriptionResponse(socket, "TI", subscrMsg.ServiceKey, subscrMsg.UUID, false, string.Format("Uknown service key {0}", subscrMsg.Service));
        }

        private void ProcessLegacyOrderReqMock(IWebSocketConnection socket, string m)
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
                            UpdateQuotes(legOrdReq.InstrumentId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing LegactyOrderReq: {0}",ex.Message), MessageType.Error);
            }

          
        }

        private void ProcessSubscriptions(IWebSocketConnection socket,string m)
        {
            WebSocketSubscribeMessage subscrMsg = JsonConvert.DeserializeObject<WebSocketSubscribeMessage>(m);

            DoLog(string.Format("Incoming subscription for service {0} - ServiceKey:{1}", subscrMsg.Service,subscrMsg.ServiceKey), MessageType.Information);


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
            else if (subscrMsg.Service == "TI")
            {
                ProcessSecurityStatus(socket, subscrMsg);
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
