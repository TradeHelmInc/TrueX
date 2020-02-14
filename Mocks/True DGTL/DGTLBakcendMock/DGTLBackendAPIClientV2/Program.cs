using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Account.V2.Credit_UI;
using DGTLBackendMock.Common.DTO.Auth.V2;
using DGTLBackendMock.Common.DTO.Auth.V2.Credit_UI;
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
using System.Threading;
using System.Threading.Tasks;

namespace DGTLBackendAPIClientV2
{
    class Program
    {
        #region Protected Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static Dictionary<string, string> SettlementAgentDict { get; set; }

        protected static string Token { get; set; }

        protected static string UUID { get; set; }

        protected static string UserId { get; set; }

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
            //Console.WriteLine("FirmsTradingStatusUpdateRequest <FirmId> <Status>");
            Console.WriteLine("CreditLimitUpdateRequest <FirmId> <Status> <Available> <Used> <MaxNotional>");
            Console.WriteLine("EmailNotificationsListRequest <SettlementFirmId>");
            Console.WriteLine("EmailNotificationsCreateRequest <SettlementFirmId> <email>");
            Console.WriteLine("EmailNotificationsUpdateRequest <SettlementFirmId> <old_email> <new_email>");
            Console.WriteLine("EmailNotificationsDeleteRequest <SettlementFirmId> <email>");
            Console.WriteLine("ForgotPasswordRequest");
            Console.WriteLine("ResetPasswordRequest <oldPwd> <NewPwd>");
            Console.WriteLine("RouteOrder <AccountId> (Harcoded..)");
            Console.WriteLine("RouteOrderBulk <Count>");
            Console.WriteLine("CancelOrder <OrderId>");
            Console.WriteLine("MassCancelRequest");
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
            try
            {
                DoLog(string.Format(">>{0}", strMsg));
                DGTLWebSocketClient.Send(strMsg,DoLog);
            }
            catch (Exception ex)
            {
                DoLog(ex.Message);
            }
        }

        private static void DoSend<T>(T obj)
        {
            try
            {
                string strMsg = JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None,
                                                        new JsonSerializerSettings
                                                        {
                                                            NullValueHandling = NullValueHandling.Ignore
                                                        });


                DoSend(strMsg);
            }
            catch (Exception ex)
            {

                DoLog(ex.Message);            
            }
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
                    Uuid = UUID,
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
            else if (mainCmd == "EmailNotificationsListRequest")
            {
                ProcessEmailNotificationsListRequest(param);
            }
            else if (mainCmd == "EmailNotificationsCreateRequest")
            {
                ProcessEmailNotificationsCreateRequest(param);
            }
            else if (mainCmd == "EmailNotificationsUpdateRequest")
            {
                ProcessEmailNotificationsUpdateRequest(param);
            }
            else if (mainCmd == "EmailNotificationsDeleteRequest")
            {
                ProcessEmailNotificationsDeleteRequest(param);
            }
            else if (mainCmd == "CreditLimitUpdateRequest")
            {
                ProcessCreditLimitUpdateRequest(param);
            }
            else if (mainCmd == "FirmsTradingStatusUpdateRequest")
            {
                ProcessFirmsTradingStatusUpdateRequest(param);
            }
            else if (mainCmd == "LogoutClient")
            {
                ProcessLogoutClient(param);
            }
            else if (mainCmd == "RouteOrder")
            {
                ProcessRouteOrder(param);
            }
            else if (mainCmd == "RouteOrderBulk")
            {
                ProcessRouteOrderBulk(param);
            }
            else if (mainCmd == "CancelOrder")
            {
                ProcessCancelOrder(param);
            }
            else if (mainCmd == "ForgotPasswordRequest")
            {
                ProcessForgotPasswordRequest(param);
            }
            else if (mainCmd == "ResetPasswordRequest")
            {
                ProcessResetPasswordRequest(param);
            }
            else if (mainCmd == "MassCancelRequest")
            {
                ProcessMassCancelRequest(param);
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
            Token = tokenResp.Token;

            DoLog(string.Format("Creating Secret for token {0}", tokenResp.Token));
            string secret = GetSecret(TempUser, TempPassword, tokenResp.Token); ; //Now we prepare the hash with UserId and Password (using Token received)

            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            ClientLoginRequest login = new ClientLoginRequest()
                                                                {
                                                                    Msg = "ClientLoginRequest",
                                                                    Secret = secret,
                                                                    Uuid = Guid.NewGuid().ToString(),
                                                                    Time = Convert.ToInt64(elapsed.TotalMilliseconds)
                                                                };
            
            
            DoSend<ClientLoginRequest>(login);

            DoLog(string.Format("Secret {1} for token {0} created and sent", tokenResp.Token, secret));
        }

