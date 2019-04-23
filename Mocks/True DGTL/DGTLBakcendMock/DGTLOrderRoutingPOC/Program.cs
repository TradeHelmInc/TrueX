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
                UUID = "OrderRoutingPOCUUID",
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

        private static void ShowOrderAck(LegacyOrderAck orderAckMsg)
        {
            DoLog("========== Order Ack Message ==========");
            DoLog(string.Format("OrderId:{0}", orderAckMsg.OrderId));
            DoLog(string.Format("UserId:{0}", orderAckMsg.UserId));
            DoLog(string.Format("ClOrderId:{0}", orderAckMsg.ClOrderId));
            DoLog(string.Format("InstrumentId:{0}", orderAckMsg.InstrumentId));
            DoLog(string.Format("Status:{0}", orderAckMsg.Status));
            DoLog(string.Format("Price:{0}", orderAckMsg.Price.HasValue? orderAckMsg.Price.Value.ToString("0.##"):null));
            DoLog(string.Format("Left Qty.:{0}", orderAckMsg.LeftQty.ToString("0.##")));
            DoLog(string.Format("Timestamp:{0}", orderAckMsg.Timestamp));
            DoLog(string.Format("Order Reject Reason:{0}", orderAckMsg.OrderRejectReason));
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

                DoLog(string.Format("Building and sending order..."));

                Order newOrder = BuildOrder();
                BuildOrderMessage(newOrder);
                
            }
            else if (msg is LegacyOrderAck)
            {
                LegacyOrderAck orderAckMsg = (LegacyOrderAck)msg;

                ShowOrderAck(orderAckMsg);
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

        private static void BuildOrderMessage(Order orderToSend)
        {

            LegacyOrderReq orderReq = new LegacyOrderReq();//TODO: Map from orderToSend!!
            orderReq.AccountId = orderToSend.Account;
            orderReq.ClOrderId = orderToSend.ClOrdId;
            orderReq.InstrumentId = orderToSend.Symbol;
            orderReq.Msg = "LegacyOrderReq";
            orderReq.cOrderType = orderToSend.OrdType == OrdType.Limit ? LegacyOrderReq._ORD_TYPE_LIMIT : LegacyOrderReq._ORD_TYPE_MARKET;
            orderReq.Price = orderToSend.Price.HasValue ? (decimal?) Convert.ToDecimal(orderToSend.Price.Value) : null;
            orderReq.Quantity = Convert.ToDecimal(orderToSend.OrderQty);
            orderReq.cSide = orderToSend.Side == Side.Buy ? LegacyOrderReq._SIDE_BUY : LegacyOrderReq._SIDE_SELL;
            orderReq.cTimeInForce =LegacyOrderReq._TIF_DAY;
            orderReq.UserId = ClientLoginResponse.UserId;
            orderReq.JsonWebToken = ClientLoginResponse.JsonWebToken;


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
