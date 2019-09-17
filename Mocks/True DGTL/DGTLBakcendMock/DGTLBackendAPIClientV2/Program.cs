using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Auth.V2;
using DGTLBackendMock.Common.DTO.MarketData.V2;
using DGTLBackendMock.Common.DTO.OrderRouting.V2;
using DGTLBackendMock.Common.DTO.SecurityList.V2;
using DGTLBackendMock.Common.DTO.Subscription.V2;
using DGTLBackendMock.Common.Util;
using DGTLBackendMock.DataAccessLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
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

        protected static long UserId { get; set; }

        protected static long FirmId { get; set; }

        protected static string TempUser { get; set; }

        protected static string TempPassword { get; set; }

        protected static ClientOrderReq LastOrderCreated { get; set; }

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
            Console.WriteLine("FirmListRequest");
            Console.WriteLine("CreditLimitUpdateRequest <FirmId> <Status> <Limit> <Total> <MaxTradeSize>");
            Console.WriteLine("RouteOrder <AccountId> (Harcoded..)");
            Console.WriteLine("CancelLastCreatedOrder");
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

        private static void ProcessSubscribe(string[] param)
        {
            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            if (param.Length >= 2)
            {
                Subscribe subscribe = new Subscribe()
                {
                    Msg = "Subscribe",
                    cAction = Subscribe._ACTION_SUBSCRIBE,
                    JsonWebToken = Token,
                    Service = param[1],
                    ServiceKey = param.Length == 3 ? param[2] : "*",
                    UUID = UUID,
                };

                string strMsg = JsonConvert.SerializeObject(subscribe, Newtonsoft.Json.Formatting.None,
                                                 new JsonSerializerSettings
                                                 {
                                                     NullValueHandling = NullValueHandling.Ignore
                                                 });

                DoSend(strMsg);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for subscription message"));

        }

        private static void ProcessCommand(string cmd)
        {

            string[] param = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            string mainCmd = param[0];

            if (mainCmd == "LoginClient")
            {
                ProcessLoginClient(param);
            }
            else if (mainCmd == "Subscribe")
            {
                ProcessSubscribe(param);
            }
            else if (mainCmd == "FirmListRequest")
            {
                ProcessFirmListRequest(param);
            }
            else if (mainCmd == "CreditLimitUpdateRequest")
            {
                ProcessCreditLimitUpdateRequest(param);
            }
            else if (mainCmd == "LogoutClient")
            {
                ProcessLogoutClient(param);
            }
            else if (mainCmd == "RouteOrder")
            {
                ProcessRouteOrder(param);
            }
            else if (mainCmd == "CancelLastCreatedOrder")
            {
                ProcessCancelLastCreatedOrderparam(param);
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


        private static void DecryptTest()
        {
            string Token = "d1d59655-6bb8-4322-8424-14b724a45b77";
            string Secret = "h/VNzVF6HoIMZ+cg/Jz9GAKmIkL4HDooz4c18+3LT8DIQlcPzApftN212zUSof2D";

            byte[] KeyBytes = AESCryptohandler.makePassPhrase(Token);

            byte[] IV = KeyBytes;

            byte[] msgToDecrypt = Convert.FromBase64String(Secret);

            string origMsg= AESCryptohandler.DecryptStringFromBytes(msgToDecrypt, KeyBytes, IV);
  
        }

        private static string GetSecret(string login, string pwd, string token)
        {
            //string msg = login + "---" + pwd; --> old format

            //1-key must be a fixed 16 bytes (pad / truncate if necessary)
            byte[] KeyBytes = AESCryptohandler.makePassPhrase(token);
            //2-set the key - NOTE: iv will be the same as the key
            byte[] IV = KeyBytes;

            //3-//convert the JSON object to a string
            var msg = new
            {
                UserId = login,
                Password = pwd

            };
            string strMsg = JsonConvert.SerializeObject(msg);

            //4-Run the encryption in CBC mode!
            byte[] encrypted = AESCryptohandler.EncryptStringToBytes(strMsg, KeyBytes, IV);

            //5- Convert byte array to base64
            string secret = Convert.ToBase64String(encrypted);
            
            //6-Roundtrip test
            string roundtrip = AESCryptohandler.DecryptStringFromBytes(encrypted, KeyBytes, IV);

            return secret;
        }

        #endregion

        #region Protected Static Methods

        private static void ProcessTokenResponse(WebSocketMessageV2 msg)
        {
            TokenResponse tokenResp = (TokenResponse)msg;
            Token = tokenResp.JsonWebToken;

            DoLog(string.Format("Creating Secret for token {0}", tokenResp.JsonWebToken));
            string secret = GetSecret(TempUser, TempPassword, tokenResp.JsonWebToken); ; //Now we prepare the hash with UserId and Password (using Token received)

            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            ClientLoginRequest login = new ClientLoginRequest()
                                                                {
                                                                    Msg = "ClientLoginRequest",
                                                                    Secret = secret,
                                                                    UUID = Guid.NewGuid().ToString(),
                                                                    Time = Convert.ToInt64(elapsed.TotalMilliseconds)
                                                                };
            
            
            DoSend<ClientLoginRequest>(login);
            
            DoLog(string.Format("Secret {1} for token {0} created and sent", tokenResp.JsonWebToken, secret));
        }

        private static void ProcessEvent(WebSocketMessageV2 msg)
        {

            if (msg is ClientLoginResponse)
            {
                ClientLoginResponse loginResp = (ClientLoginResponse)msg;

                if (loginResp.JsonWebToken != null)
                {
                   
                    UUID = loginResp.UUID;
                    UserId = loginResp.UserId;
                    FirmId = 0;
                }

                ProcessJsonMessage<ClientLoginResponse>(loginResp);
            }
            else if (msg is TokenResponse)
            {
                ProcessTokenResponse(msg);
            }
            else if (msg is ClientLogout)
            {
                ClientLogout logoutResp = (ClientLogout)msg;

                Token = null;
                UserId = 0;
                FirmId = 0;
                UUID = null;

                ProcessJsonMessage<ClientLogout>((ClientLogout)msg);
            }
            else if (msg is ClientHeartbeat)
            {
                ClientHeartbeat heartBeat = (ClientHeartbeat)msg;
                ProcessJsonMessage<ClientHeartbeat>((ClientHeartbeat)msg);
                ProcessHeartbeat(heartBeat);
            }
            else if (msg is SubscriptionResponse)
                ProcessJsonMessage<SubscriptionResponse>((SubscriptionResponse)msg);
            else if (msg is ClientAccountRecord)
                ProcessJsonMessage<ClientAccountRecord>((ClientAccountRecord)msg);
            //else if (msg is DailySettlementPrice)
            //    ProcessJsonMessage<DailySettlementPrice>((DailySettlementPrice)msg);
            //else if (msg is FirmRecord)
            //    ProcessJsonMessage<FirmRecord>((FirmRecord)msg);
            //else if (msg is OfficialFixingPrice)
            //    ProcessJsonMessage<OfficialFixingPrice>((OfficialFixingPrice)msg);
            //else if (msg is RefereceRateMsg)
            //    ProcessJsonMessage<RefereceRateMsg>((RefereceRateMsg)msg);
            //else if (msg is UserRecord)
            //    ProcessJsonMessage<UserRecord>((UserRecord)msg);
            else if (msg is ClientLastSale)
                ProcessJsonMessage<ClientLastSale>((ClientLastSale)msg);
            else if (msg is ClientBestBidOffer)
                ProcessJsonMessage<ClientBestBidOffer>((ClientBestBidOffer)msg);
            //else if (msg is CreditRecordUpdate)
            //    ProcessJsonMessage<CreditRecordUpdate>((CreditRecordUpdate)msg);
            //else if (msg is DepthOfBook)
            //    ProcessJsonMessage<DepthOfBook>((DepthOfBook)msg);
            else if (msg is ClientMarketState)
                ProcessJsonMessage<ClientMarketState>((ClientMarketState)msg);
            else if (msg is ClientInstrument)
                ProcessJsonMessage<ClientInstrument>((ClientInstrument)msg);
            else if (msg is ClientOrderAck)
                ProcessJsonMessage<ClientOrderAck>((ClientOrderAck)msg);
            else if (msg is ClientOrderRej)
                ProcessJsonMessage<ClientOrderRej>((ClientOrderRej)msg);
            else if (msg is ClientOrderReq)
                ProcessJsonMessage<ClientOrderReq>((ClientOrderReq)msg);
         
         
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

        private static ClientOrderReq CreateNewOrder(string[] param)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            ClientOrderReq clientOrderReq = new ClientOrderReq()
            {
                Msg = "ClientOrderReq",
                AccountId = param.Length >= 2 && param[1].Trim() != "" ? param[1] : "VIRT_STD_ACCT1",
                cOrderType = ClientOrderReq._ORD_TYPE_LIMIT,//Limit
                cSide = ConfigurationManager.AppSettings["OrderSide"] == "B" ? ClientOrderReq._SIDE_BUY : ClientOrderReq._SIDE_SELL,//Buy or sell
                cTimeInForce = ClientOrderReq._TIF_DAY,
                ClientOrderId= Guid.NewGuid().ToString(),
                FirmId = 0,
                InstrumentId = Convert.ToInt32(ConfigurationManager.AppSettings["OrderInstrumentId"]),
                //JsonWebToken = Token,
                Price = Convert.ToDecimal(ConfigurationManager.AppSettings["OrderPrice"]),
                Quantity = Convert.ToDecimal(ConfigurationManager.AppSettings["OrderSize"]),
                //SendingTime = Convert.ToInt64(elapsed.TotalMilliseconds),
                UserId = UserId.ToString()
            };

            return clientOrderReq;
        }


        private static void ProcessCancelLastCreatedOrderparam(string[] param)
        {
            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            if (LastOrderCreated != null)
            {
                ClientOrderCancelReq cxlReq = new ClientOrderCancelReq()
                {
                    UUID = UUID,
                    CancelReason = "Cancelled from test client",
                    ClientOrderId = LastOrderCreated.ClientOrderId,
                    FirmId = LastOrderCreated.FirmId,
                    Msg = "ClientOrderCancelReq",
                    OrderId = 0,
                    UserId = UserId,
                };

                DoSend<ClientOrderCancelReq>(cxlReq);
            }
            else
                DoLog(string.Format("Last Order Created Missing"));

        }

        private static void ProcessRouteOrder(string[] param)
        {
            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            if (param.Length >= 1)
            {
                ClientOrderReq clientOrderReq = CreateNewOrder(param);

                LastOrderCreated = clientOrderReq;

                string strMsg = JsonConvert.SerializeObject(clientOrderReq, Newtonsoft.Json.Formatting.None,
                                                 new JsonSerializerSettings
                                                 {
                                                     NullValueHandling = NullValueHandling.Ignore
                                                 });

                DoSend(strMsg);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for ClientOrderReq message"));

        }

        private static void ProcessCreditLimitUpdateRequest(string[] param)
        {
            if (param.Length == 6)
            {
                FirmsCreditLimitUpdateRequest firmsCreditLimitUpdReq = new FirmsCreditLimitUpdateRequest()
                {
                    Msg = "FirmsCreditLimitUpdateRequest",
                    CreditLimitTotal = Convert.ToDouble(param[4]),
                    CreditLimitUsage = Convert.ToDouble(param[3]),
                    CreditLimitBalance = Convert.ToDouble(param[4]) - Convert.ToDouble(param[3]),
                    CreditLimitMaxTradeSize = Convert.ToDecimal(param[5]),
                    cTradingStatus = Convert.ToChar(param[2]),
                    FirmId = Convert.ToInt64(param[1]),
                    JsonWebToken = Token,
                    UUID = UUID
                };

                FirmId = firmsCreditLimitUpdReq.FirmId;

                DoSend<FirmsCreditLimitUpdateRequest>(firmsCreditLimitUpdReq);
            
            }
            else
                DoLog(string.Format("Missing mandatory parameters for FirmsCreditLimitUpdateRequest message"));
        
        }

        private static void ProcessFirmListRequest(string[] param)
        { 
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            FirmsListRequest req = new FirmsListRequest()
            {
                JsonWebToken = Token,
                Msg = "FirmsListRequest",
                PageNo = 0,
                PageRecords = 100,
                Time = Convert.ToInt64(elapsed.TotalMilliseconds),
                UUID = UUID

            };
            DoSend<FirmsListRequest>(req);
        }

        private static void ProcessLoginClient(string[] param)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            if (param.Length == 3)
            {
                TempUser = param[1];
                TempPassword = param[2];

                TokenRequest tokenReq = new TokenRequest()
                {
                    Msg = "TokenRequest",
                    SourceIP = GetMyIpAddress(),
                    UUID = Guid.NewGuid().ToString(),
                    Time = Convert.ToInt64(elapsed.TotalMilliseconds)
                };

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

            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            ClientHeartbeat heartbeatResp = new ClientHeartbeat()
            {
                Msg = "ClientHeartbeat",
                JsonWebToken = heartBeat.JsonWebToken,
                UUID = heartBeat.UUID,
                Time = Convert.ToInt64(elapsed.TotalMilliseconds)
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
                ClientLogoutRequest logoutReq = new ClientLogoutRequest()
                {
                    Msg = "ClientLogoutRequest",
                    JsonWebToken = Token,
                    UserId = UserId.ToString(),
                    UUID = UUID,
                };

                DoSend<ClientLogoutRequest>(logoutReq);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for logout message"));

        }


        #endregion

        static void Main(string[] args)
        {
            try
            {
                DecryptTest();
                GetSecret("MM1_BLOCK", "Testing123", "2b6e7e75-b70e-4944-bb8e-09d07ae18c30");
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
