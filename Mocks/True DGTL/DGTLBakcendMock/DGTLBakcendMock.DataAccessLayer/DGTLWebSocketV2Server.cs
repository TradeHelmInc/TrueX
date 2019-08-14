using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Auth.V2;
using DGTLBackendMock.Common.Util;
using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.DataAccessLayer
{
    public class DGTLWebSocketV2Server : DGTLWebSocketServer
    {
        #region Constructors

        public DGTLWebSocketV2Server(string pURL, string pRESTAdddress)
            : base(pURL, pRESTAdddress)
        { 
        
        }

        #endregion

        #region Protected Attributes

        protected string LastTokenGenerated { get; set; }

        #endregion

        #region Private Methods

        private void ProcessTokenResponse(IWebSocketConnection socket, string m)
        {
            TokenRequest wsResp = JsonConvert.DeserializeObject<TokenRequest>(m);

            LastTokenGenerated = Guid.NewGuid().ToString();

            TokenResponse resp = new TokenResponse() { Msg = "TokenResponse", Token = LastTokenGenerated };

            DoSend<TokenResponse>(socket, resp);
        }

        private void ProcessClientLoginV2(IWebSocketConnection socket, string m)
        {
            ClientLogin wsLogin = JsonConvert.DeserializeObject<ClientLogin>(m);

            try
            {
                
                byte[] IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                byte[] keyBytes = AESCryptohandler.makePassPhrase(LastTokenGenerated);

                byte[] secretByteArr = Convert.FromBase64String(wsLogin.Secret);

                string userAndPassword = AESCryptohandler.DecryptStringFromBytes(secretByteArr, keyBytes, IV);

                string[] credentials = userAndPassword.Split(new string[] { "---" }, StringSplitOptions.RemoveEmptyEntries);


                if (credentials.Length != 2)
                {
                    DoLog(string.Format("Invalid format for user and password: {0}", userAndPassword), MessageType.Error);
                    //TODO : implement rejection mechanism for invalid format for user and paswword
                    return;
                }

                if(!UserRecords.Any(x=>x.UserId==credentials[0]))
                {
                    DoLog(string.Format("Unknown user: {0}", credentials[0]), MessageType.Error);
                    //TODO : implement rejection mechanism for invalid format for user and paswword
                    return;
                
                }

                if (credentials[1]!="Testing123")
                {
                    DoLog(string.Format("Wrong password {1} for user {0}", credentials[0], credentials[1]), MessageType.Error);
                    //TODO : implement rejection mechanism for invalid format for user and paswword
                    return;

                }

                ClientLoginResponse loginResp = new ClientLoginResponse()
                {
                    Msg = "ClientLoginResponse",
                    Uuid = wsLogin.Uuid,
                    Token = _TOKEN
                };

                DoSend<ClientLoginResponse>(socket, loginResp);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception unencrypting secret {0}: {1}", wsLogin.Secret, ex.Message), MessageType.Error);
                //TODO implement ClientReject mechanism
            }
        }

        #endregion

        #region Protected Methods

        protected void ProcessClientLogoutV2(IWebSocketConnection socket, string m)
        {
            ClientLogout wsLogout = JsonConvert.DeserializeObject<ClientLogout>(m);

            ClientLogout logout = new ClientLogout()
            {
                Msg = "ClientLogout",
                Token = wsLogout.Token,
                UserId = wsLogout.UserId,
                Uuid = wsLogout.UserId
            };


            DoSend<ClientLogout>(socket, logout);
            socket.Close();
        }

        protected override void OnMessage(IWebSocketConnection socket, string m)
        {
            try
            {
                WebSocketMessageV2 wsResp = JsonConvert.DeserializeObject<WebSocketMessageV2>(m);

                DoLog(string.Format("OnMessage {1} from IP -> {0}", socket.ConnectionInfo.ClientIpAddress, wsResp.Msg), MessageType.Information);

                if (wsResp.Msg == "ClientLogin")
                {
                    ProcessClientLoginV2(socket, m);
                }
                else if (wsResp.Msg == "TokenRequest")
                {
                    ProcessTokenResponse(socket, m);
                }
                else if (wsResp.Msg == "ClientHeartbeatResponse")
                {
                    //We do nothing as the DGTL server does

                }
                else if (wsResp.Msg == "ClientLogout")
                {

                    ProcessClientLogoutV2(socket,m);

                }
                else if (wsResp.Msg == "LegacyOrderReq")
                {

                    ProcessLegacyOrderReqMock(socket, m);

                }
                else if (wsResp.Msg == "ResetPasswordRequest")
                {

                    ProcessResetPasswordRequest(socket, m);

                }
                else if (wsResp.Msg == "Subscribe")
                {

                    ProcessSubscriptions(socket, m);

                }
                else if (wsResp.Msg == "LegacyOrderCancelReq")
                {
                    ProcessLegacyOrderCancelMock(socket, m);
                }
                else if (wsResp.Msg == "LegacyOrderMassCancelReq")
                {
                    ProcessLegacyOrderMassCancelMock(socket, m);
                }
                else
                {
                    UnknownMessage unknownMsg = new UnknownMessage()
                    {
                        Msg = "MessageReject",
                        Reason = string.Format("Unknown message type {0}", wsResp.Msg)

                    };


                    DoSend<UnknownMessage>(socket, unknownMsg);
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing onMessage:{0}", ex.Message), MessageType.Error);
                UnknownMessage errorMsg = new UnknownMessage()
                {
                    Msg = "MessageReject",
                    Reason = string.Format("Error processing message: {0}", ex.Message)

                };

                DoSend<UnknownMessage>(socket, errorMsg);
            }

        }

        #endregion
    }
}
