using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Subscription;
using DGTLBackendMock.Common.DTO.Account;
using DGTLBackendMock.DataAccessLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DGTLBackendMock.BusinessEntities;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.BusinessEntities.enums;


namespace DGTLFullMarketDataPOC
{
    class Program
    {
        #region Protected Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static ClientLoginResponse ClientLoginResponse { get; set; }

        protected static bool InitialSnapshotReceived { get; set; }

        protected static Dictionary<string, Security> Securities { get; set; }

        #endregion

        #region Private Static Methods

        private static void DoLog(string message)
        {
            Console.WriteLine(message);
        }

        private static void DoSubscribe(string service, string serviceKey)
        {
            WebSocketSubscribeMessage subscribe = new WebSocketSubscribeMessage()
            {
                Msg = "Subscribe",
                Sender = 0,
                UUID = "OrderBookPOCUUID",
                UserId = ClientLoginResponse.UserId,
                SubscriptionType = "S",
                JsonWebToken = ClientLoginResponse.JsonWebToken,
                Service = service,
                ServiceKey = serviceKey
            };

            string strMsg = JsonConvert.SerializeObject(subscribe, Newtonsoft.Json.Formatting.None,
                                             new JsonSerializerSettings
                                             {
                                                 NullValueHandling = NullValueHandling.Ignore
                                             });

            DoSend(strMsg);
        }

        public static void SubscribeLastSales()
        {
            foreach (string symbol in Securities.Keys)
            {
                DoSubscribe("LS", symbol);
                Thread.Sleep(100);
            }
        }

        public static void SubscribeQuotes()
        {
            foreach (string symbol in Securities.Keys)
            {
                DoSubscribe("LQ", symbol);
                Thread.Sleep(100);
            }
        }

        public static void SubscribeOrderBook()
        {
            foreach (string symbol in Securities.Keys)
            {
                DoSubscribe("LD", symbol);
                Thread.Sleep(100);
            }
        }

        private static void PublishOrderBookThread(object param)
        {

            while (!InitialSnapshotReceived)
                Thread.Sleep(1000);


            while (InitialSnapshotReceived)
            {
                lock (Securities)
                {
                    foreach (Security Security in Securities.Values)
                    {

                        List<PriceLevel> bids = Security.MarketData.OrderBook.Where(x => x.OrderBookEntryType == OrderBookEntryType.Bid)
                                                                             .OrderByDescending(x => x.Price).ToList();

                        List<PriceLevel> asks = Security.MarketData.OrderBook.Where(x => x.OrderBookEntryType == OrderBookEntryType.Ask)
                                                         .OrderBy(x => x.Price).ToList();

                        DoLog(string.Format("======================Refreshing Order Book for Symbol {0} @{1}======================", Security.Symbol, DateTime.Now.ToString()));
                        DoLog("============ Bids ==============");
                        bids.ForEach(x => DoLog(string.Format("Size = {0} Price = {1}", x.Size.ToString("0.#####"), x.Price.ToString("0.##"))));

                        DoLog("");

                        DoLog("============ Asks ==============");
                        asks.OrderByDescending(x => x.Price)
                            .ToList()
                            .ForEach(x => DoLog(string.Format("Size = {0} Price = {1}", x.Size.ToString("0.#####"), x.Price.ToString("0.##"))));
                        DoLog("========================================================================================================");

                        DoLog(" ");
                        DoLog(" ");
                        DoLog(" ");
                    }
                }

                Thread.Sleep(5000);
            }

        }

        private static void ProcessDepthOfBook(DepthOfBook depthOfBookDelta)
        {

            lock (Securities)
            {
                Security Security = Securities[depthOfBookDelta.Symbol];

                if (Security == null)
                    return;

                if (depthOfBookDelta.cAction == DepthOfBook._ACTION_INSERT)
                {
                    InitialSnapshotReceived = true;
                    // If the price level existed, we create the price level but a HUGE warning (WARNING3) should be logged
                    PriceLevel pl = Security.MarketData.OrderBook.Where(x => x.Price == depthOfBookDelta.Price
                                                                            && x.IsBidOrAsk(depthOfBookDelta.IsBid()))
                                                                 .FirstOrDefault();
                    if (pl != null)
                    {
                        pl.Size = depthOfBookDelta.Size;
                        DoLog(string.Format("WARNING3 - Received ADD DepthOfBook message for a price level that existed. Price Level = {0}",
                              depthOfBookDelta.Price.ToString("0.##")));

                    }
                    else
                    {
                        PriceLevel newPl = new PriceLevel()
                        {
                            Price = depthOfBookDelta.Price,
                            Size = depthOfBookDelta.Size,
                            OrderBookEntryType = depthOfBookDelta.IsBid() ? OrderBookEntryType.Bid : OrderBookEntryType.Ask
                        };

                        Security.MarketData.OrderBook.Add(newPl);
                    }

                }
                else if (depthOfBookDelta.cAction == DepthOfBook._ACTION_CHANGE)
                {
                    PriceLevel pl = Security.MarketData.OrderBook.Where(x => x.Price == depthOfBookDelta.Price
                                                        && x.IsBidOrAsk(depthOfBookDelta.IsBid()))
                                             .FirstOrDefault();
                    if (pl != null)
                        pl.Size = depthOfBookDelta.Size;
                    else
                    {
                        //This is not ok, we receive a PriceLevel for a Price that didn't exist. 
                        //We create the price level, but we log a HUGE warning (WARNING4)
                        PriceLevel newPl = new PriceLevel()
                        {
                            Price = depthOfBookDelta.Price,
                            Size = depthOfBookDelta.Size,
                            OrderBookEntryType = depthOfBookDelta.IsBid() ? OrderBookEntryType.Bid : OrderBookEntryType.Ask
                        };
                        Security.MarketData.OrderBook.Add(newPl);

                        DoLog(string.Format("WARNING4 - Received CHANGE DepthOfBook message for a price level that did not exist. Price Level = {0}",
                                            depthOfBookDelta.Price.ToString("0.##")));

                    }


                }
                else if (depthOfBookDelta.cAction == DepthOfBook._ACTION_REMOVE)
                {
                    PriceLevel pl = Security.MarketData.OrderBook.Where(x => x.Price == depthOfBookDelta.Price
                                        && x.IsBidOrAsk(depthOfBookDelta.IsBid()))
                             .FirstOrDefault();

                    if (pl != null)
                        Security.MarketData.OrderBook.Remove(pl);
                    else
                    {
                        //Another HUGE warning. We receive a message to delete a price level that didn't exist
                        //We have to log another HUGE warning (WARNING5)
                        DoLog(string.Format("WARNING5 - Received REMOVE DepthOfBook message for a price level that did not exist. Price Level = {0}",
                                            depthOfBookDelta.Price.ToString("0.##")));

                    }

                }
            }

        }

