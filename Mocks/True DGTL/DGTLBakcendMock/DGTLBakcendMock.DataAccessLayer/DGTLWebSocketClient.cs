﻿using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DGTLBackendMock.DataAccessLayer
{
    public delegate void ProcessEvent(WebSocketMessage msg);

    public class DGTLWebSocketClient
    {
        #region Protected Attributes

        protected string WebSocketURL { get; set; }

        protected ProcessEvent OnEvent { get; set; }

        protected  ClientWebSocket SubscriptionWebSocket { get; set; }

        #endregion

        #region Constructors

        public DGTLWebSocketClient(string pWebSocketURL, ProcessEvent pOnEvent)
        {
            WebSocketURL = pWebSocketURL;
            OnEvent = pOnEvent;
        }

        #endregion


        #region Public Methods

        public async void Connect()
        {

            SubscriptionWebSocket = new ClientWebSocket();
            await SubscriptionWebSocket.ConnectAsync(new Uri(WebSocketURL), CancellationToken.None);

            Thread respThread = new Thread(ReadResponses);
            respThread.Start(new object[] { });
        }

        public async void ReadResponses(object param)
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
                            OnEvent(loginReponse);
                        }
                        else if (wsResp.Msg == "ClientReject")
                        {
                            ClientReject loginRejected = JsonConvert.DeserializeObject<ClientReject>(resp);
                            OnEvent(loginRejected);
                        }
                        else
                        {
                            UnknownMessage unknownMsg = new UnknownMessage() { Msg = "UnknownMsg", Reason = string.Format("Unknown message: {0}", resp) };
                            OnEvent(unknownMsg);
                        }
                    }
                    else
                        Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    ErrorMessage errorMsg = new ErrorMessage() { Msg = "ErrorMsg", Error = ex.Message } ;
                    OnEvent(errorMsg);
                }
            }
        }

        public async void Send(string strMsg)
        {
            byte[] msgArray = Encoding.ASCII.GetBytes(strMsg);

            ArraySegment<byte> bytesToSend = new ArraySegment<byte>(msgArray);

            await SubscriptionWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                                                          CancellationToken.None);
        
        }


        #endregion
    }
}
