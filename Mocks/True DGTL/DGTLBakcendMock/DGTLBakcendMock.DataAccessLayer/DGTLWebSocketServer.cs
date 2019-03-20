using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth;
using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBakcendMock.DataAccessLayer
{
    public class DGTLWebSocketServer
    {
        #region Protected Attributes

        protected string URL { get; set; }

        protected Fleck.WebSocketServer WebSocketServer { get; set; }

        #endregion


        #region Constructors

        public DGTLWebSocketServer(string pURL)
        {
            URL = pURL;
        
        }


        #endregion

        #region Protected Methods

        protected  void OnOpen(IWebSocketConnection socket)
        {
            //socket.Send("Connection Opened");
        }

        protected void OnClose(IWebSocketConnection socket)
        {


        }

        protected void OnMessage(IWebSocketConnection socket, string m)
        {
            try
            {
                WebSocketMessage wsResp = JsonConvert.DeserializeObject<WebSocketMessage>(m);

                if (wsResp.Msg == "ClientLogin")
                {
                    WebSocketLoginMessage wsLogin = JsonConvert.DeserializeObject<WebSocketLoginMessage>(m);

                    if (wsLogin.UserId == "user1" && wsLogin.Password == "tr4d3h3lm")
                    {
                        ClientLoginResponse resp = new ClientLoginResponse()
                        {
                            Msg = "ClientLoginResponse",
                            Sender = wsLogin.Sender,
                            UUID = wsLogin.UUID,
                            UserId = wsLogin.UserId,
                            JWToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE1NTEzODY5NjksImV4cCI"


                        };

                        string respMsg = JsonConvert.SerializeObject(resp, Newtonsoft.Json.Formatting.None,
                                                       new JsonSerializerSettings
                                                       {
                                                           NullValueHandling = NullValueHandling.Ignore
                                                       });
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

                        string rejMsg = JsonConvert.SerializeObject(reject, Newtonsoft.Json.Formatting.None,
                               new JsonSerializerSettings
                               {
                                   NullValueHandling = NullValueHandling.Ignore
                               });
                        socket.Send(rejMsg);
                        socket.Close();
                    }

                }
                else if (wsResp.Msg == "ClientHeartbeat")
                { 
                    //We do nothing as the DGTL server does
                
                }
                else if (wsResp.Msg == "ClientLogout")
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
                else
                {
                    UnknownMessage unknownMsg = new UnknownMessage()
                    {
                        Msg = "MessageReject",
                        Reason = string.Format("Unknown message type {0}", wsResp.Msg)

                    };

                    string strUnknownMsg = JsonConvert.SerializeObject(unknownMsg, Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                    socket.Send(strUnknownMsg);
                }

            }
            catch (Exception ex)
            {
                UnknownMessage errorMsg = new UnknownMessage()
                {
                    Msg = "MessageReject",
                    Reason = string.Format("Error processing message: {0}", ex.Message)

                };

                string strErrorMsg = JsonConvert.SerializeObject(errorMsg, Newtonsoft.Json.Formatting.None,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                socket.Send(strErrorMsg);
            }

        }

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
