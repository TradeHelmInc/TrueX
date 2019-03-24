using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DGTLBackendMockClient
{
    class Program
    {
        #region Protected Attributes

        protected static ClientWebSocket SubscriptionWebSocket { get; set; }

        #endregion

        #region Private Static Methods

        //This should be logged, but we will write it on the screen for simplicity
        private static void DoLog(string message)
        {
            Console.WriteLine(message);
        }

        #endregion

        public static async void ConnectWebSocket(string WebSocketURL)
        {
            SubscriptionWebSocket = new ClientWebSocket();
            await SubscriptionWebSocket.ConnectAsync(new Uri(WebSocketURL), CancellationToken.None);
         
        }

        protected static async void DoLogin()
        {
            int Sender = Convert.ToInt32(ConfigurationManager.AppSettings["Sender"]);
            string UUID = ConfigurationManager.AppSettings["UUID"];
            string UserId = ConfigurationManager.AppSettings["UserId"];
            string Password = ConfigurationManager.AppSettings["Password"];

            WebSocketLoginMessage login = new WebSocketLoginMessage()
            {
                Msg = "ClientLogin",
                Sender = Sender,
                UUID = UUID,
                UserId = UserId,
                Password = Password
            };

            DoLog(string.Format("Logging user {0}", UserId));
            
            string strMsg = JsonConvert.SerializeObject(login, Newtonsoft.Json.Formatting.None,
                                                        new JsonSerializerSettings
                                                        {
                                                            NullValueHandling = NullValueHandling.Ignore
                                                        });

            DoLog(string.Format("Sending: {0}",strMsg));


            byte[] msgArray = Encoding.ASCII.GetBytes(strMsg);

            ArraySegment<byte> bytesToSend = new ArraySegment<byte>(msgArray);

            await SubscriptionWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                                                          CancellationToken.None);
        
        }

        public static async void ClientMessageThread(object param)
        {
            while (true)
            {
                try
                {
                    string resp = "";
                    WebSocketReceiveResult webSocketResp;
                    if (SubscriptionWebSocket.State == WebSocketState.Open)
                    {
                        do
                        {
                            ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1000]);
                            webSocketResp = await SubscriptionWebSocket.ReceiveAsync(bytesReceived, CancellationToken.None);
                            resp += Encoding.ASCII.GetString(bytesReceived.Array, 0, webSocketResp.Count);
                        }
                        while (!webSocketResp.EndOfMessage);

                        WebSocketMessage wsResp = JsonConvert.DeserializeObject<WebSocketMessage>(resp);

                        if (wsResp.Msg == "ClientLoginResponse")
                        {
                            ClientLoginResponse loginReponse = JsonConvert.DeserializeObject<ClientLoginResponse>(resp);

                            DoLog(string.Format("Client {0} succesfully logged. Token {1}", loginReponse.UserId, loginReponse.JsonWebToken));
                        }
                        else if (wsResp.Msg == "ClientReject")
                        {
                            ClientReject loginRejected = JsonConvert.DeserializeObject<ClientReject>(resp);
                            DoLog(string.Format("Login rejected for user {0}. Reason: {1}", loginRejected.UserId, loginRejected.RejectReason));
                        }
                        else
                            DoLog(string.Format("Unknown message: {0}", resp));
                    }
                    else
                        Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("Error: {0}", ex.Message));
                }
            }
        }

        static void Main(string[] args)
        {
            string WebSocketURL = ConfigurationManager.AppSettings["WebSocketURL"];

            DoLog("Connecting client...");
            ConnectWebSocket(WebSocketURL);

            DoLog("Client connected...");

            ClientMessageThread(new object[] { });

            DoLogin();

            Console.ReadKey();

        }
    }
}
