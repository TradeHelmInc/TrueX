using DGTLBackendMock.BusinessEntities;
using DGTLBackendMock.BusinessEntities.enums;
using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.Subscription;
using DGTLBackendMock.DataAccessLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLOrderRoutingPOC
{
    class Program
    { 
        #region Protected Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static ClientLoginResponse ClientLoginResponse { get; set; }

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

        private static void DoSend(string strMsg)
        {
            DoLog(string.Format(">>{0}", strMsg));
            DGTLWebSocketClient.Send(strMsg);
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
            }
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

        private static string BuildClOrdId()
        { 
            //We will use the total milliseconds in today

            TimeSpan elapsed = DateTime.Now - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            string clOrdId = elapsed.TotalMilliseconds.ToString();

            return clOrdId;
        }

        private void BuildOrderMessage(Order orderToSend)
        {

            LegacyOrderReq orderReq = new LegacyOrderReq();//TODO: Map from orderToSend!!


            string strLegacyOrderReq = JsonConvert.SerializeObject(orderReq, Newtonsoft.Json.Formatting.None,
                                         new JsonSerializerSettings
                                         {
                                             NullValueHandling = NullValueHandling.Ignore
                                         });

            DoSend(strLegacyOrderReq);
        }

        private static Order BuildOrder()
        {
            Order newOrder = new Order()
            {
                Account = ConfigurationManager.AppSettings["Account"],
                Currency = "USD",
                ClOrdId = BuildClOrdId(),
                OrderQty = Convert.ToDouble(ConfigurationManager.AppSettings["Quantity"]),
                OrdType = (OrdType)Convert.ToChar(ConfigurationManager.AppSettings["OrdType"]),
                Price = ConfigurationManager.AppSettings["InitialPrice"] != "" ? (double?)Convert.ToDouble(ConfigurationManager.AppSettings["InitialPrice"]) : null,
                Security = new Security() { Symbol = ConfigurationManager.AppSettings["Symbol"] },
                Side = (Side)Convert.ToChar(ConfigurationManager.AppSettings["Side"]),
                TimeInForce = (TimeInForce)Convert.ToChar(ConfigurationManager.AppSettings["TimeInForce"]),

            };

            return newOrder;
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

            Order newOrder = BuildOrder();

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
