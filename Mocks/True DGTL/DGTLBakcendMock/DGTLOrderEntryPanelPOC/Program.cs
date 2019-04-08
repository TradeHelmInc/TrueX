using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth;
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

namespace DGTLOrderEntryPanelPOC
{
    class Program
    {
        #region Protected Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static ClientLoginResponse ClientLoginResponse { get; set; }

        protected static List<AccountRecord> AccountRecords { get; set; }

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

        public static void SubscribeUserRecord(ClientLoginResponse loginResp)
        {

            WebSocketSubscribeMessage subscribe = new WebSocketSubscribeMessage()
            {
                Msg = "Subscribe",
                Sender = 0,
                UserId = ClientLoginResponse.UserId,
                SubscriptionType = "S",
                JsonWebToken = ClientLoginResponse.JsonWebToken,
                Service = "TB",
                ServiceKey = ClientLoginResponse.UserId
            };

            string strMsg = JsonConvert.SerializeObject(subscribe, Newtonsoft.Json.Formatting.None,
                                             new JsonSerializerSettings
                                             {
                                                 NullValueHandling = NullValueHandling.Ignore
                                             });

            DoSend(strMsg);
        
        
        }

        public static void SubscribeAccountRecord(UserRecord userRecord)
        {

            WebSocketSubscribeMessage subscribe = new WebSocketSubscribeMessage()
            {
                Msg = "Subscribe",
                Sender = 0,
                UserId = ClientLoginResponse.UserId,
                SubscriptionType = "S",
                JsonWebToken = ClientLoginResponse.JsonWebToken,
                Service = "TD",
                ServiceKey = userRecord.FirmId + "@*"
            };

            string strMsg = JsonConvert.SerializeObject(subscribe, Newtonsoft.Json.Formatting.None,
                                             new JsonSerializerSettings
                                             {
                                                 NullValueHandling = NullValueHandling.Ignore
                                             });

            DoSend(strMsg);


        }

        public static void ShowUserRecord(UserRecord userRecord)
        {
            DoLog(string.Format("Requesting AccountRecords for FirmId {0}", userRecord.FirmId));
        }

        public static void ShowAccountRecords()
        {
            DoLog("");
            DoLog("================ 1)Showing Account Records Combo ================");
            foreach (AccountRecord accRecord in AccountRecords)
            {
                DoLog(string.Format("{0}-{1}", accRecord.UniqueId, accRecord.EPNickName));
            }
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
                SubscribeUserRecord(loginResp);
          
            }
            else if (msg is UserRecord) 
            {
                UserRecord userRecord = (UserRecord)msg;
                ShowUserRecord(userRecord);
                SubscribeAccountRecord(userRecord);
            }
            else if (msg is AccountRecord)
            {
                AccountRecord accRecord = (AccountRecord)msg;

                AccountRecords.Add(accRecord);
            
            }
            else if (msg is SubscriptionResponse)
            {
                SubscriptionResponse subscrResp = (SubscriptionResponse)msg;

                if (subscrResp.Service == "TB")
                {
                   
                
                }
                else if (subscrResp.Service == "TD")
                {
                    ShowAccountRecords();
                }
            }
        }

        #endregion

        static void Main(string[] args)
        {
            string WebSocketURL = ConfigurationManager.AppSettings["WebSocketURL"];
            string Sender = ConfigurationManager.AppSettings["Sender"];
            string UUID = ConfigurationManager.AppSettings["UUID"];
            string UserId = ConfigurationManager.AppSettings["UserId"];
            string Password = ConfigurationManager.AppSettings["Password"];
            AccountRecords = new List<AccountRecord>();
           

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
