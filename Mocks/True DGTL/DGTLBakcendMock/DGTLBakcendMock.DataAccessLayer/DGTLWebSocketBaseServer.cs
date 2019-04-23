using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Subscription;
using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToolsShared.Logging;
using zHFT.Main.Common.Util;

namespace DGTLBackendMock.DataAccessLayer
{
    public enum MessageType { Information, Debug, Error, Exception, EndLog };

    public abstract class DGTLWebSocketBaseServer
    {
        #region Protected Attributes

        protected string URL { get; set; }

        protected Fleck.WebSocketServer WebSocketServer { get; set; }

        public int HeartbeatSeqNum { get; set; }

        public bool UserLogged { get; set; }

        protected ILogSource Logger;

        protected UserRecord[] UserRecords { get; set; }

        #endregion

        #region Protected Methods

        protected void OnLogMessage(string msg, Constants.MessageType type)
        {
            Logger.Debug(msg, type);
        }

        protected void DoLog(string msg, MessageType type)
        {
            Logger.Debug(msg, type);
        }

        protected void DoSend<T>(IWebSocketConnection socket, T entity)
        {
            string strUserRecord = JsonConvert.SerializeObject(entity, Newtonsoft.Json.Formatting.None,
                              new JsonSerializerSettings
                              {
                                  NullValueHandling = NullValueHandling.Ignore
                              });

            socket.Send(strUserRecord);

        }

        protected void LoadUserRecords()
        {
            string strUserRecords = File.ReadAllText(@".\input\UserRecord.json");

            //Aca le metemos que serialize el contenido
            UserRecords = JsonConvert.DeserializeObject<UserRecord[]>(strUserRecords);
        }

      

        protected void ProcessClientLoginMock(IWebSocketConnection socket, string m)
        {
            WebSocketLoginMessage wsLogin = JsonConvert.DeserializeObject<WebSocketLoginMessage>(m);


            UserRecord loggedUser = UserRecords.Where(x => x.UserId == wsLogin.UserId).FirstOrDefault();

            DoLog(string.Format("Incoming Login request for user {0}", wsLogin.UUID), MessageType.Information);

            if (loggedUser != null)
            {
                ClientLoginResponse resp = new ClientLoginResponse()
                {
                    Msg = "ClientLoginResponse",
                    Sender = wsLogin.Sender,
                    UUID = wsLogin.UUID,
                    UserId = wsLogin.UserId,
                    JsonWebToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE1NTEzODY5NjksImV4cCI"


                };


                DoLog(string.Format("user {0} Successfully logged in", wsLogin.UUID), MessageType.Information);

                string respMsg = JsonConvert.SerializeObject(resp, Newtonsoft.Json.Formatting.None,
                                               new JsonSerializerSettings
                                               {
                                                   NullValueHandling = NullValueHandling.Ignore
                                               });

                UserLogged = true;
                socket.Send(respMsg);
            }
            else
            {
                ClientReject reject = new ClientReject()
                {
                    Msg = "ClientReject",
                    Sender = wsLogin.Sender,
                    UUID = wsLogin.UUID,
                    UserId = wsLogin.UserId,
                    RejectReason = string.Format("Invalid user or password")
                };

                DoLog(string.Format("user {0} Rejected because of wrong user or password", wsLogin.UUID), MessageType.Information);

                string rejMsg = JsonConvert.SerializeObject(reject, Newtonsoft.Json.Formatting.None,
                       new JsonSerializerSettings
                       {
                           NullValueHandling = NullValueHandling.Ignore
                       });
                socket.Send(rejMsg);
                socket.Close();
            }
        }

        protected void ProcessClientLogoutMock(IWebSocketConnection socket)
        {

            ClientLogoutResponse logout = new ClientLogoutResponse()
            {
                Msg = "ClientLogoutResponse",
                UserId = "0",
                Sender = 1,
                Time = 0
            };



            string logoutRespMsg = JsonConvert.SerializeObject(logout, Newtonsoft.Json.Formatting.None,
                       new JsonSerializerSettings
                       {
                           NullValueHandling = NullValueHandling.Ignore
                       });
            socket.Send(logoutRespMsg);
            socket.Close();

        }

        

        protected void ProcessSubscriptionResponse(IWebSocketConnection socket, string service, string serviceKey, string UUID, bool success=true, string msg ="")
        {
            SubscriptionResponse resp = new SubscriptionResponse()
            {
                Message = msg,
                Success = success,
                Service = service,
                ServiceKey = serviceKey,
                UUID = UUID,
                Msg = "SubscriptionResponse"

            };

            string strSubscResp = JsonConvert.SerializeObject(resp, Newtonsoft.Json.Formatting.None,
                         new JsonSerializerSettings
                         {
                             NullValueHandling = NullValueHandling.Ignore
                         });

            socket.Send(strSubscResp);

        }

        protected void ProcessUserRecord(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            if (subscrMsg.ServiceKey != "*")
            {

                UserRecord userRecord = UserRecords.Where(x => x.UserId == subscrMsg.ServiceKey).FirstOrDefault();

                if (userRecord != null)
                    DoSend<UserRecord>(socket, userRecord);
            }
            else
            {
                foreach (UserRecord userRecord in UserRecords)
                {
                    DoSend<UserRecord>(socket, userRecord);
                }

            }
            ProcessSubscriptionResponse(socket, "TB", subscrMsg.ServiceKey, subscrMsg.UUID);
        }

        #endregion

        #region Thread Methods

        protected void ClientHeartbeatThread(object param)
        {
            IWebSocketConnection socket = (IWebSocketConnection)param;

            while (socket.IsAvailable)
            {
                try
                {
                    if (UserLogged)
                    {
                        ClientHeartbeatRequest heartbeatReq = new ClientHeartbeatRequest()
                        {
                            Msg = "ClientHeartbeatRequest",
                            UserId = "user1",
                            Sender = 0,
                            SeqNum = HeartbeatSeqNum,
                            Time = 0,
                            UUID = "user1"

                        };


                        string strHeartbeatReq = JsonConvert.SerializeObject(heartbeatReq, Newtonsoft.Json.Formatting.None,
                              new JsonSerializerSettings
                              {
                                  NullValueHandling = NullValueHandling.Ignore
                              });
                        socket.Send(strHeartbeatReq);
                        HeartbeatSeqNum++;
                    }

                    Thread.Sleep(2000);
                }
                catch (Exception ex)
                {
                    socket.Close();
                }
            }


        }

        #endregion

        #region Public Abstract Methods

        protected abstract void OnOpen(IWebSocketConnection socket);

        protected abstract void OnClose(IWebSocketConnection socket);

        protected abstract void OnMessage(IWebSocketConnection socket, string m);

        #endregion

        #region Public Methods

        public void Start()
        {
            WebSocketServer = new Fleck.WebSocketServer(URL);
            WebSocketServer.Start(socket =>
            {
                socket.OnOpen = () => OnOpen(socket);
                socket.OnClose = () => OnClose(socket);
                socket.OnMessage = m => OnMessage(socket, m);
            });

        }

        #endregion
    }
}
