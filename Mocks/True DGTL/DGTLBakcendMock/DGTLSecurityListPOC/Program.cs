using DGTLBackendMock.BusinessEntities;
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
using System.Threading;
using System.Threading.Tasks;

namespace DGTLSecurityListPOC
{
    class Program
    {

        #region Protected Static Attributes

        protected static DGTLWebSocketClient DGTLWebSocketClient { get; set; }

        protected static ClientLoginResponse ClientLoginResponse { get; set; }

        protected static List<SecurityMasterRecord> SecurityMasterRecords { get; set; }

        protected static DateTime SecurityMasterRecordRequestStartTime { get; set; }

        protected static int _SECURITY_MASTER_RECORD_TIMOUT_IN_SECONDS = 1;        

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

        private static void LoginClient(string userId,string UUID, string password)
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

        private static void RequestSecurityMasterList()
        {
            WebSocketSubscribeMessage subscribe = new WebSocketSubscribeMessage()
            {
                Msg = "Subscribe",
                Sender = 0,
                UserId = ClientLoginResponse.UserId,
                SubscriptionType = "S",
                JsonWebToken = ClientLoginResponse.JsonWebToken,
                Service = "TA",
                ServiceKey = "*"
            };

            string strMsg = JsonConvert.SerializeObject(subscribe, Newtonsoft.Json.Formatting.None,
                                             new JsonSerializerSettings
                                             {
                                                 NullValueHandling = NullValueHandling.Ignore
                                             });

            DoSend(strMsg);
        }

        private static void ShowProductCombo(List<SecurityType> securityTypes)
        {
            DoLog("================== SHOWING Product Combo ==================");
            foreach (SecurityType secType in securityTypes)
                DoLog(string.Format("-Code:{0} Description:{1}", secType.Code, secType.Description));

            DoLog(" ");
        }

        private static void ShowPairCombo(List<Security> securities)
        {
            DoLog("================== SHOWING Pair Combo ==================");
            foreach (Security sec in securities)
                DoLog(string.Format("-Symbol:{0} Description:{1}", sec.Symbol, sec.Description));

            DoLog(" ");
        }

        private static void ShowSecurityList(List<Security> securities)
        {
            DoLog("================== SHOWING Seleted Securities for Product and Pair==================");
            foreach (Security security in securities)
                DoLog(string.Format("-Symbol:{0} Description:{1}", security.Symbol, security.Description));

            DoLog(" ");
        }

        private static void ProcessProductCombo()
        {
            //As I suggest, what they have in the Product combo, is actually what is called SecurityType in FIX, 
            //so We should extract "security types" from the security list master record. Not a Product List
            List<SecurityType> securityTypes = new List<SecurityType>();

            //5.1.1 - This is the algo to extract ALL the security types in the security master record
            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {

                if (!securityTypes.Any(x => x.Code == security.AssetClass))
                {
                    securityTypes.Add(new SecurityType() { Code = security.AssetClass, Description = security.AssetClass });
                }
            
            }

            //5.1.2 - Just because it can be seen in the image, put the SWP secType as the default <unless there is no SWP
            // in which case, just put the next one available
            ShowProductCombo(securityTypes);
        
        }

        private static void ProcessPairCombo()
        {
            //As I suggest, what they have in the Pair combo, is actually what is called the undelying Security in FIX, 
            //so We should extract "Securities" from the security list master record. Not a Pair List
            List<Security> securities = new List<Security>();

            //5.1.2 - This is the algo to extract ALL the securities (underlying) in the security master record
            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {

                if (!securities.Any(x => x.Symbol == "XBT-USD"))
                {
                    //here, the Id field, is not called Code. It's usually called Symbol
                    securities.Add(new Security() { Symbol = "XBT-USD", Description = "XBT-USD" });
                }

            }

            //5.2- Just because it can be seen in the image, put the SWP secType as the default <unless there is no SWP
            // in which case, just put the next one available
            ShowPairCombo(securities);
        
        }

        private static void ProcessSecurityList(string secType, string symbol)
        {
            //5.2.1 - As the derivative (contract) traded is also a security, we will show a security list like we did in ProcessPairCombo method
            List<Security> securities = new List<Security>();

            //5.2.2 - Still missing to filter the underlying as we have no field for it!!!
            foreach (SecurityMasterRecord secMasterRecord in SecurityMasterRecords.Where(x=>x.AssetClass==secType))
            {
                securities.Add(new Security() { Symbol = secMasterRecord.Symbol, Description = secMasterRecord.Description });
                
            }

            //5.2.3 - Showing all the derivatives (contracts) available for secType (prodcut) and symbol (pair) selected
            ShowSecurityList(securities);

        
        }

        private static void ProcessSecurityMasterRecordThread()
        {
            while (true)
            {
                TimeSpan elapsed = DateTime.Now - SecurityMasterRecordRequestStartTime;
                if (elapsed.TotalSeconds > _SECURITY_MASTER_RECORD_TIMOUT_IN_SECONDS)
                {
                    //5-Now we have all the securities We can process them
                    //5.1 We process the product combo
                    ProcessProductCombo();

                    //5.2 We process the pair combo
                    ProcessPairCombo();

                    //5.3 We process the security list. Swaps (SWP) for XBT-USD symbol 
                    ProcessSecurityList("SWP","XBT-USD");

                    break;
                }
                Thread.Sleep(10);
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

                DoLog(string.Format("Client successfully logged with token {0}", loginResp.JsonWebToken));
                //3- Once Logged we request the security master record. We set the request timestamp for timeout calculation
                SecurityMasterRecordRequestStartTime = DateTime.Now;
                RequestSecurityMasterList();

                //3.1- We launch the thread that will process all the securities once everything is available
                Thread processSecurityMasterRecordThread=new Thread(ProcessSecurityMasterRecordThread);
                processSecurityMasterRecordThread.Start();
            }
            else if (msg is SecurityMasterRecord)
            {
                SecurityMasterRecord security = (SecurityMasterRecord)msg;
                //4-Every time we get a security, if the arrival time is less than timeout time, we update the list that hold
                //all the securities
                TimeSpan elapsed = DateTime.Now - SecurityMasterRecordRequestStartTime;
                if (elapsed.TotalSeconds < _SECURITY_MASTER_RECORD_TIMOUT_IN_SECONDS)
                    SecurityMasterRecords.Add(security);
                else
                { 
                    //4.1- Here the security arrive after the timeout. We have to set some warning in the logs to check 
                    //     if we have to recalibrate the timeout threshold
                    DoLog(string.Format("TC1-Security Master Record arrived after timeout expiration!:{0}",security.Symbol));
                }
            }
        }


        #endregion

        static  void Main(string[] args)
        {
            string WebSocketURL = ConfigurationManager.AppSettings["WebSocketURL"];
            string Sender = ConfigurationManager.AppSettings["Sender"];
            string UUID = ConfigurationManager.AppSettings["UUID"];
            string UserId = ConfigurationManager.AppSettings["UserId"];
            string Password = ConfigurationManager.AppSettings["Password"];
            SecurityMasterRecords = new List<SecurityMasterRecord>();

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
