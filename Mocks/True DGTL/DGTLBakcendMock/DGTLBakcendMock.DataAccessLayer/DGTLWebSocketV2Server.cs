using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Auth.V2;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.SecurityList.V2;
using DGTLBackendMock.Common.Util;
using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        protected Thread HeartbeatThread { get; set; }

        #endregion

        #region Private and Protected Methods

        protected override void DoCLoseThread(object p)
        {
            base.DoCLoseThread(p);

            lock (tLock)
            {
                HeartbeatThread.Abort();
            
            }
        }


        protected void SendHeartbeat(object param)
        {
            IWebSocketConnection socket = (IWebSocketConnection)((object[])param)[0];
            string token = (string)((object[])param)[1];
            string uuid = (string)((object[])param)[2];

            try
            {

                ClientHeartbeat heartbeat = new ClientHeartbeat()
                {
                    Msg = "ClientHeartbeat",
                    Token = token,
                    UUID = uuid

                };

                while (true)
                {
                    Thread.Sleep(3000);//3 seconds
                    DoSend<ClientHeartbeat>(socket, heartbeat);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error sending heartbeat for token {0}: {1}", token,ex.Message), MessageType.Error);
            }

        }

        private void ProcessTokenResponse(IWebSocketConnection socket, string m)
        {
            TokenRequest wsTokenReq = JsonConvert.DeserializeObject<TokenRequest>(m);

            LastTokenGenerated = Guid.NewGuid().ToString();

            TokenResponse resp = new TokenResponse() { Msg = "TokenResponse", Token = LastTokenGenerated, UUID = wsTokenReq.UUID };

            DoSend<TokenResponse>(socket, resp);
        }

        private void SendClientReject(IWebSocketConnection socket, ClientLogin wsLogin)
        {
            ClientReject reject = new ClientReject()
            {
                Msg = "ClientReject",
                RejectCode = ClientReject._GENERIC_REJECT_CODE,
                Sender = 0,
                Time = 0,
                UUID = wsLogin.UUID
            };

            DoSend<ClientReject>(socket, reject);
        }

        private void SendCRMInstruments(IWebSocketConnection socket,string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {

                InstrumentMsg instrumentMsg = new InstrumentMsg();
                instrumentMsg.Msg = "InstrumentMsg";
                instrumentMsg.CreatedAt = Convert.ToInt64(epochElapsed.TotalMilliseconds);
                instrumentMsg.UpdatedAt = Convert.ToInt64(epochElapsed.TotalMilliseconds);
                instrumentMsg.ExchangeId = 0;
                instrumentMsg.Description = security.Description;
                instrumentMsg.InstrumentDate = security.MaturityDate;
                instrumentMsg.InstrumentId = security.Symbol;
                instrumentMsg.InstrumentName = security.Description;
                instrumentMsg.LastUpdatedBy = "fernandom";
                instrumentMsg.MaxLotSize = Convert.ToDouble(security.MaxSize);
                instrumentMsg.MinLotSize = Convert.ToDouble(security.MinSize);
                instrumentMsg.Currency1 = "";
                instrumentMsg.Currency2 = "";
                instrumentMsg.Test = false;
                instrumentMsg.UUID = Uuid;

                DoSend<InstrumentMsg>(socket, instrumentMsg);
            }
        
        }

        //Sending logged user
        private void SendCRMUsers(IWebSocketConnection socket, string login, string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            UserRecord userRecord =   UserRecords.Where(x=>x.UserId==login).FirstOrDefault();

            if (userRecord != null)
            {
                UserRecordMsg userRecordMsg = new UserRecordMsg();
                userRecordMsg.Address = "";
                userRecordMsg.cConnectionType = '0';
                userRecordMsg.City = "";
                userRecordMsg.cUserType = '0';
                userRecordMsg.Email = "";
                userRecordMsg.FirmId = userRecord.FirmId;
                userRecordMsg.FirstName = userRecord.FirstName;
                userRecordMsg.GroupId = "";
                userRecordMsg.IsAdmin = false;
                userRecordMsg.LastName = userRecord.LastName;
                userRecordMsg.Msg = "UserRecordMsg";
                userRecordMsg.PostalCode = "";
                userRecordMsg.State = "";
                userRecordMsg.UserId = userRecord.UserId;
                userRecordMsg.UUID = Uuid;

                DoSend<UserRecordMsg>(socket, userRecordMsg);
            }
            else
                throw new Exception(string.Format("User not found {0}", login));
        }

        private void SendMarketStatus(IWebSocketConnection socket,string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);

            MarketStateMsg marketStateMsg = new MarketStateMsg();
            marketStateMsg.cExchangeId=MarketStateMsg._DEFAULT_EXCHANGE_ID;
            marketStateMsg.cReasonCode='0';
            marketStateMsg.cState = PlatformStatus.cState ;
            marketStateMsg.Msg = "MarketStateMsg";
            marketStateMsg.StateTime = Convert.ToInt64(epochElapsed.TotalMilliseconds);
            marketStateMsg.UUID = Uuid;

            DoSend<MarketStateMsg>(socket, marketStateMsg);
        }

        private void SendCRMAccounts(IWebSocketConnection socket, string login,string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            UserRecord userRecord = UserRecords.Where(x => x.UserId == login).FirstOrDefault();

            if (userRecord != null)
            {
                List<DGTLBackendMock.Common.DTO.Account.AccountRecord> accountRecords = AccountRecords.Where(x => x.EPFirmId == userRecord.FirmId).ToList();

                foreach (DGTLBackendMock.Common.DTO.Account.AccountRecord accountRecord in accountRecords)
                {
                    AccountRecordMsg accountRecordMsg = new AccountRecordMsg();
                    accountRecordMsg.AccountAlias = accountRecord.AccountId;
                    accountRecordMsg.AccountId = accountRecord.AccountId;
                    accountRecordMsg.AccountName = accountRecord.EPNickName;
                    accountRecordMsg.AccountNumber = "";
                    accountRecordMsg.AccountType = 0;
                    accountRecordMsg.cStatus = AccountRecordMsg._DEFAULT_STATUS;
                    accountRecordMsg.Cti = 0;
                    accountRecordMsg.Currency = "";
                    accountRecordMsg.FirmId = userRecord.FirmId;
                    accountRecordMsg.WalletAddress = "";
                    accountRecordMsg.UUID = Uuid;

                    DoSend<AccountRecordMsg>(socket, accountRecordMsg);
                }
            }
            else
                throw new Exception(string.Format("User not found {0}", login));
        
        }

        private void SendCRMMessages(IWebSocketConnection socket,string login,string Uuid)
        {
            SendCRMInstruments(socket, Uuid);

            SendCRMUsers(socket, login,Uuid);

            SendMarketStatus(socket, Uuid);

            SendCRMAccounts(socket, login, Uuid);
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
                    SendClientReject(socket, wsLogin);
                    return;
                }

                if(!UserRecords.Any(x=>x.UserId==credentials[0]))
                {
                    DoLog(string.Format("Unknown user: {0}", credentials[0]), MessageType.Error);
                    SendClientReject(socket, wsLogin);
                    return;
                
                }

                if (credentials[1]!="Testing123")
                {
                    DoLog(string.Format("Wrong password {1} for user {0}", credentials[0], credentials[1]), MessageType.Error);
                    SendClientReject(socket, wsLogin);
                    return;

                }

                ClientLoginResponse loginResp = new ClientLoginResponse()
                {
                    Msg = "ClientLoginResponse",
                    UUID = wsLogin.UUID,
                    Token = LastTokenGenerated
                };

                DoSend<ClientLoginResponse>(socket, loginResp);

                SendCRMMessages(socket, credentials[0], wsLogin.UUID);

                HeartbeatThread = new Thread(SendHeartbeat);
                HeartbeatThread.Start(new object[] { socket, loginResp.Token, loginResp.UUID });
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception unencrypting secret {0}: {1}", wsLogin.Secret, ex.Message), MessageType.Error);
                SendClientReject(socket, wsLogin);
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
                UUID = wsLogout.UserId
            };


            DoSend<ClientLogout>(socket, logout);
            socket.Close();

            if(HeartbeatThread!=null)
                HeartbeatThread.Abort();
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
                else if (wsResp.Msg == "ClientHeartbeat")
                {
                    //We do nothing//

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