        private static void ProcessEvent(WebSocketMessageV2 msg)
        {

            if (msg is ClientLoginResponse)
            {
                ClientLoginResponse loginResp = (ClientLoginResponse)msg;

                if (loginResp.JsonWebToken != null)
                {
                   
                    UUID = loginResp.Uuid;
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
                UserId = UserId;
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
            else if (msg is ClientLastSale)
                ProcessJsonMessage<ClientLastSale>((ClientLastSale)msg);
            else if (msg is FirmsTradingStatusUpdateResponse)
                ProcessJsonMessage<FirmsTradingStatusUpdateResponse>((FirmsTradingStatusUpdateResponse)msg);
            else if (msg is ClientBestBidOffer)
                ProcessJsonMessage<ClientBestBidOffer>((ClientBestBidOffer)msg);
            else if (msg is ClientMarketState)
                ProcessJsonMessage<ClientMarketState>((ClientMarketState)msg);
            else if (msg is ClientInstrument)
                ProcessJsonMessage<ClientInstrument>((ClientInstrument)msg);
            else if (msg is ClientOrderAck)
                ProcessJsonMessage<ClientOrderAck>((ClientOrderAck)msg);
            else if (msg is ClientOrderRej)
                ProcessJsonMessage<ClientOrderRej>((ClientOrderRej)msg);
            else if (msg is ClientMassCancelResponse)
                ProcessJsonMessage<ClientMassCancelResponse>((ClientMassCancelResponse)msg);
            else if (msg is ClientOrderReq)
                ProcessJsonMessage<ClientOrderReq>((ClientOrderReq)msg);
            else if (msg is ClientDSP)
                ProcessJsonMessage<ClientDSP>((ClientDSP)msg);
            else if (msg is FirmsListResponse)
            {
                FirmsListResponse resp = (FirmsListResponse)msg;
                resp.Firms.ToList().ForEach(x => SettlementAgentDict.Add(x.FirmId.ToString(), resp.SettlementAgentId));
                ProcessJsonMessage<FirmsListResponse>(resp);
            }
            else if (msg is FirmsCreditLimitUpdateResponse)
                ProcessJsonMessage<FirmsCreditLimitUpdateResponse>((FirmsCreditLimitUpdateResponse)msg);
            else if (msg is FirmsTradingStatusUpdateResponse)
                ProcessJsonMessage<FirmsTradingStatusUpdateResponse>((FirmsTradingStatusUpdateResponse)msg);
            else if (msg is EmailNotificationsListResponse)
                ProcessJsonMessage<EmailNotificationsListResponse>((EmailNotificationsListResponse)msg);
            else if (msg is EmailNotificationsCreateResponse)
                ProcessJsonMessage<EmailNotificationsCreateResponse>((EmailNotificationsCreateResponse)msg);
            else if (msg is EmailNotificationsUpdateResponse)
                ProcessJsonMessage<EmailNotificationsUpdateResponse>((EmailNotificationsUpdateResponse)msg);
            else if (msg is EmailNotificationsDeleteResponse)
                ProcessJsonMessage<EmailNotificationsDeleteResponse>((EmailNotificationsDeleteResponse)msg);
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
                InstrumentId = ConfigurationManager.AppSettings["OrderInstrumentId"],
                //JsonWebToken = Token,
                Price = Convert.ToDecimal(ConfigurationManager.AppSettings["OrderPrice"]),
                Quantity = Convert.ToDecimal(ConfigurationManager.AppSettings["OrderSize"]),
                //SendingTime = Convert.ToInt64(elapsed.TotalMilliseconds),
                UserId = UserId.ToString()
            };

            return clientOrderReq;
        }


        private static void ProcessMassCancelRequest(string[] param)
        {

            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            ClientMassCancelReq cxlMassReq = new ClientMassCancelReq()
            {
                Uuid = UUID,
                Msg = "ClientMassCancelReq",
                UserId = UserId,
                JsonWebToken=Token
            };

            DoSend<ClientMassCancelReq>(cxlMassReq);
        }

        private static void ProcessResetPasswordRequest(string[] param)
        {
            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            if (param.Length >= 3)
            {
                ResetPasswordRequest resetPwdReq = new ResetPasswordRequest()
                {
                    MessageName = "ResetPasswordRequest",
                    Uuid = UUID,
                    TempSecret = GetSecret(TempUser, param[1], Token),
                    NewSecret = GetSecret(TempUser, param[2], Token),
                };

                DoSend<ResetPasswordRequest>(resetPwdReq);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for ProcessResetPasswordRequest message"));
        
        }

        private static void ProcessForgotPasswordRequest(string[] param)
        {
            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            ForgotPasswordRequest forgotReq = new ForgotPasswordRequest()
            {
                MessageName = "ForgotPasswordRequest",
                TokenId = Token,
                UserId = UserId,
                Uuid = UUID,
                Email = "test@gmail.com"
            };

            DoSend<ForgotPasswordRequest>(forgotReq);
        
        
        }

        private static void ProcessCancelOrder(string[] param)
        {
            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            ClientOrderCancelReq cxlReq = new ClientOrderCancelReq()
            {
                Uuid = UUID,
                CancelReason = "Cancelled from test client",
                //ClientOrderId = LastOrderCreated.ClientOrderId,
                FirmId = LastOrderCreated.FirmId,
                Msg = "ClientOrderCancelReq",
                OrderId = param[1],
                UserId = UserId,
            };

            DoSend<ClientOrderCancelReq>(cxlReq);

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

        private static void ProcessRouteOrderBulk(string[] param)
        {
            if (Token == null)
            {
                DoLog("Missing authentication token in memory!. User not logged");
                return;
            }

            if (param.Length == 2)
            {

                int count = Convert.ToInt32(param[1]);

                for (int i = 0; i < count; i++)
                {

                    ClientOrderReq clientOrderReq = CreateNewOrder(param);

                    LastOrderCreated = clientOrderReq;

                    string strMsg = JsonConvert.SerializeObject(clientOrderReq, Newtonsoft.Json.Formatting.None,
                                                     new JsonSerializerSettings
                                                     {
                                                         NullValueHandling = NullValueHandling.Ignore
                                                     });

                    DoSend(strMsg);
                    Thread.Sleep(100);
                }
            }
            else
                DoLog(string.Format("Missing mandatory parameters for ClientOrderReq message"));

        }

        private static void ProcessFirmsTradingStatusUpdateRequest(string[] param)
        {
            if (param.Length == 3)
            {
                FirmsTradingStatusUpdateRequest firmsCreditLimitUpdReq = new FirmsTradingStatusUpdateRequest()
                {
                    Msg = "FirmsTradingStatusUpdateRequest",
                    Time = 0,
                    cTradingStatus = Convert.ToChar(param[2]),
                    FirmId = Convert.ToInt64(param[1]),
                    JsonWebToken = Token,
                    Uuid = UUID
                };

                FirmId = firmsCreditLimitUpdReq.FirmId;

                DoSend<FirmsTradingStatusUpdateRequest>(firmsCreditLimitUpdReq);

            }
            else
                DoLog(string.Format("Missing mandatory parameters for FirmsTradingStatusUpdateRequest message"));
        }

        private static void ProcessCreditLimitUpdateRequest(string[] param)
        {
            if (param.Length == 6)
            {
                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                FirmsCreditLimitUpdateRequest firmsCreditLimitUpdReq = new FirmsCreditLimitUpdateRequest()
                {
                    Msg = "FirmsCreditLimitUpdateRequest",
                    AvailableCredit = Convert.ToDouble(param[3]),
                    UsedCredit = Convert.ToDouble(param[4]),
                    PotentialExposure = 0,
                    MaxNotional = Convert.ToDouble(param[5]),
                    MaxQuantity = Convert.ToDouble(param[5]) / 7000,//We use BTC price as reference
                    cTradingStatus = Convert.ToChar(param[2]),
                    FirmId = Convert.ToInt64(param[1]),
                    Time = Convert.ToInt64(elapsed.TotalSeconds),
                    SettlementAgentId = SettlementAgentDict.ContainsKey(param[1]) ? SettlementAgentDict[param[1]] : null,
                    JsonWebToken = Token,
                    Uuid = UUID
                };

                FirmId = firmsCreditLimitUpdReq.FirmId;

                DoSend<FirmsCreditLimitUpdateRequest>(firmsCreditLimitUpdReq);
            
            }
            else
                DoLog(string.Format("Missing mandatory parameters for FirmsCreditLimitUpdateRequest message"));
        
        }

        private static void ProcessEmailNotificationsDeleteRequest(string[] param)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            if (param.Length == 3)
            {
                EmailNotificationsDeleteRequest req = new EmailNotificationsDeleteRequest()
                {
                    JsonWebToken = Token,
                    Msg = "EmailNotificationsDeleteRequest",
                    SettlementFirmId = param[1],
                    Email = param[2],
                    Time = Convert.ToInt64(elapsed.TotalMilliseconds),
                    Uuid = UUID

                };
                DoSend<EmailNotificationsDeleteRequest>(req);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for EmailNotificationsDeleteRequest message"));
        
        }

        private static void ProcessEmailNotificationsUpdateRequest(string[] param)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            if (param.Length == 4)
            {
                EmailNotificationsUpdateRequest req = new EmailNotificationsUpdateRequest()
                {
                    JsonWebToken = Token,
                    Msg = "EmailNotificationsUpdateRequest",
                    SettlementFirmId = param[1],
                    EmailCurrent = param[2],
                    EmailNew = param[3],
                    Time = Convert.ToInt64(elapsed.TotalMilliseconds),
                    Uuid = UUID

                };
                DoSend<EmailNotificationsUpdateRequest>(req);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for EmailNotificationsUpdateRequest message"));
        }

        private static void ProcessEmailNotificationsCreateRequest(string[] param)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            if (param.Length == 3)
            {
                EmailNotificationsCreateRequest req = new EmailNotificationsCreateRequest()
                {
                    JsonWebToken = Token,
                    Msg = "EmailNotificationsCreateRequest",
                    SettlementFirmId = param[1],
                    Email = param[2],
                    Time = Convert.ToInt64(elapsed.TotalMilliseconds),
                    Uuid = UUID

                };
                DoSend<EmailNotificationsCreateRequest>(req);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for EmailNotificationsCreateRequest message"));
        }

        private static void ProcessEmailNotificationsListRequest(string[] param)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            if (param.Length == 2)
            {
                EmailNotificationsListRequest req = new EmailNotificationsListRequest()
                {
                    JsonWebToken = Token,
                    Msg = "EmailNotificationsListRequest",
                    SettlementFirmId = param[1],
                    Time = Convert.ToInt64(elapsed.TotalMilliseconds),
                    Uuid = UUID

                };
                DoSend<EmailNotificationsListRequest>(req);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for EmailNotificationsListRequest message"));
        
        }

        private static void ProcessFirmListRequest(string[] param)
        { 
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            FirmsListRequest req = new FirmsListRequest()
            {
                JsonWebToken = Token,
                Msg = "FirmsListRequest",
                PageNo = 0,
                PageRecords = 1000,
                Time = Convert.ToInt64(elapsed.TotalMilliseconds),
                Uuid = UUID
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
                    Uuid = Guid.NewGuid().ToString(),
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
                Uuid = heartBeat.Uuid,
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
                    Uuid = UUID,
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
                //DecryptTest();
                //GetSecret("MM1_BLOCK", "Testing123", "2b6e7e75-b70e-4944-bb8e-09d07ae18c30");

                SettlementAgentDict = new Dictionary<string, string>();
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
