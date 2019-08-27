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
            try
            {
                base.DoCLoseThread(p);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error closing base.DoCLoseThread:{0}", ex.Message), MessageType.Error);
            
            }

            lock (tLock)
            {
                try
                {
                    HeartbeatThread.Abort();
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("Error closing HeartbeatThread:{0}", ex.Message), MessageType.Error);
                }
            }
        }


        protected void SendHeartbeat(object param)
        {
            IWebSocketConnection socket = (IWebSocketConnection)((object[])param)[0];
            string token = (string)((object[])param)[1];
            string uuid = (string)((object[])param)[2];

            try
            {

                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                ClientHeartbeat heartbeat = new ClientHeartbeat()
                {
                    Msg = "ClientHeartbeat",
                    JsonWebToken = token,
                    UUID = uuid,
                    Time = Convert.ToInt64(elapsed.TotalMilliseconds)

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

            TokenResponse resp = new TokenResponse() 
            {
                Msg = "TokenResponse", 
                JsonWebToken = LastTokenGenerated, 
                UUID = wsTokenReq.UUID,
                cSuccess=TokenResponse._STATUS_OK,
                Time=wsTokenReq.Time
            };

            DoSend<TokenResponse>(socket, resp);
        }

        private void SendLoginRejectReject(IWebSocketConnection socket, ClientLoginRequest wsLogin, string msg)
        {
            ClientLoginResponse reject = new ClientLoginResponse()
            {
                Msg = "ClientLoginResponse",
                UUID = wsLogin.UUID,
                JsonWebToken = LastTokenGenerated,
                Message = msg,
                cSuccess = ClientLoginResponse._STATUS_FAILED,
                Time=wsLogin.Time,
                UserId = null
            };

            DoSend<ClientLoginResponse>(socket, reject);
        }

        private void SendCRMInstruments(IWebSocketConnection socket,string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {

                Instrument instrumentMsg = new Instrument();
                instrumentMsg.Msg = "Instrument";
                instrumentMsg.CreatedAt = Convert.ToInt64(epochElapsed.TotalMilliseconds);
                instrumentMsg.UpdatedAt = Convert.ToInt64(epochElapsed.TotalMilliseconds);
                instrumentMsg.LastUpdatedBy = "";
                instrumentMsg.ExchangeId = 0;
                instrumentMsg.Description = security.Description;
                instrumentMsg.InstrumentDate = security.MaturityDate;
                instrumentMsg.InstrumentId = security.Symbol;
                instrumentMsg.InstrumentName = security.Description;
                instrumentMsg.LastUpdatedBy = "fernandom";
                instrumentMsg.LotSize = security.LotSize;
                instrumentMsg.MaxLotSize = Convert.ToDouble(security.MaxSize);
                instrumentMsg.MinLotSize = Convert.ToDouble(security.MinSize);
                instrumentMsg.cProductType = Instrument.GetProductType(security.AssetClass);
                instrumentMsg.MinQuotePrice = security.MinPrice;
                instrumentMsg.MaxQuotePrice = security.MaxPrice;
                instrumentMsg.MinPriceIncrement = security.MinPriceIncrement;
                instrumentMsg.MaxNotionalValue = security.MaxPrice * security.LotSize;
                instrumentMsg.Currency1 = "";
                instrumentMsg.Currency2 = "";
                instrumentMsg.Test = false;
                instrumentMsg.UUID = Uuid;

                DoLog(string.Format("Sending Instrument with UUID {0}", instrumentMsg.UUID), MessageType.Information);
                DoSend<Instrument>(socket, instrumentMsg);
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

                DoLog(string.Format("Sending UserRecordMsg with UUID {0}", userRecordMsg.UUID), MessageType.Information);
                DoSend<UserRecordMsg>(socket, userRecordMsg);
            }
            else
                throw new Exception(string.Format("User not found {0}", login));
        }

        private void SendMarketStatus(IWebSocketConnection socket,string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);

            MarketState marketStateMsg = new MarketState();
            marketStateMsg.cExchangeId=MarketState._DEFAULT_EXCHANGE_ID;
            marketStateMsg.cReasonCode='0';
            marketStateMsg.cState = MarketState.TranslateV1StatesToV2States(PlatformStatus.cState);
            marketStateMsg.Msg = "MarketState";
            marketStateMsg.StateTime = Convert.ToInt64(epochElapsed.TotalMilliseconds);
            marketStateMsg.UUID = Uuid;

            DoLog(string.Format("Sending MarketState with UUID {0}", marketStateMsg.UUID), MessageType.Information);
            DoSend<MarketState>(socket, marketStateMsg);
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
                    AccountRecord accountRecordMsg = new AccountRecord();
                    accountRecordMsg.Msg = "AccountRecord";
                    accountRecordMsg.AccountId = accountRecord.AccountId;
                    accountRecordMsg.FirmId = userRecord.FirmId;
                    accountRecordMsg.SettlementFirmId = "1";
                    accountRecordMsg.AccountName = accountRecord.EPNickName;
                    accountRecordMsg.AccountAlias = accountRecord.AccountId;
                    
                    
                    accountRecordMsg.AccountNumber = "";
                    accountRecordMsg.AccountType = 0;
                    accountRecordMsg.cStatus = AccountRecord._STATUS_ACTIVE;
                    accountRecordMsg.cUserType = AccountRecord._DEFAULT_USER_TYPE;
                    accountRecordMsg.cCti = AccountRecord._CTI_OTHER;
                    accountRecordMsg.Lei = "";
                    accountRecordMsg.Currency = "";
                    accountRecordMsg.IsSuspense = false;
                    accountRecordMsg.UsDomicile = true;
                    accountRecordMsg.UpdatedAt = 0;
                    accountRecordMsg.CreatedAt = 0;
                    accountRecordMsg.LastUpdatedBy = "";
                    accountRecordMsg.WalletAddress = "";
                    accountRecordMsg.UUID = Uuid;

                    DoLog(string.Format("Sending AccountRecord with UUID {0}", accountRecordMsg.UUID), MessageType.Information);
                    DoSend<AccountRecord>(socket, accountRecordMsg);
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
            ClientLoginRequest wsLogin = JsonConvert.DeserializeObject<ClientLoginRequest>(m);

            try
            {
                
                //byte[] IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                byte[] keyBytes = AESCryptohandler.makePassPhrase(LastTokenGenerated);

                byte[] IV = keyBytes;

                byte[] secretByteArr = Convert.FromBase64String(wsLogin.Secret);

                string jsonUserAndPassword = AESCryptohandler.DecryptStringFromBytes(secretByteArr, keyBytes, IV);

                JsonCredentials jsonCredentials = JsonConvert.DeserializeObject<JsonCredentials>(jsonUserAndPassword);

                if (!UserRecords.Any(x => x.UserId == jsonCredentials.UserId))
                {
                    DoLog(string.Format("Unknown user: {0}", jsonCredentials.UserId), MessageType.Error);
                    SendLoginRejectReject(socket, wsLogin, string.Format("Unknown user: {0}", jsonCredentials.UserId));
                    return;
                
                }

                if (jsonCredentials .Password!= "Testing123")
                {
                    DoLog(string.Format("Wrong password {1} for user {0}", jsonCredentials.UserId, jsonCredentials.Password), MessageType.Error);
                    SendLoginRejectReject(socket, wsLogin, string.Format("Wrong password {1} for user {0}", jsonCredentials.UserId, jsonCredentials.UserId));
                    return;

                }

                ClientLoginResponse loginResp = new ClientLoginResponse()
                {
                    Msg = "ClientLoginResponse",
                    UUID = wsLogin.UUID,
                    JsonWebToken = LastTokenGenerated,
                    cSuccess = ClientLoginResponse._STATUS_OK,
                    Time = wsLogin.Time,
                    UserId=Guid.NewGuid().ToString()
                };

                DoLog(string.Format("Sending ClientLoginResponse with UUID {0}", loginResp.UUID), MessageType.Information);

                DoSend<ClientLoginResponse>(socket, loginResp);

                SendCRMMessages(socket, jsonCredentials.UserId, wsLogin.UUID);

                HeartbeatThread = new Thread(SendHeartbeat);
                HeartbeatThread.Start(new object[] { socket, loginResp.JsonWebToken, loginResp.UUID });
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception unencrypting secret {0}: {1}", wsLogin.Secret, ex.Message), MessageType.Error);
                SendLoginRejectReject(socket, wsLogin,string.Format("Exception unencrypting secret {0}: {1}", wsLogin.Secret, ex.Message));
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
                JsonWebToken = wsLogout.JsonWebToken,
                UserId = wsLogout.UserId,
                UUID = wsLogout.UserId
            };


            DoSend<ClientLogout>(socket, logout);
            socket.Close();

            if(HeartbeatThread!=null)
                HeartbeatThread.Abort();
        }

        protected  override void OnOpen(IWebSocketConnection socket)
        {
            try
            {
                if (!Connected)
                {
                    DoLog(string.Format("Connecting for the first time to client {0}", socket.ConnectionInfo.ClientPort), MessageType.Information);

                    Connected = true;

                    DoLog(string.Format("Connected for the first time to client {0}", socket.ConnectionInfo.ClientPort), MessageType.Information);

                }
                else
                {
                    DoLog(string.Format("Closing previous client "), MessageType.Information);

                    DoClose(); //We close previous client
                }
            }
            catch (Exception ex)
            {
                if (socket != null && socket.ConnectionInfo.ClientPort != null && socket.ConnectionInfo != null)
                    DoLog(string.Format("Exception at  OnOpen for client {0}: {1}", socket.ConnectionInfo.ClientPort, ex.Message), MessageType.Error);
                else
                    DoLog(string.Format("Exception at  OnOpen for unknown client {0}", ex.Message), MessageType.Error);
            }
        }

        protected override void OnClose(IWebSocketConnection socket)
        {
            try
            {
                DoLog(string.Format(" OnClose for client {0}", socket.ConnectionInfo.ClientPort), MessageType.Information);
                Connected = false;
                DoClose(); 
            }
            catch (Exception ex)
            {
                if (socket != null && socket.ConnectionInfo != null && socket.ConnectionInfo.ClientPort != null)
                    DoLog(string.Format("Exception at  OnClose for client {0}: {1}", socket.ConnectionInfo.ClientPort, ex.Message), MessageType.Error);
                else
                    DoLog(string.Format("Exception at  OnClose for unknown client: {0}", ex.Message), MessageType.Error);

            }
        }


        protected override void OnMessage(IWebSocketConnection socket, string m)
        {
            try
            {
                WebSocketMessageV2 wsResp = JsonConvert.DeserializeObject<WebSocketMessageV2>(m);

                DoLog(string.Format("OnMessage {1} from IP -> {0}", socket.ConnectionInfo.ClientIpAddress, wsResp.Msg), MessageType.Information);

                if (wsResp.Msg == "ClientLoginRequest")
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

                    //ProcessSubscriptions(socket, m);

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
