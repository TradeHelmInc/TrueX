using DGTLBackendMock.BusinessEntities;
using DGTLBackendMock.BusinessEntities.enums;
using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.Subscription;
using DGTLBackendMock.DataAccessLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DGTLOrderBookPOC
{
    class Program
    {
        #region Protected Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static ClientLoginResponse ClientLoginResponse { get; set; }

        protected static bool InitialSnapshotReceived { get; set; }

        public static Security Security { get; set; }

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

        public static void SubscribeOrderBook()
        {
            DoSubscribe("LD", Security.Symbol);
        }

        private static void PublishOrderBookThread(object param)
        {

            while (!InitialSnapshotReceived)
                Thread.Sleep(1000);


            while (InitialSnapshotReceived)
            {
                lock (Security)
                {
                    List<PriceLevel> bids = Security.MarketData.OrderBook.Where(x => x.OrderBookEntryType == OrderBookEntryType.Bid)
                                                                         .OrderByDescending(x => x.Price).ToList();

                    List<PriceLevel> asks = Security.MarketData.OrderBook.Where(x => x.OrderBookEntryType == OrderBookEntryType.Ask)
                                                     .OrderBy(x => x.Price).ToList();

                    DoLog("========================================================================================================");
                    DoLog("============ Bids ==============");
                    bids.ForEach(x => DoLog(string.Format("Size = {0} Price = {1}", x.Size.ToString("0.#####"), x.Price.ToString("0.##"))));

                    DoLog("");

                    DoLog("============ Asks ==============");
                    asks.ForEach(x => DoLog(string.Format("Size = {0} Price = {1}", x.Size.ToString("0.#####"), x.Price.ToString("0.##"))));
                    DoLog("========================================================================================================");

                    DoLog(" ");
                }

                Thread.Sleep(5000);
            }
        
        }

        private static void ProcessDepthOfBook(DepthOfBook depthOfBookDelta)
        {

            lock (Security)
            {

                if (depthOfBookDelta.cAction == DepthOfBook._ACTION_SNAPSHOT)
                {
                    InitialSnapshotReceived = true;
                    //We have the initial snapshot for a given price level. 
                    // If it didn't exsist (first snapshot), we just add it to the price level list
                    // If it existed (maybe because we wanted some snapshot refresh), we just update its quantity
                    PriceLevel pl = Security.MarketData.OrderBook.Where(x => x.Price == depthOfBookDelta.Price
                                                                            && x.IsBidOrAsk(depthOfBookDelta.IsBid())).FirstOrDefault();

                    if (pl != null)
                    {
                        pl.Size = depthOfBookDelta.Size;
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
                else if (depthOfBookDelta.cAction == DepthOfBook._ACTION_INSERT)
                {
                    InitialSnapshotReceived = true;
                    // If the price level existed, we create the price level but a HUGE warning (WARNING3) should be logged
                    PriceLevel pl = Security.MarketData.OrderBook.Where(x => x.Price == depthOfBookDelta.Price
                                                                            && x.IsBidOrAsk(depthOfBookDelta.IsBid()))
                                                                 .FirstOrDefault();
                    if (pl != null)
                    {
                        pl.Size += depthOfBookDelta.Size;
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
                        pl.Size += depthOfBookDelta.Size;
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
                SubscribeOrderBook();
            }
            if (msg is DepthOfBook)
            {
                DepthOfBook depthOfBookDelta = (DepthOfBook)msg;
                ProcessDepthOfBook(depthOfBookDelta);
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
            string Symbol = ConfigurationManager.AppSettings["Symbol"];
            Security = new Security() { Symbol = Symbol, MarketData = new MarketData() { OrderBook = new List<PriceLevel>() } };


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
