using DGTLBackendMock.BusinessEntities;
using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.SecurityList;
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

namespace DGTLMarketaDataPOC
{
    class Program
    {

        #region Public Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static ClientLoginResponse ClientLoginResponse { get; set; }

        public static Security Security { get; set; }

        #endregion

        #region Private Static Methods

        private static void DoLog(string message)
        {
            Console.WriteLine(message);
        }

        private static void DoSend(string strMsg)
        {
            DoLog(string.Format(">>{0}", strMsg));
            DGTLWebSocketClient.Send(strMsg);
        }

        private static void RequestMarketData(Security security)
        {
            WebSocketSubscribeMessage LSSubscribe = new WebSocketSubscribeMessage()
            {
                Msg = "Subscribe",
                Sender = 0,
                UserId = ClientLoginResponse.UserId,
                SubscriptionType = "S",
                JsonWebToken = ClientLoginResponse.JsonWebToken,
                Service = "LS",
                ServiceKey = security.Symbol
            };

            WebSocketSubscribeMessage LQSubscribe = new WebSocketSubscribeMessage()
            {
                Msg = "Subscribe",
                Sender = 0,
                UserId = ClientLoginResponse.UserId,
                SubscriptionType = "S",
                JsonWebToken = ClientLoginResponse.JsonWebToken,
                Service = "LQ",
                ServiceKey = security.Symbol
            };

            string strLSMsg = JsonConvert.SerializeObject(LSSubscribe, Newtonsoft.Json.Formatting.None,
                                             new JsonSerializerSettings
                                             {
                                                 NullValueHandling = NullValueHandling.Ignore
                                             });

            string strLQMsg = JsonConvert.SerializeObject(LQSubscribe, Newtonsoft.Json.Formatting.None,
                                             new JsonSerializerSettings
                                             {
                                                 NullValueHandling = NullValueHandling.Ignore
                                             });

            DoSend(strLSMsg);
            DoSend(strLQMsg);
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

        
        private static void MarketDataRefresh()
        {
            DoLog(string.Format("=================Refreshing Security {0} =================", Security.Symbol));
            DoLog(string.Format("Best Bid Price = {0}", Security.MarketData.BestBidPrice.HasValue ? Security.MarketData.BestBidPrice.Value.ToString("0.##") : "-"));
            DoLog(string.Format("Best Bid Size = {0}", Security.MarketData.BestBidSize.HasValue ? Security.MarketData.BestBidSize.Value.ToString("0.########") : "-"));
            DoLog(string.Format("Best Ask Price = {0}", Security.MarketData.BestAskPrice.HasValue ? Security.MarketData.BestAskPrice.Value.ToString("0.##") : "-"));
            DoLog(string.Format("Best Ask Size = {0}", Security.MarketData.BestAskSize.HasValue ? Security.MarketData.BestAskSize.Value.ToString("0.########") : "-"));
            DoLog(string.Format("Open= {0}", Security.MarketData.OpeningPrice.HasValue ? Security.MarketData.OpeningPrice.Value.ToString("0.##") : "-"));
            DoLog(string.Format("High= {0}", Security.MarketData.TradingSessionHighPrice.HasValue ? Security.MarketData.TradingSessionHighPrice.Value.ToString("0.##") : "-"));
            DoLog(string.Format("Low= {0}", Security.MarketData.TradingSessionLowPrice.HasValue ? Security.MarketData.TradingSessionLowPrice.Value.ToString("0.##") : "-"));
            DoLog(string.Format("Change= {0}%", Security.MarketData.NetChgPrevDay.HasValue ? Security.MarketData.NetChgPrevDay.Value.ToString("0.##") : "-"));
            DoLog(string.Format("24H Volume= {0}", Security.MarketData.NominalVolume.HasValue ? Security.MarketData.NominalVolume.Value.ToString("0.######") : "-"));
            DoLog("");
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

                DoLog(string.Format("Client successfully logged with token {0}", loginResp.JsonWebToken));
                //3- Subscribe market data for Security
                //Market data will be delivered through many services, for the moment we will use
                //      LS - LastSales will have all the information regarding the trades that took place (high, low, last, etc.)
                //      LQ - QuoteService will tell us the the best offers (best buy and sell) that we have for a security. 
                //           It is what fills the red/green holders that we can see in the image!
                // When we select a Product (SPOT) and Pair (XBT-USD) in the combos, we will have to fill the instrument holder (story 74)
                //      ---> In that context we only need the Quote (LQ) service
                // Only when we CLICK that instrument we will need the trades service (LS) to fill the header
                //      ---> Then we can make this call
                //Of course, every time we subscribe to some security , we will have to ususcribe to another one. 
                // That will be covered in the spec document
                RequestMarketData(Security);

            
            }
            else if (msg is LastSale)
            {
                //4.1 LastSale event arrived! We update the security in memory with the following fields
                LastSale lastSale = (LastSale)msg;

                lock (Security)
                {
                    Security.MarketData.LastTradeDateTime = lastSale.GetLastTime();
                    Security.MarketData.MDTradeSize = lastSale.LastShares;
                    Security.MarketData.Trade = lastSale.LastPrice;
                    Security.MarketData.NominalVolume = lastSale.Volume;
                    Security.MarketData.TradingSessionHighPrice = lastSale.High;
                    Security.MarketData.TradingSessionLowPrice = lastSale.Low;
                    Security.MarketData.OpeningPrice = lastSale.Open;
                    Security.MarketData.NetChgPrevDay = lastSale.Change;

                }
                MarketDataRefresh();
               
            }
            else if (msg is Quote)
            {
                //4.2 Quote event arrived! We update the security in memory with the following fields
                  Quote quote = (Quote)msg;

                  lock (Security)
                  {
                      Security.MarketData.BestBidPrice = quote.Bid;
                      Security.MarketData.BestBidSize = quote.BidSize;
                      Security.MarketData.BestAskPrice = quote.Ask;
                      Security.MarketData.BestAskSize = quote.AskSize;
                  }
                  MarketDataRefresh();
            }
            //4.3 --> Missing 2 services for DailySettlemetPrice FIX Price. Will be added later
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

            Security = new Security() { Symbol = Symbol, Description = Symbol, MarketData = new MarketData() };


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
