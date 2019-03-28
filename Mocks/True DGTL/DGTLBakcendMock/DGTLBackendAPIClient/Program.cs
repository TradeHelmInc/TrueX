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
using System.Threading.Tasks;

namespace DGTLBackendAPIClient
{
    class Program
    {
        #region Protected Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static string JWTToken { get; set; }

        protected static string UUID { get; set; }

        protected static string UserId { get; set; }

        #endregion

        #region Private Static Methods

        private static void DoLog(string message)
        {
            Console.WriteLine(message);
        }


        private static void ShowCommands() 
        {
            Console.WriteLine();
            Console.WriteLine("-------------- Enter Commands to Invoke -------------- ");
            Console.WriteLine("LoginClient <userId> <UUID> <Password>");
            Console.WriteLine("LogoutClient (Cxt credentials will be used)");
            Console.WriteLine("Subscribe <Service> <ServiceKey>");
            Console.WriteLine("-CLEAR");
            Console.WriteLine();
        
        }

        private static void ProcessJsonMessage<T>(T msg)
        {

            string strMessage = JsonConvert.SerializeObject(msg, Newtonsoft.Json.Formatting.None,
                                              new JsonSerializerSettings
                                              {
                                                  NullValueHandling = NullValueHandling.Ignore
                                              });
            DoLog(string.Format("<<{0}", strMessage));
        
        }

        private static void ProcessEvent(WebSocketMessage msg)
        {
            if (msg is ClientLoginResponse)
            {
                ClientLoginResponse loginResp = (ClientLoginResponse)msg;

                if (loginResp.JsonWebToken != null)
                {
                    JWTToken = loginResp.JsonWebToken;
                    UUID = loginResp.UUID;
                    UserId = loginResp.UserId;
                }

                ProcessJsonMessage<ClientLoginResponse>(loginResp);
            }
            else if (msg is ClientReject)
                ProcessJsonMessage<ClientReject>((ClientReject)msg);
            else if (msg is SubscriptionResponse)
                ProcessJsonMessage<SubscriptionResponse>((SubscriptionResponse)msg);
            else if (msg is AccountRecord)
                ProcessJsonMessage<AccountRecord>((AccountRecord)msg);
            else if (msg is DailySettlementPrice)
                ProcessJsonMessage<DailySettlementPrice>((DailySettlementPrice)msg);
            else if (msg is FirmRecord)
                ProcessJsonMessage<FirmRecord>((FirmRecord)msg);
            else if (msg is OfficialFixingPrice)
                ProcessJsonMessage<OfficialFixingPrice>((OfficialFixingPrice)msg);
            else if (msg is RefereceRateMsg)
                ProcessJsonMessage<RefereceRateMsg>((RefereceRateMsg)msg);
            else if (msg is SecurityMasterRecord)
                ProcessJsonMessage<SecurityMasterRecord>((SecurityMasterRecord)msg);
            else if (msg is UserRecord)
                ProcessJsonMessage<UserRecord>((UserRecord)msg);
            else if (msg is LastSale)
                ProcessJsonMessage<LastSale>((LastSale)msg);
            else if (msg is Quote)
                ProcessJsonMessage<Quote>((Quote)msg);
            else if (msg is ClientLogoutResponse)
            {
                ClientLogoutResponse logoutResp = (ClientLogoutResponse)msg;

                JWTToken = null;
                UserId = null;
                UUID = null;

                ProcessJsonMessage<ClientLogoutResponse>((ClientLogoutResponse)msg);
            }
            else if (msg is ClientHeartbeatRequest)
            {
                ClientHeartbeatRequest heartBeatReq = (ClientHeartbeatRequest)msg;
                ProcessJsonMessage<ClientHeartbeatRequest>((ClientHeartbeatRequest)msg);
                ProcessHeartbeat(heartBeatReq.SeqNum);
            }
            else if (msg is UnknownMessage)
            {
                UnknownMessage unknownMsg = (UnknownMessage)msg;

                DoLog(string.Format("<<unknown {0}", unknownMsg.Resp));

            }
            else if (msg is ErrorMessage)
            {
                ErrorMessage errorMsg = (ErrorMessage)msg;

                DoLog(string.Format("<<unknown {0}", errorMsg.Error));

            }
            else
                DoLog(string.Format("<<Unknown message type {0}", msg.ToString()));

            Console.WriteLine();
        
        }

