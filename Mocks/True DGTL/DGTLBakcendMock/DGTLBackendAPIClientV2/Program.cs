using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth.V2;
using DGTLBackendMock.DataAccessLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendAPIClientV2
{
    class Program
    {
        #region Protected Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static string Token { get; set; }

        protected static string UUID { get; set; }

        protected static string UserId { get; set; }

        protected static string TempUser { get; set; }

        protected static string TempPassword { get; set; }

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
            Console.WriteLine("RouteOrder <AccountId> (Harcoded..)");
            Console.WriteLine("RouteAndCancelOrder <AccountId> (Harcoded..)");
            Console.WriteLine("ResetPassword <OldPwd> <NewPwd>");
            Console.WriteLine("MassiveCancel");
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

        private static void DoSend(string strMsg)
        {
            DoLog(string.Format(">>{0}", strMsg));
            DGTLWebSocketClient.Send(strMsg);
        }

        private static void DoSend<T>(T obj)
        {
            string strMsg = JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None,
                                                    new JsonSerializerSettings
                                                    {
                                                        NullValueHandling = NullValueHandling.Ignore
                                                    });


            DoSend(strMsg);
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
           
            else if (mainCmd.ToUpper() == "CLEAR")
            {
                Console.Clear();
                ShowCommands();
            }
            else
                DoLog(string.Format("Unknown command {0}", mainCmd));
        }

        private static string GetMyIpAddress()
        {
            String strHostName = string.Empty;
            // Getting Ip address of local machine...
            // First get the host name of local machine.
            strHostName = Dns.GetHostName();
            Console.WriteLine("Local Machine's Host Name: " + strHostName);
            // Then using host name, get the IP address list..
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;

            for (int i = 0; i < addr.Length; i++)
            {
                return addr[i].ToString();
            }

            return "";
        
        }

        #endregion

        #region Protected Static Methods

        private static void ProcessEvent(WebSocketMessageV2 msg)
        {

            if (msg is ClientLoginResponse)
            {
                ClientLoginResponse loginResp = (ClientLoginResponse)msg;

                if (loginResp.Token != null)
                {
                   
                    UUID = loginResp.Uuid;
                    UserId = loginResp.UserId;
                }

                ProcessJsonMessage<ClientLoginResponse>(loginResp);
            }
            else if (msg is TokenResponse)
            {
                TokenResponse tokenResp = (TokenResponse)msg;
                Token = tokenResp.Token;
               
                DoLog(string.Format("Creating Secret for token {0}", tokenResp.Token));
                string secret = ""; //Now we prepare the hash with UserId and Password (using Token received)
                ClientLogin login = new ClientLogin() { Msg = "ClientLogin", Secret = secret, Uuid = secret };
                DoSend<ClientLogin>(login);
                DoLog(string.Format("Secret {1} for token {0} created and sent", tokenResp.Token, secret));
            }
            else if (msg is ClientLogout)
            {
                ClientLogout logoutResp = (ClientLogout)msg;

                Token = null;
                UserId = null;
                UUID = null;

                ProcessJsonMessage<ClientLogout>((ClientLogout)msg);
            }
            else if (msg is ClientHeartbeat)
            {
                ClientHeartbeat heartBeat = (ClientHeartbeat)msg;
                ProcessJsonMessage<ClientHeartbeat>((ClientHeartbeat)msg);
                ProcessHeartbeat(heartBeat);
            }
            //else if (msg is ClientReject)
            //    ProcessJsonMessage<ClientReject>((ClientReject)msg);
            //else if (msg is SubscriptionResponse)
            //    ProcessJsonMessage<SubscriptionResponse>((SubscriptionResponse)msg);
            //else if (msg is AccountRecord)
            //    ProcessJsonMessage<AccountRecord>((AccountRecord)msg);
            //else if (msg is DailySettlementPrice)
            //    ProcessJsonMessage<DailySettlementPrice>((DailySettlementPrice)msg);
            //else if (msg is FirmRecord)
            //    ProcessJsonMessage<FirmRecord>((FirmRecord)msg);
            //else if (msg is OfficialFixingPrice)
            //    ProcessJsonMessage<OfficialFixingPrice>((OfficialFixingPrice)msg);
            //else if (msg is RefereceRateMsg)
            //    ProcessJsonMessage<RefereceRateMsg>((RefereceRateMsg)msg);
            //else if (msg is SecurityMasterRecord)
            //    ProcessJsonMessage<SecurityMasterRecord>((SecurityMasterRecord)msg);
            //else if (msg is UserRecord)
            //    ProcessJsonMessage<UserRecord>((UserRecord)msg);
            //else if (msg is LastSale)
            //    ProcessJsonMessage<LastSale>((LastSale)msg);
            //else if (msg is Quote)
            //    ProcessJsonMessage<Quote>((Quote)msg);
            //else if (msg is CreditRecordUpdate)
            //    ProcessJsonMessage<CreditRecordUpdate>((CreditRecordUpdate)msg);
            //else if (msg is DepthOfBook)
            //    ProcessJsonMessage<DepthOfBook>((DepthOfBook)msg);
            //else if (msg is LegacyOrderAck)
            //    ProcessJsonMessage<LegacyOrderAck>((LegacyOrderAck)msg);
            //else if (msg is LegacyOrderCancelRejAck)
            //    ProcessJsonMessage<LegacyOrderCancelRejAck>((LegacyOrderCancelRejAck)msg);
         
         
            else if (msg is UnknownMessageV2)
            {
                UnknownMessageV2 unknownMsg = (UnknownMessageV2)msg;

                DoLog(string.Format("<<unknown {0}", unknownMsg.Resp));

            }
            else if (msg is ErrorMessageV2)
            {
                ErrorMessageV2 errorMsg = (ErrorMessageV2)msg;

                DoLog(string.Format("<<unknown {0}", errorMsg.Error));

            }
            else
                DoLog(string.Format("<<Unknown message type {0}", msg.ToString()));

            Console.WriteLine();

        }

        private static void ProcessLoginClient(string[] param)
        {

            if (param.Length == 3)
            {
                TempUser = param[1];
                TempPassword = param[2];

                TokenRequest tokenReq = new TokenRequest() { Msg = "TokenRequest", SourceIP = GetMyIpAddress() };
                DoSend<TokenRequest>(tokenReq);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for LoginClient message"));
        }

        private static void ProcessHeartbeat(ClientHeartbeat heartBeat)
        {

            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            ClientHeartbeat heartbeatResp = new ClientHeartbeat()
            {
                Msg = "ClientHeartbeat",
                Token = heartBeat.Token,
                Uuid = heartBeat.Uuid
            };
            DoSend<ClientHeartbeat>(heartBeat);
        }

        private static void ProcessLogoutClient(string[] param)
        {

            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            if (param.Length == 1)
            {
                ClientLogout logout = new ClientLogout()
                {
                    Msg = "ClientLogout",
                    Token = Token,
                    UserId = UserId,
                    Uuid = UUID
                };

                DoSend<ClientLogout>(logout);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for logout message"));

        }


        #endregion

        static void Main(string[] args)
        {
            try
            {
                string WebSocketURL = ConfigurationManager.AppSettings["WebSocketURL"];
                DGTLWebSocketClient = new DGTLWebSocketClientV2(WebSocketURL, ProcessEvent);
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
