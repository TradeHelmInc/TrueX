using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Account.V2.Credit_UI;
using DGTLBackendMock.Common.DTO.Auth.V2;
using DGTLBackendMock.Common.DTO.Auth.V2.Credit_UI;
using DGTLBackendMock.Common.DTO.MarketData.V2;
using DGTLBackendMock.Common.DTO.OrderRouting.V2;
using DGTLBackendMock.Common.DTO.SecurityList.V2;
using DGTLBackendMock.Common.DTO.Subscription.V2;
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
    public delegate void ProcessEventV2(WebSocketMessageV2 msg);

    public class DGTLWebSocketClientV2 : DGTLWebSocketClient
    {
        #region Protected Attributes

        protected ProcessEventV2 OnEvent { get; set; }

        #endregion


        #region Constructors

        public DGTLWebSocketClientV2(string pWebSocketURL, ProcessEventV2 pOnEvent)
        {
            WebSocketURL = pWebSocketURL;
            OnEvent = pOnEvent;
        }

        #endregion

        #region Protected Methods

       
        #endregion

        #region Public Methods

        public override async void ReadResponses(object param)
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
                            WebSocketMessageV2 wsResp = JsonConvert.DeserializeObject<WebSocketMessageV2>(resp);

                            if (wsResp.Msg == "ClientLoginResponse")
                            {
                                ClientLoginResponse loginReponse = JsonConvert.DeserializeObject<ClientLoginResponse>(resp);
                                OnEvent(loginReponse);
                            }
                           
                            else if (wsResp.Msg == "ClientLogout")
                            {
                                ClientLogout logoutReponse = JsonConvert.DeserializeObject<ClientLogout>(resp);
                                OnEvent(logoutReponse);
                            }
                            else if (wsResp.Msg == "ClientLogout")
                            {
                                ClientOrderAck clientOrderAck = JsonConvert.DeserializeObject<ClientOrderAck>(resp);
                                OnEvent(clientOrderAck);
                            }
                            else if (wsResp.Msg == "ClientOrderRej")
                            {
                                ClientOrderRej clientOrderRej = JsonConvert.DeserializeObject<ClientOrderRej>(resp);
                                OnEvent(clientOrderRej);
                            }
                            else if (wsResp.Msg == "TokenResponse")
                            {
                                TokenResponse tokenReponse = JsonConvert.DeserializeObject<TokenResponse>(resp);
                                OnEvent(tokenReponse);
                            }
                            else if (wsResp.Msg == "SubscriptionResponse")
                            {
                                SubscriptionResponse msg = JsonConvert.DeserializeObject<SubscriptionResponse>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientAccountRecord")
                            {
                                ClientAccountRecord msg = JsonConvert.DeserializeObject<ClientAccountRecord>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientLastSale")
                            {
                                ClientLastSale msg = JsonConvert.DeserializeObject<ClientLastSale>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientBestBidOffer")
                            {
                                ClientBestBidOffer msg = JsonConvert.DeserializeObject<ClientBestBidOffer>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientMarketState")
                            {
                                ClientMarketState msg = JsonConvert.DeserializeObject<ClientMarketState>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientInstrument")
                            {
                                ClientInstrument msg = JsonConvert.DeserializeObject<ClientInstrument>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "DailySettlementPrice")
                            {
                                DailySettlement msg = JsonConvert.DeserializeObject<DailySettlement>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientOrderAck")
                            {
                                ClientOrderAck msg = JsonConvert.DeserializeObject<ClientOrderAck>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientOrderRej")
                            {
                                ClientOrderRej msg = JsonConvert.DeserializeObject<ClientOrderRej>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientOrderReq")
                            {
                                ClientOrderReq msg = JsonConvert.DeserializeObject<ClientOrderReq>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "FirmsCreditLimitUpdateResponse")
                            {
                                FirmsCreditLimitUpdateResponse msg = JsonConvert.DeserializeObject<FirmsCreditLimitUpdateResponse>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "FirmsTradingStatusUpdateResponse")
                            {
                                FirmsTradingStatusUpdateResponse msg = JsonConvert.DeserializeObject<FirmsTradingStatusUpdateResponse>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "EmailNotificationsListResponse")
                            {
                                EmailNotificationsListResponse msg = JsonConvert.DeserializeObject<EmailNotificationsListResponse>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "EmailNotificationsCreateResponse")
                            {
                                EmailNotificationsCreateResponse msg = JsonConvert.DeserializeObject<EmailNotificationsCreateResponse>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "EmailNotificationsUpdateResponse")
                            {
                                EmailNotificationsUpdateResponse msg = JsonConvert.DeserializeObject<EmailNotificationsUpdateResponse>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "EmailNotificationsDeleteResponse")
                            {
                                EmailNotificationsDeleteResponse msg = JsonConvert.DeserializeObject<EmailNotificationsDeleteResponse>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "FirmsListResponse")
                            {
                                FirmsListResponse msg = JsonConvert.DeserializeObject<FirmsListResponse>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientMassCancelResponse")
                            {
                                ClientMassCancelResponse msg = JsonConvert.DeserializeObject<ClientMassCancelResponse>(resp);
                                OnEvent(msg);
                            }
                            else if (wsResp.Msg == "ClientHeartbeat")
                                OnEvent(JsonConvert.DeserializeObject<ClientHeartbeat>(resp));
                            else
                            {
                                UnknownMessageV2 unknownMsg = new UnknownMessageV2()
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
                    ErrorMessageV2 errorMsg = new ErrorMessageV2() { Msg = "ErrorMsg", Error = ex.Message };
                    OnEvent(errorMsg);
                }
            }
        }

        #endregion
    }
}
