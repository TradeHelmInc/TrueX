using DGTLBackendMock.BusinessEntities;
using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.Platform;
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
        #region Protected Static Consts

        protected static string _TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE1NTEzODY5NjksImV4cCI";

        #endregion


        #region Protected Attributes

        protected bool Connected { get; set; }

        protected List<int> ConnectedClients = new List<int>();

        protected Thread HeartbeatThread { get; set; }

        protected string URL { get; set; }

        protected string RESTURL { get; set; }

        protected Fleck.WebSocketServer WebSocketServer { get; set; }

        protected Dictionary<string, Thread> ProcessDailySettlementThreads { get; set; }

        protected Dictionary<string, Thread> ProcessDailyOfficialFixingPriceThreads { get; set; }

        protected Dictionary<string, Thread> ProcessCreditLimitUpdatesThreads { get; set; }

        public int HeartbeatSeqNum { get; set; }

        public bool UserLogged { get; set; }

        protected ILogSource Logger;

        protected string AccountRecordsFirmId { get; set; }

        protected UserRecord[] UserRecords { get; set; }

        protected DailySettlementPrice[] DailySettlementPrices { get; set; }

        protected OfficialFixingPrice[] OfficialFixingPrices { get; set; }

        protected SecurityMasterRecord[] SecurityMasterRecords { get; set; }

        protected DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate[] CreditRecordUpdates { get; set; }

        protected AccountRecord[] AccountRecords { get; set; }

        protected PlatformStatus PlatformStatus { get; set; }

        protected object tLock = new object();

        protected IWebSocketConnection ConnectionSocket { get; set; }

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

        protected void LoadPlatformStatus()
        {
            string strPlatformStatus = File.ReadAllText(@".\input\PlatformStatus.json");

            //Aca le metemos que serialize el contenido
            PlatformStatus = JsonConvert.DeserializeObject<PlatformStatus>(strPlatformStatus);
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            PlatformStatus.StatusTime = Convert.ToInt64(elapsed.TotalSeconds);
        }

     

        protected void ProcessPlatformStatus(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {

            bool doLogout = false;
            if (PlatformStatus.cState == PlatformStatus._STATE_SEND_CLIENT_LOGOUT)
            {
                DoLog(string.Format("Sending state open because of status 7 <will send ClientLogoutResponse>"), MessageType.Information);
                PlatformStatus.cState = PlatformStatus._STATE_OPEN;
                doLogout = true;
            }
            else
                DoLog(string.Format("Senting platform status {0}", PlatformStatus.cState), MessageType.Information);



            DoSend<PlatformStatus>(socket, PlatformStatus);
            ProcessSubscriptionResponse(socket, "PS", subscrMsg.ServiceKey, subscrMsg.UUID, true);

            if (doLogout)
            {
                PlatformStatus.cState = PlatformStatus._STATE_SEND_CLIENT_LOGOUT;
                DoLog(string.Format("Sleeping before Returning ClientLogoutResponse..."), MessageType.Information);
                Thread.Sleep(10 * 1000);
                DoLog(string.Format("Returning ClientLogoutResponse..."), MessageType.Information);
                ClientLogoutResponse logout = new ClientLogoutResponse()
                {
                    Msg = "ClientLogoutResponse",
                    Sender = 0,
                    Time = 0,
                    UserId = subscrMsg.UserId,
                    ReLogin = false
                };

                DoSend<ClientLogoutResponse>(socket, logout);
                DoLog(string.Format(" ClientLogoutResponse sent..."), MessageType.Information);
            }
        }

        protected virtual void ProcessClientLoginMock(IWebSocketConnection socket, string m)
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
                    JsonWebToken = _TOKEN
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
                        //officialFixingPrice.Price += Convert.ToDecimal( DateTime.Now.Second )/ 100;
                        DoLog(string.Format("Returning fixing price for symbol {0}:{1}...", subscrMsg.ServiceKey,officialFixingPrice.Price), MessageType.Information);

                        DoSend<OfficialFixingPrice>(socket, officialFixingPrice);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "FD", subscrMsg.ServiceKey, subscrMsg.UUID);
                            Thread.Sleep(2000);
                            subscResp = true;
                            return;
                            
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
            else
            {
                DoLog(string.Format("Double subscription for service FD for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                ProcessSubscriptionResponse(socket, "FD", subscrMsg.ServiceKey, subscrMsg.UUID, false, "Double subscription");
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
            else
            {
                DoLog(string.Format("Double subscription for service FP for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                ProcessSubscriptionResponse(socket, "FP", subscrMsg.ServiceKey, subscrMsg.UUID, false, "Double subscription");

            }
        }

        protected void ProcessAccountRecord(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            string prevServiceKey = subscrMsg.ServiceKey;
            if (subscrMsg.ServiceKey != "*")
            {
                if (subscrMsg.ServiceKey.EndsWith("@*"))
                    subscrMsg.ServiceKey = subscrMsg.ServiceKey.Replace("@*", "");

                AccountRecordsFirmId = subscrMsg.ServiceKey;

                List<AccountRecord> accountRecords = AccountRecords.Where(x => x.EPFirmId == subscrMsg.ServiceKey).ToList();

                accountRecords.ForEach(x => DoSend<AccountRecord>(socket, x));
            }
            else
                AccountRecords.ToList().ForEach(x => DoSend<AccountRecord>(socket, x));

            ProcessSubscriptionResponse(socket, "TD", prevServiceKey, subscrMsg.UUID);
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

        protected void ProcessCreditLimitUpdatesThread(object param)
        {
            DoLog(string.Format("Starting ProcessCreditLimitUpdatesThread thread"), MessageType.Information);

            object[] parameters = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection) parameters[0];
            WebSocketSubscribeMessage subscrMsg = (WebSocketSubscribeMessage)parameters[1];

            string firmId = subscrMsg.ServiceKey;
            if (subscrMsg.ServiceKey.Contains("@"))
                firmId = subscrMsg.ServiceKey.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries)[0];

            DoLog(string.Format("Getting account record for firmId {0}", firmId), MessageType.Information);
            AccountRecord accRecord = AccountRecords.Where(x => x.EPFirmId == firmId).FirstOrDefault();
            DoLog(string.Format("Account record for firmId {0} {1} found",firmId ,accRecord!=null? "do": "not"), MessageType.Information);

            if (accRecord == null)
                return;

            decimal maxNotional = accRecord.MaxNotional;
            double creditLimit = accRecord.CreditLimit;

            while (true)
            {
                Thread.Sleep(10000);
                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

                creditLimit += 1000d;
                maxNotional+=10000;

                CreditLimitUpdate climUpd = new CreditLimitUpdate();
                climUpd.Active = true;
                climUpd.CreditLimit = creditLimit;
                climUpd.FirmId = accRecord.EPFirmId;
                climUpd.MaxNotional = maxNotional;
                climUpd.Msg = "CreditLimitUpdate";
                climUpd.RouteId = accRecord.RouteId;
                climUpd.Sender = 0;
                climUpd.Time = Convert.ToInt64(elapsed.TotalMilliseconds);


                DoLog(string.Format("Sending Credit Limit Update New MaxLimit:{0} New MaxNotional:{1}", creditLimit, maxNotional), MessageType.Information);
                DoSend<CreditLimitUpdate>(socket, climUpd);

            }
        
        }

        protected void ProcessCreditLimitUpdates(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {


            if (!ProcessCreditLimitUpdatesThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                ProcessSubscriptionResponse(socket, "Cm", subscrMsg.ServiceKey, subscrMsg.UUID);
                Thread CreditLimitUpdateThread = new Thread(ProcessCreditLimitUpdatesThread);
                CreditLimitUpdateThread.Start(new object[] { socket, subscrMsg });
                ProcessCreditLimitUpdatesThreads.Add(subscrMsg.ServiceKey, CreditLimitUpdateThread);

            }
            else
            {
                DoLog(string.Format("Double subscription for service Cm for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                ProcessSubscriptionResponse(socket, "Cm", subscrMsg.ServiceKey, subscrMsg.UUID, false, "Already subscribed");
            }
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

        //protected abstract void OnOpen(IWebSocketConnection socket);

        //protected abstract void OnClose(IWebSocketConnection socket);

        protected abstract void OnMessage(IWebSocketConnection socket, string m);

        protected abstract void DoClose();

        protected abstract void DoCLoseThread(object p);

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

        #region Protected Overriden Methods

        protected virtual  void OnOpen(IWebSocketConnection socket)
        {
            try
            {
                if (!Connected)
                {
                    DoLog(string.Format("Connecting for the first time to client {0}", socket.ConnectionInfo.ClientPort), MessageType.Information);
                    //socket.Send("Connection Opened");
                    ConnectedClients.Add(socket.ConnectionInfo.ClientPort);

                    HeartbeatThread = new Thread(ClientHeartbeatThread);
                    HeartbeatThread.Start(socket);

                    Connected = true;

                    ConnectionSocket = socket;

                    DoLog(string.Format("Connected for the first time to client {0}", socket.ConnectionInfo.ClientPort), MessageType.Information);


                }
                else
                {
                    DoLog(string.Format("Connecting second client {0}.  Removing previous client", socket.ConnectionInfo.ClientPort), MessageType.Information);

                    DoCLoseThread(null);

                    ConnectedClients.Add(socket.ConnectionInfo.ClientPort);

                    HeartbeatThread = new Thread(ClientHeartbeatThread);
                    HeartbeatThread.Start(socket);

                    Connected = true;

                    ConnectionSocket = socket;

                    DoLog(string.Format("Connected second client {0}.  Removing previous client", socket.ConnectionInfo.ClientPort), MessageType.Information);


                    //DoLog("Only 1 connection at a time allowed", MessageType.Error);
                    //socket.Send("Only 1 connection at a time allowed");
                }
            }
            catch (Exception ex)
            {
                if(socket !=null && socket.ConnectionInfo.ClientPort!=null && socket.ConnectionInfo!= null)
                    DoLog(string.Format("Exception at  OnOpen for client {0}: {1}", socket.ConnectionInfo.ClientPort, ex.Message), MessageType.Error);
                else
                    DoLog(string.Format("Exception at  OnOpen for unknown client {0}", ex.Message), MessageType.Error);


            }
        }

        protected virtual  void OnClose(IWebSocketConnection socket)
        {
            try
            {
                DoLog(string.Format(" OnClose for client {0}", socket.ConnectionInfo.ClientPort), MessageType.Information);

                if (ConnectedClients.Any(x => x == socket.ConnectionInfo.ClientPort))
                    DoClose();
            }
            catch (Exception ex)
            {
                if (socket!= null && socket.ConnectionInfo != null && socket.ConnectionInfo.ClientPort != null)
                    DoLog(string.Format("Exception at  OnClose for client {0}: {1}", socket.ConnectionInfo.ClientPort, ex.Message), MessageType.Error);
                else
                    DoLog(string.Format("Exception at  OnClose for unknown client: {0}", ex.Message), MessageType.Error);

            }
        }

        #endregion
    }
}