        private static void ProcessLoginClient(string[] param)
        {
            if (param.Length == 4)
            {
                WebSocketLoginMessage login = new WebSocketLoginMessage()
                {
                    Msg = "ClientLogin",
                    Sender = 0,
                    UserId = param[1],
                    UUID = param[2],
                    Password = param[3]
                };

                string strMsg = JsonConvert.SerializeObject(login, Newtonsoft.Json.Formatting.None,
                                                 new JsonSerializerSettings
                                                 {
                                                     NullValueHandling = NullValueHandling.Ignore
                                                 });

                DoSend(strMsg);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for LoginClient message"));
        
        }


        private static void ProcessHeartbeat(int seqNum)
        {

            if (JWTToken == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            ClientHeartbeatResponse heartbeat = new ClientHeartbeatResponse()
            {
                Msg = "ClientHeartbeatResponse",
                Sender = 0,
                UserId = UserId,
                JsonWebToken = JWTToken,
                SeqNum = seqNum
            };

            string strMsg = JsonConvert.SerializeObject(heartbeat, Newtonsoft.Json.Formatting.None,
                                             new JsonSerializerSettings
                                             {
                                                 NullValueHandling = NullValueHandling.Ignore
                                             });

            DoSend(strMsg);

        }

        private static void ProcessLogoutClient(string[] param)
        {

            if (JWTToken == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            if (param.Length == 1)
            {
                WebSocketLogoutMessage logout = new WebSocketLogoutMessage()
                {
                    Msg = "ClientLogout",
                    Sender = 0,
                    UserId = UserId,
                    JsonWebToken = JWTToken,
                };

                string strMsg = JsonConvert.SerializeObject(logout, Newtonsoft.Json.Formatting.None,
                                                 new JsonSerializerSettings
                                                 {
                                                     NullValueHandling = NullValueHandling.Ignore
                                                 });

                DoSend(strMsg);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for logout message"));

        }


        private static void DoSend(string strMsg)
        {
            DoLog(string.Format(">>{0}", strMsg));
            DGTLWebSocketClient.Send(strMsg);
        }

        private static void ProcessSubscribe(string[] param)
        {
            if (JWTToken == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            if (param.Length >= 2)
            {
                WebSocketSubscribeMessage subscribe = new WebSocketSubscribeMessage()
                {
                    Msg = "Subscribe",
                    Sender = 0,
                    UserId = UserId,
                    SubscriptionType = "S",
                    JsonWebToken = JWTToken,
                    Service = param[1],
                    ServiceKey = param.Length == 3 ? param[2] : "*"
                };

                string strMsg = JsonConvert.SerializeObject(subscribe, Newtonsoft.Json.Formatting.None,
                                                 new JsonSerializerSettings
                                                 {
                                                     NullValueHandling = NullValueHandling.Ignore
                                                 });

                DoSend(strMsg);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for logout message"));
        
        }

        private static void ProcessCommand(string cmd)
        {

            string[] param = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            string mainCmd = param[0];

            if (mainCmd == "LoginClient")
            {
                ProcessLoginClient(param);
            }
          
            else if (mainCmd == "LogoutClient")
            {
                ProcessLogoutClient(param);
            }
            else if (mainCmd == "Subscribe")
            {
                ProcessSubscribe(param);
            }
            else if (mainCmd.ToUpper() == "CLEAR") 
            {
                Console.Clear();
                ShowCommands();
            }
            else
                DoLog(string.Format("Unknown command {0}", mainCmd));
        }

        #endregion

        static void Main(string[] args)
        {
            try
            {
                string WebSocketURL = ConfigurationManager.AppSettings["WebSocketURL"];
                DGTLWebSocketClient = new DGTLWebSocketClient(WebSocketURL, ProcessEvent);
                DoLog(string.Format("Connecting to URL {0}", WebSocketURL));
                DGTLWebSocketClient.Connect();
                DoLog("Successfully connected");

                ShowCommands();

                while (true)
                {
                    string cmd = Console.ReadLine();
                    ProcessCommand(cmd);
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical Error: {0}", ex.Message));
                Console.ReadKey();
            
            }


        }
    }
}
