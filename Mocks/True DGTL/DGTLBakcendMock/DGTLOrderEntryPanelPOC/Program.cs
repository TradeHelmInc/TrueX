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

namespace DGTLOrderEntryPanelPOC
{
    class Program
    {
        #region Protected Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static ClientLoginResponse ClientLoginResponse { get; set; }

        protected static List<AccountRecord> AccountRecords { get; set; }

        protected static CreditRecordUpdate CreditRecordUpdate { get; set; }

        #endregion

        #region Private Static Methods

        private static bool AccountsReceived { get; set; }

        private static bool CreditUsageReceived { get; set; }

        private static void DoLog(string message)
        {
            Console.WriteLine(message);
        }

        private static void DoSend(string strMsg)
        {
            DoLog(string.Format(">>{0}", strMsg));
            DGTLWebSocketClient.Send(strMsg);
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
            DoSubscribe("TB", ClientLoginResponse.UserId);
        }

        public static void SubscribeAccountRecord(UserRecord userRecord)
        {
            DoSubscribe("TD", userRecord.FirmId + "@*");
        }


        public static void SubscribeCreditRecordUpdate(UserRecord userRecord)
        {
            DoSubscribe("CU", userRecord.FirmId + "@*");
        }

        public static void ShowUserRecord(UserRecord userRecord)
        {
            DoLog(string.Format("Requesting AccountRecords for FirmId {0}", userRecord.FirmId));
        }

        #region UI Methods

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

        public static void ShowCreditUsageBarThread(object param)
        {

            while (!AccountsReceived && ! CreditUsageReceived)
                Thread.Sleep(1000);

            //We will use the first account in the combo as the Credit Limit just for the example
            //Every time we change the combo selection, we will have to calculate this credit usage ratio again
            double creditLimit = 0;
            if (AccountRecords.Count > 0)
                creditLimit = AccountRecords[0].CreditLimit;


            DoLog("");
            DoLog("================ 2)Showing CreditUsageBar ================");
            if (creditLimit > 0 && CreditRecordUpdate != null)
            {
                double ratio = (CreditRecordUpdate.CreditUsed / creditLimit) * 100 ;
                DoLog(string.Format("{0}% ({1}/{2})", ratio.ToString("0.##"), CreditRecordUpdate.CreditUsed, creditLimit));
            }
            else if (creditLimit <= 0)
                DoLog(string.Format("Invalid value for Credit Limit by Firm: {0}", creditLimit));
            else if (CreditRecordUpdate == null)
            { 
                //we use 0 as a reference
                DoLog(string.Format("0% (0/{0})", creditLimit));
            }
           
            DoLog("");
        
        }

        public static void WaitForCreditUsageBar()
        {

            Thread showCreditUsageBarThread = new Thread(ShowCreditUsageBarThread);
            showCreditUsageBarThread.Start();
        }

        #endregion

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
                //3.1-We get the UserRecord for the logged user. The main field that we are interested here is the FirmId
                //we have to get all the accounts for that FirmId, and we will use those accounts to fill the Accounts Combo
                UserRecord userRecord = (UserRecord)msg;
                ShowUserRecord(userRecord);
                SubscribeAccountRecord(userRecord);

                //3.2-We also want to know how much of credit it's used (CREDIT USAGE). 
                // The credit LIMIT is calculated at a firm level and account level
                // In the screen we will show the credit USAGE at the FIRM level.
                // So we will request a Credit Limit for the Firm and based on the selected account (Account Credit Limit)
                //   we will calculate the CREDIT USAGE.
                // So every time we change the account, we will have to calculate a new CREDIT USAGE <USAGE = CreditUsed <FirmLevel> / CreditLimit <AccountLevel>>
                //For mor info about ow to calculate the Credit Usage bar, see specs Order entry panel specs v1.x
                SubscribeCreditRecordUpdate(userRecord);
                WaitForCreditUsageBar();
                
            }
            else if (msg is AccountRecord)
            {
                //4.1- After subscribing for account record, I will start getting all the accounts
                //I will have to save those accounts in a Collection until the SubscriptionResponse message arrives (or the timout mechanism is activated)
                AccountRecord accRecord = (AccountRecord)msg;

                AccountRecords.Add(accRecord);
            }
            else if (msg is CreditRecordUpdate)
            {
                //5.1- After subscribing for credit record updates, I will start getting all the credit records
                //I will have to save those recordsuntil the SubscriptionResponse message arrives (or the timout mechanism is activated)
                CreditRecordUpdate = (CreditRecordUpdate)msg;
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
                    AccountsReceived = true;//4.2 Once we receive all the accounts , we mark the accounts as received
                }
                else if (subscrResp.Service == "CU") 
                {
                    CreditUsageReceived = true;//5.2 Once we receive the credit usage for the Firm, we mark the credit usage as received
                }
                //4.2-5.2 --> Once all the accounts and the credit usage is received, we are ready to show the Credit Usage progress bar <see WaitForCreditUsageBar()>
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
