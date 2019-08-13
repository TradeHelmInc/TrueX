using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Subscription;
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

        public DGTLWebSocketClient() { }

        #endregion


        #region Public Methods

        public async Task<bool> Connect()
        {

            SubscriptionWebSocket = new ClientWebSocket();
            await SubscriptionWebSocket.ConnectAsync(new Uri(WebSocketURL), CancellationToken.None);

            Thread respThread = new Thread(ReadResponses);
            respThread.Start(new object[] { });
            return true;
        }

        public virtual async void ReadResponses(object param)
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

                        if (resp != "")
                        {
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
                            else if (wsResp.Msg == "ClientLogoutResponse")
                            {
                                ClientLogoutResponse logoutReponse = JsonConvert.DeserializeObject<ClientLogoutResponse>(resp);
                                OnEvent(logoutReponse);
                            }
                            else if (wsResp.Msg == "SubscriptionResponse")
                            {
                                SubscriptionResponse subscrResponse = JsonConvert.DeserializeObject<SubscriptionResponse>(resp);
                                OnEvent(subscrResponse);
                            }
                            else if (wsResp.Msg == "ClientHeartbeatRequest")
                                OnEvent(JsonConvert.DeserializeObject<ClientHeartbeatRequest>(resp));
                            else if (wsResp.Msg == "AccountRecord")
                                OnEvent(JsonConvert.DeserializeObject<AccountRecord>(resp));
                            else if (wsResp.Msg == "CreditRecordUpdate")
                                OnEvent(JsonConvert.DeserializeObject<CreditRecordUpdate>(resp));
                            else if (wsResp.Msg == "DailySettlementPrice")
                                OnEvent(JsonConvert.DeserializeObject<DailySettlementPrice>(resp));
                            else if (wsResp.Msg == "FirmRecord")
                                OnEvent(JsonConvert.DeserializeObject<FirmRecord>(resp));
                            else if (wsResp.Msg == "OfficialFixingPrice")
                                OnEvent(JsonConvert.DeserializeObject<OfficialFixingPrice>(resp));
                            else if (wsResp.Msg == "RefereceRateMsg")
                                OnEvent(JsonConvert.DeserializeObject<RefereceRateMsg>(resp));
                            else if (wsResp.Msg == "SecurityMasterRecord")
                                OnEvent(JsonConvert.DeserializeObject<SecurityMasterRecord>(resp));
                            else if (wsResp.Msg == "UserRecord")
                                OnEvent(JsonConvert.DeserializeObject<UserRecord>(resp));
                            else if (wsResp.Msg == "CreditRecordUpdate")
                                OnEvent(JsonConvert.DeserializeObject<CreditRecordUpdate>(resp));
                            else if (wsResp.Msg == "LastSale")
                                OnEvent(JsonConvert.DeserializeObject<LastSale>(resp));
                            else if (wsResp.Msg == "Quote")
                                OnEvent(JsonConvert.DeserializeObject<Quote>(resp));
                            else if (wsResp.Msg == "DepthOfBook")
                                OnEvent(JsonConvert.DeserializeObject<DepthOfBook>(resp));
                            else if (wsResp.Msg == "LegacyOrderAck")
                                OnEvent(JsonConvert.DeserializeObject<LegacyOrderAck>(resp));
                            else if (wsResp.Msg == "LegacyOrderCancelRejAck")
                                OnEvent(JsonConvert.DeserializeObject<LegacyOrderCancelRejAck>(resp));
                            else
                            {
                                UnknownMessage unknownMsg = new UnknownMessage()
                                {
                                    Msg = "UnknownMsg",
                                    Resp = resp,
                                    Reason = string.Format("Unknown message: {0}", resp)
                                };
                                OnEvent(unknownMsg);
                            }
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
