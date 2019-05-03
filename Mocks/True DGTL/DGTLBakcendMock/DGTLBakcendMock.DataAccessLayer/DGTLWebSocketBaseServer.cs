using DGTLBackendMock.BusinessEntities;
using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account;
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

        protected Dictionary<string, Thread> ProcessDailySettlementThreads { get; set; }

        protected Dictionary<string, Thread> ProcessDailyOfficialFixingPriceThreads { get; set; }

        public int HeartbeatSeqNum { get; set; }

        public bool UserLogged { get; set; }

        protected ILogSource Logger;

        protected UserRecord[] UserRecords { get; set; }

        protected DailySettlementPrice[] DailySettlementPrices { get; set; }

        protected OfficialFixingPrice[] OfficialFixingPrices { get; set; }

        protected SecurityMasterRecord[] SecurityMasterRecords { get; set; }

        protected DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate[] CreditRecordUpdates { get; set; }

        protected AccountRecord[] AccountRecords { get; set; }

        protected object tLock = new object();

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
            string strMsg = JsonConvert.SerializeObject(entity, Newtonsoft.Json.Formatting.None,
                              new JsonSerializerSettings
                              {
                                  NullValueHandling = NullValueHandling.Ignore
                              });

            socket.Send(strMsg);

        }

        protected void LoadUserRecords()
        {
            string strUserRecords = File.ReadAllText(@".\input\UserRecord.json");

            //Aca le metemos que serialize el contenido
            UserRecords = JsonConvert.DeserializeObject<UserRecord[]>(strUserRecords);
        }

        protected void LoadDailySettlementPrices()
        {
            string strDaylySettlementPrices = File.ReadAllText(@".\input\DailySettlementPrice.json");

            //Aca le metemos que serialize el contenido
            DailySettlementPrices = JsonConvert.DeserializeObject<DailySettlementPrice[]>(strDaylySettlementPrices);
        }

        protected void LoadOfficialFixingPrices()
        {
            string strOfficialFixingPrices = File.ReadAllText(@".\input\OfficialFixingPrice.json");

            //Aca le metemos que serialize el contenido
            OfficialFixingPrices = JsonConvert.DeserializeObject<OfficialFixingPrice[]>(strOfficialFixingPrices);
        }

        protected void LoadCreditRecordUpdates()
        {
            string strCreditRecordUpdate = File.ReadAllText(@".\input\CreditRecordUpdate.json");

            //Aca le metemos que serialize el contenido
            CreditRecordUpdates = JsonConvert.DeserializeObject<DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate[]>(strCreditRecordUpdate);
        }

        protected void LoadAccountRecords()
        {
            string strAccountRecords = File.ReadAllText(@".\input\AccountRecord.json");

            //Aca le metemos que serialize el contenido
            AccountRecords = JsonConvert.DeserializeObject<AccountRecord[]>(strAccountRecords);
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
                UserLogged = true;
                DoSend<ClientLoginResponse>(socket, resp);
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
                DoSend<ClientReject>(socket, reject);
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


            DoSend<ClientLogoutResponse>(socket, logout);
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

            DoLog(string.Format("SubscriptionResponse UUID:{0} Service:{1} ServiceKey:{2} Success:{3}", resp.UUID,resp.Service,resp.ServiceKey,resp.Success), MessageType.Information);
            DoSend<SubscriptionResponse>(socket, resp);
        }


        private void DailySettlementThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    DailySettlementPrice dailySettl = DailySettlementPrices.Where(x => x.Symbol == subscrMsg.ServiceKey).FirstOrDefault();
                    if (dailySettl != null)
                    {
                        DoSend<DailySettlementPrice>(socket, dailySettl);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "FP", subscrMsg.ServiceKey, subscrMsg.UUID);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Daily Settlement Price not found for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing daily settlement price message: {0}...", ex.Message), MessageType.Error);

            }
        }

        private void EvalDailyOfficialFixingPriceWarnings(string symbol)
        {
            SecurityMasterRecord security = SecurityMasterRecords.Where(x => x.Symbol == symbol).FirstOrDefault();

            if (security.AssetClass == Security._SPOT)
            {
                //WE shouldn't have this service requested for a spot currency
                DoLog(string.Format("WARNING1 - Daily Official Fixing Price requested for spot currency! : {0}", symbol), MessageType.Error);
            }

        }

        private void EvalDailySettlementPriceWarnings(string symbol)
        {
            SecurityMasterRecord security = SecurityMasterRecords.Where(x => x.Symbol == symbol).FirstOrDefault();

            if (security.AssetClass == Security._SPOT)
            {
                //WE shouldn't have this service requested for a spot currency
                DoLog(string.Format("WARNING2 - Daily Settlement Price requested for spot currency! : {0}", symbol), MessageType.Error);
            }

        }

        protected void DailyOfficialFixingPriceThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    OfficialFixingPrice officialFixingPrice = OfficialFixingPrices.Where(x => x.Symbol == subscrMsg.ServiceKey).FirstOrDefault();
                    if (officialFixingPrice != null)
                    {
                        DoSend<OfficialFixingPrice>(socket, officialFixingPrice);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "FD", subscrMsg.ServiceKey, subscrMsg.UUID);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Official Fixing Price not found for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing daily settlement price message: {0}...", ex.Message), MessageType.Error);

            }
        }

        protected void ProcessDailySettlement(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            EvalDailySettlementPriceWarnings(subscrMsg.ServiceKey);


            if (!ProcessDailySettlementThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {
                    Thread ProcessDailySettlementThread = new Thread(DailySettlementThread);
                    ProcessDailySettlementThread.Start(new object[] { socket, subscrMsg });
                    ProcessDailySettlementThreads.Add(subscrMsg.ServiceKey, ProcessDailySettlementThread);
                }
            }

        }

        protected void ProcessOficialFixingPrice(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            EvalDailyOfficialFixingPriceWarnings(subscrMsg.ServiceKey);
            if (!ProcessDailyOfficialFixingPriceThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {
                    Thread ProcessDailyOfficialFixingPriceThread = new Thread(DailyOfficialFixingPriceThread);
                    ProcessDailyOfficialFixingPriceThread.Start(new object[] { socket, subscrMsg });
                    ProcessDailyOfficialFixingPriceThreads.Add(subscrMsg.ServiceKey, ProcessDailyOfficialFixingPriceThread);
                }
            }
        }

        protected void ProcessAccountRecord(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            if (subscrMsg.ServiceKey != "*")
            {
                if (subscrMsg.ServiceKey.EndsWith("@*"))
                    subscrMsg.ServiceKey = subscrMsg.ServiceKey.Replace("@*", "");

                List<AccountRecord> accountRecords = AccountRecords.Where(x => x.EPFirmId == subscrMsg.ServiceKey).ToList();

                accountRecords.ForEach(x => DoSend<AccountRecord>(socket, x));
            }
            else
                AccountRecords.ToList().ForEach(x => DoSend<AccountRecord>(socket, x));

            ProcessSubscriptionResponse(socket, "TD", subscrMsg.ServiceKey, subscrMsg.UUID);
        }

        protected void ProcessCreditRecordUpdates(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            if (subscrMsg.ServiceKey != "*")
            {
                if (subscrMsg.ServiceKey.EndsWith("@*"))
                    subscrMsg.ServiceKey = subscrMsg.ServiceKey.Replace("@*", "");

                DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate creditRecordUpdate = CreditRecordUpdates.Where(x => x.FirmId == subscrMsg.ServiceKey).FirstOrDefault();

                if (creditRecordUpdate != null)
                    DoSend<DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate>(socket, creditRecordUpdate);
            }


            ProcessSubscriptionResponse(socket, "CU", subscrMsg.ServiceKey, subscrMsg.UUID);
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
                    
                        DoSend<ClientHeartbeatRequest>(socket, heartbeatReq);
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