        private static void ProcessEvent(WebSocketMessage msg)
        {
            if (msg is ClientLoginResponse)
            {
                ClientLoginResponse loginResp = (ClientLoginResponse)msg;

                if (loginResp.JsonWebToken != null)
                {
                    ClientLoginResponse = loginResp;
                }

                Thread publishOrderBookThread = new Thread(PublishOrderBookThread);
                publishOrderBookThread.Start();

                DoLog(string.Format("Client successfully logged with token {0}", loginResp.JsonWebToken));
                SubscribeLastSales();
                SubscribeQuotes();
                Thread.Sleep(1000);
                SubscribeOrderBook();
            }
            else if (msg is DepthOfBook)
            {
                DepthOfBook depthOfBookDelta = (DepthOfBook)msg;
                ProcessDepthOfBook(depthOfBookDelta);
            }
            else if  (msg is LastSale)
            {
                LastSale lastSale = (LastSale)msg;
                DoLog(string.Format("Received last sale for symbol {1}: {0}", lastSale.LastPrice, lastSale.Symbol));
            
            }
            else if (msg is Quote)
            {
                Quote quote = (Quote)msg;
                DoLog(string.Format("Received quote for symbol {4}: Bid {0}-{1} -- Ask {2}-{3}",
                     quote.BidSize, quote.Bid, quote.AskSize, quote.Ask, quote.Symbol));

            }
        }

        private static void DoSend(string strMsg)
        {
            DoLog(string.Format(">>{0}", strMsg));
            DGTLWebSocketClient.Send(strMsg);
        }

        private static void LoginClient(string userId, string UUID, string password)
        {
            WebSocketLoginMessage login = new WebSocketLoginMessage()
            {
                Msg = "ClientLogin",
                Sender = 0,
                UserId = userId,
                UUID = UUID,
                Password = password
            };

            string strMsg = JsonConvert.SerializeObject(login, Newtonsoft.Json.Formatting.None,
                                             new JsonSerializerSettings
                                             {
                                                 NullValueHandling = NullValueHandling.Ignore
                                             });

            DoSend(strMsg);

        }

        #endregion

        static void Main(string[] args)
        {
            string WebSocketURL = ConfigurationManager.AppSettings["WebSocketURL"];
            string Sender = ConfigurationManager.AppSettings["Sender"];
            string UUID = ConfigurationManager.AppSettings["UUID"];
            string UserId = ConfigurationManager.AppSettings["UserId"];
            string Password = ConfigurationManager.AppSettings["Password"];
           
            Security sec1 = new Security() { Symbol = ConfigurationManager.AppSettings["Symbol1"], MarketData = new MarketData() { OrderBook = new List<PriceLevel>() } };
            Security sec2 = new Security() { Symbol = ConfigurationManager.AppSettings["Symbol2"], MarketData = new MarketData() { OrderBook = new List<PriceLevel>() } };
            Security sec3 = new Security() { Symbol = ConfigurationManager.AppSettings["Symbol3"], MarketData = new MarketData() { OrderBook = new List<PriceLevel>() } };


            Securities = new Dictionary<string, Security>();
            Securities.Add(sec1.Symbol, sec1);
            Securities.Add(sec2.Symbol, sec2);
            Securities.Add(sec3.Symbol, sec3);

            //1- We do all the logging and connection procedure
            DGTLWebSocketClient = new DGTLWebSocketClient(WebSocketURL, ProcessEvent);
            DoLog(string.Format("Connecting to URL {0}", WebSocketURL));
            DGTLWebSocketClient.Connect().Wait();
            DoLog("Successfully connected");


            //2-We log the user and wait for the response
            DoLog(string.Format("Logging user {0}", UserId));
            LoginClient(UserId, UUID, Password);

            Console.ReadKey();
        }
    }
}
