using DGTLBackendMock.Common.DTO.Auth;
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
            Console.WriteLine("-CLEAR");
            Console.WriteLine();
        
        }

        private static void ProcessEvent(WebSocketMessage msg)
        {
            if (msg is ClientLoginResponse)
            {
                ClientLoginResponse loginResp = (ClientLoginResponse)msg;

                string loginJson = JsonConvert.SerializeObject(loginResp, Newtonsoft.Json.Formatting.None,
                                                  new JsonSerializerSettings
                                                  {
                                                      NullValueHandling = NullValueHandling.Ignore
                                                  });
                DoLog(string.Format(">>{0}", loginJson));

            }
            else if (msg is ClientReject)
            {
                ClientReject loginReject = (ClientReject)msg;
                string rejectJson = JsonConvert.SerializeObject(loginReject, Newtonsoft.Json.Formatting.None,
                                                  new JsonSerializerSettings
                                                  {
                                                      NullValueHandling = NullValueHandling.Ignore
                                                  });
                DoLog(string.Format(">>{0}", rejectJson));
                
            }
            else
                DoLog(string.Format(">>Unknown message type {0}", msg.ToString()));

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

                DGTLWebSocketClient.Send(strMsg);
            }
            else
                DoLog(string.Format("Missing mandatory parameters for LoginClient message"));
        
        }

        private static void ProcessCommand(string cmd)
        {

            string[] param = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            string mainCmd = param[0];

            if (mainCmd == "LoginClient")
            {
                ProcessLoginClient(param);
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
