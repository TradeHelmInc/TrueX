using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account;
using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Auth.V2;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.MarketData.V2;
using DGTLBackendMock.Common.DTO.OrderRouting;
using DGTLBackendMock.Common.DTO.OrderRouting.V2;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.SecurityList.V2;
using DGTLBackendMock.Common.DTO.Subscription;
using DGTLBackendMock.Common.DTO.Subscription.V2;
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
            NextOrderId = _INITIAL_ORDER_ID;
        }

        #endregion

        #region Protected Static Consts

        private static long _INITIAL_ORDER_ID = 10000;

        #endregion

        #region Protected Attributes

        protected ClientInstrumentBatch InstrBatch { get; set; }

        protected string LastTokenGenerated { get; set; }

        protected Thread HeartbeatThread { get; set; }

        protected bool SubscribedLQ { get; set; }

        protected long NextOrderId { get; set; }

        public string LoggedFirmId { get; set; }

        public string LoggedUserId { get; set; }

        #endregion

        #region Private and Protected Methods

        protected override void DoCLoseThread(object p)
        {
            lock (tLock)
            {

                ProcessLastSaleThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessLastSaleThreads.Clear();

                ProcessLastQuoteThreads.Values.ToList().ForEach(x => x.Abort());
                ProcessLastQuoteThreads.Clear();

                //ProcessDailyOfficialFixingPriceThreads.Values.ToList().ForEach(x => x.Abort());
                //ProcessDailyOfficialFixingPriceThreads.Clear();

                //ProcessDailySettlementThreads.Values.ToList().ForEach(x => x.Abort());
                //ProcessDailySettlementThreads.Clear();

                //ProcessSecuritStatusThreads.Values.ToList().ForEach(x => x.Abort());
                //ProcessSecuritStatusThreads.Clear();

                //ProcessCreditLimitUpdatesThreads.Values.ToList().ForEach(x => x.Abort());
                //ProcessCreditLimitUpdatesThreads.Clear();


                //NotificationsSubscriptions.Clear();
                //OpenOrderCountSubscriptions.Clear();

                Connected = false;

                DoLog("Turning threads off on socket disconnection", MessageType.Information);
            }
        }

        protected void ProcessUnsubscriptions(Subscribe subscrMsg)
        {
          
            lock (SecurityMappings)
            {

                if (subscrMsg.Service == "LS")
                {
                    if (ProcessLastSaleThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessLastSaleThreads[subscrMsg.ServiceKey].Abort();
                        ProcessLastSaleThreads.Remove(subscrMsg.ServiceKey);
                    }
                }
                else if (subscrMsg.Service == "LQ")
                {
                    if (ProcessLastQuoteThreads.ContainsKey(subscrMsg.ServiceKey))
                    {
                        ProcessLastQuoteThreads[subscrMsg.ServiceKey].Abort();
                        ProcessLastQuoteThreads.Remove(subscrMsg.ServiceKey);
                    }
                }
                //else if (subscrMsg.Service == "FP")
                //{
                //    if (ProcessDailyOfficialFixingPriceThreads.ContainsKey(subscrMsg.ServiceKey))
                //    {
                //        ProcessDailyOfficialFixingPriceThreads.Remove(subscrMsg.ServiceKey);
                //    }
                //}
                //else if (subscrMsg.Service == "TI")
                //{
                //    if (ProcessSecuritStatusThreads.ContainsKey(subscrMsg.ServiceKey))
                //    {
                //        ProcessSecuritStatusThreads[subscrMsg.ServiceKey].Abort();
                //        ProcessSecuritStatusThreads.Remove(subscrMsg.ServiceKey);
                //    }
                //}
                //else if (subscrMsg.Service == "Ot")
                //{
                //    if (OpenOrderCountSubscriptions.Contains(subscrMsg.ServiceKey))
                //        OpenOrderCountSubscriptions.Add(subscrMsg.ServiceKey);
                //}
                //else if (subscrMsg.Service == "TN")
                //{
                //    if (NotificationsSubscriptions.Contains(subscrMsg.ServiceKey))
                //        NotificationsSubscriptions.Add(subscrMsg.ServiceKey);
                //}
                //else if (subscrMsg.Service == "FD")
                //{
                //    if (ProcessDailySettlementThreads.ContainsKey(subscrMsg.ServiceKey))
                //    {
                //        ProcessDailySettlementThreads[subscrMsg.ServiceKey].Abort();
                //        ProcessDailySettlementThreads.Remove(subscrMsg.ServiceKey);
                //    }
                //}
                //else if (subscrMsg.Service == "Cm")
                //{
                //    if (ProcessCreditLimitUpdatesThreads.ContainsKey(subscrMsg.ServiceKey))
                //    {
                //        ProcessCreditLimitUpdatesThreads[subscrMsg.ServiceKey].Abort();
                //        ProcessCreditLimitUpdatesThreads.Remove(subscrMsg.ServiceKey);
                //    }
                //}
                //else if (subscrMsg.Service == "CU")
                //{
                //    //ProcessCreditRecordUpdates(socket, subscrMsg);
                //}
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
            int i = 1;

            List<ClientInstrument> instrList = new List<ClientInstrument>();
            foreach (SecurityMasterRecord security in SecurityMasterRecords)
            {

                ClientInstrument instrumentMsg = new ClientInstrument();
                instrumentMsg.Msg = "ClientInstrument";
                instrumentMsg.CreatedAt = Convert.ToInt64(epochElapsed.TotalMilliseconds);
                instrumentMsg.UpdatedAt = Convert.ToInt64(epochElapsed.TotalMilliseconds);
                instrumentMsg.LastUpdatedBy = "";
                instrumentMsg.ExchangeId = 0;
                instrumentMsg.Description = security.Description;
                instrumentMsg.InstrumentDate = security.MaturityDate;
                instrumentMsg.InstrumentId = i;
                instrumentMsg.InstrumentName = security.Symbol;
                instrumentMsg.LastUpdatedBy = "fernandom";
                instrumentMsg.LotSize = security.LotSize;
                instrumentMsg.MaxLotSize = Convert.ToDouble(security.MaxSize);
                instrumentMsg.MinLotSize = Convert.ToDouble(security.MinSize);
                instrumentMsg.cProductType = ClientInstrument.GetProductType(security.AssetClass);
                instrumentMsg.MinQuotePrice = security.MinPrice;
                instrumentMsg.MaxQuotePrice = security.MaxPrice;
                instrumentMsg.MinPriceIncrement = security.MinPriceIncrement;
                instrumentMsg.MaxNotionalValue = security.MaxPrice * security.LotSize;
                instrumentMsg.Currency1 = security.CurrencyPair;
                instrumentMsg.Currency2 = "";
                instrumentMsg.Test = false;
                //instrumentMsg.UUID = Uuid;

                security.InstrumentId = i;
                i++;

                //DoLog(string.Format("Sending Instrument "), MessageType.Information);
                //DoSend<Instrument>(socket, instrumentMsg);
                instrList.Add(instrumentMsg);
            }

            InstrBatch = new ClientInstrumentBatch() { Msg = "ClientInstrumentBatch", messages = instrList.ToArray() };
            DoLog(string.Format("Sending Instrument Batch "), MessageType.Information);
            DoSend<ClientInstrumentBatch>(socket, InstrBatch);
        
        }

        //Sending logged user
        private void SendCRMUsers(IWebSocketConnection socket, string login, string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            UserRecord userRecord =   UserRecords.Where(x=>x.UserId==login).FirstOrDefault();

            if (userRecord != null)
            {
                ClientUserRecord userRecordMsg = new ClientUserRecord();
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
                userRecordMsg.Msg = "ClientUserRecord";
                userRecordMsg.PostalCode = "";
                userRecordMsg.State = "";
                userRecordMsg.UserId = userRecord.UserId;
                //userRecordMsg.UUID = Uuid;

                DoLog(string.Format("Sending ClientUserRecord"), MessageType.Information);
                DoSend<ClientUserRecord>(socket, userRecordMsg);
            }
            else
                throw new Exception(string.Format("User not found {0}", login));
        }

        private void SendMarketStatus(IWebSocketConnection socket,string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);

            ClientMarketState marketStateMsg = new ClientMarketState();
            marketStateMsg.cExchangeId=ClientMarketState._DEFAULT_EXCHANGE_ID;
            marketStateMsg.cReasonCode='0';
            marketStateMsg.cState = ClientMarketState.TranslateV1StatesToV2States(PlatformStatus.cState);
            marketStateMsg.Msg = "ClientMarketState";
            marketStateMsg.StateTime = Convert.ToInt64(epochElapsed.TotalMilliseconds);
            //marketStateMsg.UUID = Uuid;


            ClientMarketStateBatch marketStateBatch = new ClientMarketStateBatch()
            {
                Msg = "ClientMarketStateBatch",
                messages = new ClientMarketState[] { marketStateMsg }

            };

            DoLog(string.Format("Sending ClientMarketStateBatch"), MessageType.Information);
            DoSend<ClientMarketStateBatch>(socket, marketStateBatch);
        }

        private void SendCRMAccounts(IWebSocketConnection socket, string login,string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            UserRecord userRecord = UserRecords.Where(x => x.UserId == login).FirstOrDefault();

            if (userRecord != null)
            {
                List<DGTLBackendMock.Common.DTO.Account.AccountRecord> accountRecords = AccountRecords.Where(x => x.EPFirmId == userRecord.FirmId).ToList();

                List<ClientAccountRecord> accRecordList = new List<ClientAccountRecord>();
                foreach (DGTLBackendMock.Common.DTO.Account.AccountRecord accountRecord in accountRecords)
                {
                    ClientAccountRecord accountRecordMsg = new ClientAccountRecord();
                    accountRecordMsg.Msg = "ClientAccountRecord";
                    accountRecordMsg.AccountId = accountRecord.AccountId;
                    accountRecordMsg.FirmId = userRecord.FirmId;
                    accountRecordMsg.SettlementFirmId = "1";
                    accountRecordMsg.AccountName = accountRecord.EPNickName;
                    accountRecordMsg.AccountAlias = accountRecord.AccountId;
                    
                    
                    accountRecordMsg.AccountNumber = "";
                    accountRecordMsg.AccountType = 0;
                    accountRecordMsg.cStatus = ClientAccountRecord._STATUS_ACTIVE;
                    accountRecordMsg.cUserType = ClientAccountRecord._DEFAULT_USER_TYPE;
                    accountRecordMsg.cCti = ClientAccountRecord._CTI_OTHER;
                    accountRecordMsg.Lei = "";
                    accountRecordMsg.Currency = "";
                    accountRecordMsg.IsSuspense = false;
                    accountRecordMsg.UsDomicile = true;
                    accountRecordMsg.UpdatedAt = 0;
                    accountRecordMsg.CreatedAt = 0;
                    accountRecordMsg.LastUpdatedBy = "";
                    accountRecordMsg.WalletAddress = "";
                    //accountRecordMsg.UUID = Uuid;

                    //DoLog(string.Format("Sending AccountRecord "), MessageType.Information);
                    //DoSend<ClientAccountRecord>(socket, accountRecordMsg);
                    accRecordList.Add(accountRecordMsg);
                }


                ClientAccountRecordBatch accRecordBatch = new ClientAccountRecordBatch()
                {
                    Msg = "ClientAccountRecordBatch",
                    messages = accRecordList.ToArray()
                };

                DoLog(string.Format("Sending ClientAccountRecordBatch "), MessageType.Information);
                DoSend<ClientAccountRecordBatch>(socket, accRecordBatch);
            }
            else
                throw new Exception(string.Format("User not found {0}", login));
        
        }

        private void SendCRMMessages(IWebSocketConnection socket,string login,string Uuid=null)
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

                UserRecord memUserRecord = UserRecords.Where(x => x.UserId == jsonCredentials.UserId).FirstOrDefault();
                LoggedUserId = memUserRecord.UserId;
                LoggedFirmId = memUserRecord.FirmId;

                ClientLoginResponse loginResp = new ClientLoginResponse()
                {
                    Msg = "ClientLoginResponse",
                    UUID = wsLogin.UUID,
                    JsonWebToken = LastTokenGenerated,
                    cSuccess = ClientLoginResponse._STATUS_OK,
                    Time = wsLogin.Time,
                    UserId = memUserRecord.UserId
                };

                DoLog(string.Format("Sending ClientLoginResponse with UUID {0}", loginResp.UUID), MessageType.Information);

                DoSend<ClientLoginResponse>(socket, loginResp);

                SendCRMMessages(socket, jsonCredentials.UserId);

                HeartbeatThread = new Thread(SendHeartbeat);
                HeartbeatThread.Start(new object[] { socket, loginResp.JsonWebToken, loginResp.UUID });
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception unencrypting secret {0}: {1}", wsLogin.Secret, ex.Message), MessageType.Error);
                SendLoginRejectReject(socket, wsLogin,string.Format("Exception unencrypting secret {0}: {1}", wsLogin.Secret, ex.Message));
            }
        }

        private bool ProcessRejectionsForNewOrders(ClientOrderReq clientOrderReq, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            //TODO : Conseguir InstrumentId de instrumento para rechazar
            if (clientOrderReq.cSide == ClientOrderReq._SIDE_BUY && clientOrderReq.InstrumentId == _REJECTED_SECURITY_ID && clientOrderReq.Price.Value < 6000)
            {


                ClientOrderResponse clientOrdAck = new ClientOrderResponse()
                {
                    Msg = "ClientOrderResponse",
                    ClientOrderId = clientOrderReq.ClientOrderId,
                    InstrumentId = clientOrderReq.InstrumentId,
                    Message = string.Format("Invalid Order for security id {0}", clientOrderReq.InstrumentId),
                    Success = false,
                    OrderId = NextOrderId,
                    UserId = clientOrderReq.UserId,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    UUID = clientOrderReq.UUID
                };
                DoLog(string.Format("Sending ClientOrderResponse rejected ..."), MessageType.Information);
                DoSend<ClientOrderResponse>(socket, clientOrdAck);

                ////We reject the messageas a convention, we cannot send messages lower than 6000 USD
                //ClientOrderRej reject = new ClientOrderRej()
                //{
                //    Msg = "ClientOrderRej",
                //    cRejectCode='0',
                //    ExchangeId=0,
                //    UUID=clientOrderReq.UUID,
                //    TransactionTimes=Convert.ToInt64(elapsed.TotalMilliseconds)

                //};

                //DoSend<ClientOrderRej>(socket, reject);

                return true;
            }

            return false;

        }

        protected void ProcessLegacyOrderReqMock(IWebSocketConnection socket, string m)
        {
            try
            {
                lock (Orders)
                {
                    TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                    ClientOrderReq clientOrderReq = JsonConvert.DeserializeObject<ClientOrderReq>(m);

                    if (!ProcessRejectionsForNewOrders(clientOrderReq, socket))
                    {

                        ClientOrderResponse clientOrdAck = new ClientOrderResponse()
                        {
                            Msg = "ClientOrderResponse",
                            ClientOrderId = clientOrderReq.ClientOrderId,
                            InstrumentId = clientOrderReq.InstrumentId,
                            Message = "success",
                            Success = true,
                            OrderId = NextOrderId,
                            UserId=clientOrderReq.UserId,
                            Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                            UUID = clientOrderReq.UUID
                        };
                        DoLog(string.Format("Sending ClientOrderResponse ..."), MessageType.Information);
                        DoSend<ClientOrderResponse>(socket, clientOrdAck);
                        NextOrderId++;

                        //We send the mock ack
                        //ClientOrderAck clientOrdAck = new ClientOrderAck()
                        //{
                        //    Msg = "ClientOrderAck",
                        //    ClientOrderId = clientOrderReq.ClientOrderId,
                        //    OrderId = Guid.NewGuid().ToString(),
                        //    TransactionTime = Convert.ToInt64(elapsed.TotalMilliseconds),
                        //    UUID = clientOrderReq.UUID,
                        //    UserId = clientOrderReq.UserId.ToString()
                        //};

                        //DoLog(string.Format("Sending ClientOrderAck ..."), MessageType.Information);
                        //DoSend<ClientOrderAck>(socket, clientOrdAck);


                        //TODO: Implement this when the order book is implementd
                        //if (!EvalTrades(legOrdReq, socket))
                        //{
                        //    DoLog(string.Format("Evaluating price levels ..."), MessageType.Information);
                        //    EvalPriceLevelsIfNotTrades(socket, legOrdReq);
                        //    DoLog(string.Format("Evaluating LegacyOrderRecord ..."), MessageType.Information);
                        //    EvalNewOrder(socket, legOrdReq, LegacyOrderRecord._STATUS_OPEN, 0);
                        //    DoLog(string.Format("Updating quotes ..."), MessageType.Information);
                        //    UpdateQuotes(socket, legOrdReq.InstrumentId);
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Exception processing ClientOrderReq: {0}", ex.Message), MessageType.Error);
            }


        }



        #endregion

        #region Private Static Consts

        private static int _REJECTED_SECURITY_ID = 1;

        #endregion

        #region Protected Methods

        protected ClientInstrument GetInstrumentBySymbol(string symbol)
        {
            return  InstrBatch.messages.Where(x => x.InstrumentName == symbol).FirstOrDefault();
        }

        protected ClientInstrument GetInstrumentByServiceKey(string serviceKey)
        {
            if (InstrBatch == null)
                throw new Exception("Initial load for instrument not finished!");

            int filterIntrId=0;
            try
            {
                filterIntrId = Convert.ToInt32(serviceKey);
            }
            catch(Exception ex)
            {
                throw new Exception(string.Format("Wrong format for instrumentId (serviceKey): {}. The instrumentId has to be an integer",serviceKey));
            }



            ClientInstrument instr  = InstrBatch.messages.Where(x => x.InstrumentId == filterIntrId).FirstOrDefault();
            return instr;
        }

        protected void ProcessSubscriptionResponse(IWebSocketConnection socket, string service, string serviceKey, string UUID, bool success = true, string msg = "success")
        {
            DGTLBackendMock.Common.DTO.Subscription.V2.SubscriptionResponse resp = new DGTLBackendMock.Common.DTO.Subscription.V2.SubscriptionResponse()
            {
                Message = msg,
                Success = success,
                Service = service,
                ServiceKey = serviceKey,
                UUID = UUID,
                Msg = "SubscriptionResponse"

            };

            DoLog(string.Format("SubscriptionResponse UUID:{0} Service:{1} ServiceKey:{2} Success:{3}", resp.UUID, resp.Service, resp.ServiceKey, resp.Success), MessageType.Information);
            DoSend<DGTLBackendMock.Common.DTO.Subscription.V2.SubscriptionResponse>(socket, resp);
        }

        private void LastSaleThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            Subscribe subscrMsg = (Subscribe)paramArray[1];
            ClientInstrument instr = (ClientInstrument)paramArray[2];

            bool subscResp = false;
      
            try
            {
                int i = 0;
                while (true)
                {
                    LastSale legacyLastSale = LastSales.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault();
                    TranslateAndSendOldSale(socket, subscrMsg.UUID, legacyLastSale, instr);
                    Thread.Sleep(3000);//3 seconds
                    if (!subscResp)
                    {
                        ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.UUID);
                        Thread.Sleep(2000);
                        subscResp = true;
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing last sales message: {0}...", ex.Message), MessageType.Error);
            }
        }

        private void TranslateAndSendOldSale(IWebSocketConnection socket, string UUID, LastSale legacyLastSale, ClientInstrument instr)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            if (legacyLastSale != null)
            {
                ClientLastSale lastSale = new ClientLastSale()
                {
                    Msg = "ClientLastSale",
                    Change = legacyLastSale.Change,
                    High = legacyLastSale.High,
                    InstrumentId = instr.InstrumentId,
                    LastPrice = legacyLastSale.LastPrice,
                    LastSize = legacyLastSale.LastShares,
                    Low = legacyLastSale.Low,
                    Open = legacyLastSale.Open,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    UUID = UUID,
                    Volume = legacyLastSale.Volume

                };

                //EmulatePriceChanges(i, lastSale, ref initialPrice);
                DoSend<ClientLastSale>(socket, lastSale);
            }
            else
            {
                ClientLastSale lastSale = new ClientLastSale()
                {
                    Msg = "ClientLastSale",
                    InstrumentId = instr.InstrumentId,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    UUID = UUID,
                };

                DoSend<ClientLastSale>(socket, lastSale);
            }
        }


        private void TranslateAndSendOldLegacyTradeHistory(IWebSocketConnection socket, string UUID, LegacyTradeHistory legacyTradeHistory)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            if (legacyTradeHistory != null)
            {
                ClientInstrument instr = GetInstrumentBySymbol(legacyTradeHistory.Symbol);
                ClientTradeRecord trade = new ClientTradeRecord()
                {
                    Msg = "ClientTradeRecord",
                    ClientOrderId = null,
                    CreateTimeStamp = legacyTradeHistory.TradeTimeStamp,
                    cSide = legacyTradeHistory.cMySide,
                    cStatus = ClientTradeRecord._STATUS_OPEN,
                    ExchangeFees = 0.005 * (legacyTradeHistory.TradePrice * legacyTradeHistory.TradeQuantity),
                    FirmId = Convert.ToInt64(LoggedFirmId),
                    InstrumentId = instr.InstrumentId,
                    Notional = (legacyTradeHistory.TradePrice * legacyTradeHistory.TradeQuantity),
                    OrderId = 0,
                    TimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    TradePrice = legacyTradeHistory.TradePrice,
                    TradeQty = legacyTradeHistory.TradeQuantity,
                    UserId = 0,
                    UUID = UUID
                };

                DoSend<ClientTradeRecord>(socket, trade);
            }
        }

        private void TranslateAndSendOldLegacyOrderRecord(IWebSocketConnection socket, string UUID, LegacyOrderRecord legacyOrderRecord)
        {
            TimeSpan startFromToday = DateTime.Now.Date - new DateTime(1970, 1, 1);
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            if (legacyOrderRecord != null)
            {
                ClientInstrument instr = GetInstrumentBySymbol(legacyOrderRecord.InstrumentId);
                ClientOrderRecord order = new ClientOrderRecord()
                {
                    Msg = "ClientOrderRecord",
                    AveragePrice = legacyOrderRecord.Price,
                    ClientOrderId = legacyOrderRecord.ClientOrderId,
                    CreateTimeStamp = Convert.ToInt64(startFromToday.TotalMilliseconds),
                    cSide = legacyOrderRecord.cSide,
                    cStatus = legacyOrderRecord.cStatus,//Both systems V1 and V2 keep the same status
                    CumQty = legacyOrderRecord.FillQty,
                    ExchageFees = 0,
                    FirmId = Convert.ToInt64(LoggedFirmId),
                    UserId = 0,
                    InstrumentId = instr.InstrumentId,
                    LeavesQty = legacyOrderRecord.LvsQty,
                    Message = "",
                    Notional = legacyOrderRecord.Price.HasValue ? legacyOrderRecord.Price.Value * legacyOrderRecord.OrdQty : 0,
                    OrderId = GUIDToLongConverter.GUIDToLong(legacyOrderRecord.OrderId),
                    Price = legacyOrderRecord.Price,
                    Quantity = legacyOrderRecord.OrdQty,
                    TimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    
                    UUID = UUID
                };

                DoSend<ClientOrderRecord>(socket, order);
            }
        }

        private void TranslateAndSendOldQuote(IWebSocketConnection socket, string UUID, Quote legacyLastQuote, ClientInstrument instr)
        {
            if (legacyLastQuote != null)
            {
                ClientBestBidOffer cBidOffer = new ClientBestBidOffer()
                {
                    Msg = "ClientBestBidOffer",
                    Ask = legacyLastQuote.Ask,
                    AskSize = legacyLastQuote.AskSize,
                    Bid = legacyLastQuote.Bid,
                    BidSize = legacyLastQuote.BidSize,
                    InstrumentId = instr.InstrumentId,
                    MidPrice = legacyLastQuote.MidPoint,
                    UUID = UUID,
                };

                DoSend<ClientBestBidOffer>(socket, cBidOffer);
            }
            else
            {
                ClientBestBidOffer cBidOffer = new ClientBestBidOffer()
                {
                    Msg = "ClientBestBidOffer",
                    InstrumentId = instr.InstrumentId,
                    UUID = UUID,
                };

                DoSend<ClientBestBidOffer>(socket, cBidOffer);
            }
        }

        private void TranslateAndSendOldDepthOfBook(IWebSocketConnection socket, DepthOfBook legacyDepthOfBook, ClientInstrument instr,string UUID)
        {
            ClientDepthOfBook depthOfBook = new ClientDepthOfBook()
            {
                Msg = "ClientDepthOfBook",
                cAction = legacyDepthOfBook.cAction,
                cSide = legacyDepthOfBook.cBidOrAsk,
                InstrumentId = instr.InstrumentId,
                Price = legacyDepthOfBook.Price,
                Size = legacyDepthOfBook.Size,
                UUID=UUID
                
            };

            DoSend<ClientDepthOfBook>(socket, depthOfBook);
        }

        private void TranslateAndSendOldCreditRecordUpdate(IWebSocketConnection socket, CreditRecordUpdate creditRecordUpdate,
                                                           DGTLBackendMock.Common.DTO.Account.AccountRecord defaultAccount, string UUID = null)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            ClientCreditUpdate ccUpd = new ClientCreditUpdate()
            {
                Msg = "ClientCreditUpdate",
                AccountId = Convert.ToInt64(defaultAccount.AccountId),
                CreditLimit = defaultAccount.CreditLimit,
                CreditUsed = creditRecordUpdate.CreditUsed,
                cStatus = ClientCreditUpdate._SEC_STATUS_TRADING,
                cUpdateReason = ClientCreditUpdate._UPDATE_REASON_DEFAULT,
                FirmId = Convert.ToInt32(creditRecordUpdate.FirmId),
                MaxNotional = defaultAccount.MaxNotional,
                Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                UUID = UUID
            };

            DoSend<ClientCreditUpdate>(socket, ccUpd);
        }

        private void QuoteThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            Subscribe subscrMsg = (Subscribe)paramArray[1];
            ClientInstrument instr = (ClientInstrument)paramArray[2];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    Quote legacyLastQuote  = Quotes.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault();

                    TranslateAndSendOldQuote(socket, subscrMsg.UUID, legacyLastQuote, instr);
                    Thread.Sleep(3000);//3 seconds
                    if (!subscResp)
                    {
                        ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID);
                        Thread.Sleep(2000);
                        subscResp = true;
                        SubscribedLQ = true;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing quote message: {0}...", ex.Message), MessageType.Error);
            }
        }

        private void NewQuoteThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            ClientInstrument instr = (ClientInstrument)paramArray[1];
            string UUID = (string)paramArray[2];

            try
            {
                while (true)
                {
                    Quote quote = null;
                    lock (Quotes)
                    {
                        DoLog(string.Format("Searching quotes for symbol {0}. ", instr.InstrumentName), MessageType.Information);
                        quote = Quotes.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault();
                    }

                    if (quote != null)
                    {
                        DoLog(string.Format("Sending LQ for symbol {0}. Best bid={1} Best ask={2}", instr.InstrumentName, quote.Bid, quote.Ask), MessageType.Information);

                        TranslateAndSendOldQuote(socket, UUID, quote,instr);
                        DoSend<Quote>(socket, quote);

                    }
                    else
                    {
                        DoLog(string.Format("quotes for symbol {0} not found ", instr.InstrumentName), MessageType.Information);
                    }
                    Thread.Sleep(3000);//3 seconds
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing quote message for symbol {1}: {0}...", ex.Message, instr.InstrumentName), MessageType.Error);
            }
        }

        protected void ProcessLastSale(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            if (!ProcessLastSaleThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {

                    try
                    {
                        ClientInstrument instr = GetInstrumentByServiceKey(subscrMsg.ServiceKey);
                        Thread ProcessLastSaleThread = new Thread(LastSaleThread);
                        ProcessLastSaleThread.Start(new object[] { socket, subscrMsg, instr });
                        ProcessLastSaleThreads.Add(subscrMsg.ServiceKey, ProcessLastSaleThread);
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format(ex.Message), MessageType.Error);
                        ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
                    }
                }
            }
            else
            {
                DoLog(string.Format("Double subscription for service LS for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.UUID, false, "Double subscription");

            }

        }

        protected void ProcessQuote(IWebSocketConnection socket, Subscribe subscrMsg)
        {

            if (!ProcessLastQuoteThreads.ContainsKey(subscrMsg.ServiceKey))
            {
                lock (tLock)
                {
                    try
                    {
                        ClientInstrument instr = GetInstrumentByServiceKey(subscrMsg.ServiceKey);
                        Thread ProcessQuoteThread = new Thread(QuoteThread);
                        ProcessQuoteThread.Start(new object[] { socket, subscrMsg, instr });
                        ProcessLastQuoteThreads.Add(subscrMsg.ServiceKey, ProcessQuoteThread);
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format(ex.Message), MessageType.Error);
                        ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
                    }
                }
            }
            else
            {
                DoLog(string.Format("Double subscription for service LQ for symbol {0}...", subscrMsg.ServiceKey), MessageType.Information);
                ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID, false, "Double subscription");
            }
        }

        protected void UpdateQuotes(IWebSocketConnection socket, ClientInstrument instr, string UUID = null)
        {

            DoLog(string.Format("Updating best bid and ask for symbol {0}", instr.InstrumentName), MessageType.Information);
            DepthOfBook bestBid = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName && x.cBidOrAsk == DepthOfBook._BID_ENTRY).OrderByDescending(x => x.Price).FirstOrDefault();
            DepthOfBook bestAsk = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).OrderBy(x => x.Price).FirstOrDefault();

            lock (Quotes)
            {
                Quote quote = Quotes.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault();

                if (quote != null)
                {
                    if (bestBid != null)
                    {
                        quote.BidSize = bestBid.Size;
                        quote.Bid = bestBid.Price;
                    }
                    else
                    {
                        DoLog(string.Format("Erasing best bid for symbol {0}", instr.InstrumentName), MessageType.Information);
                        quote.BidSize = null;
                        quote.Bid = null;
                    }

                    if (bestAsk != null)
                    {
                        quote.AskSize = bestAsk.Size;
                        quote.Ask = bestAsk.Price;
                    }
                    else
                    {
                        DoLog(string.Format("Erasing best ask for symbol {0}", instr.InstrumentName), MessageType.Information);
                        quote.AskSize = null;
                        quote.Ask = null;
                    }


                    quote.RefreshMidPoint(SecurityMasterRecords.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault().MinPriceIncrement);

                }
                else
                {
                    Quote newQuote = new Quote()
                    {
                        Msg = "Quote",
                        Symbol = instr.InstrumentName
                    };

                    if (bestAsk != null)
                    {
                        newQuote.AskSize = bestAsk.Size;
                        newQuote.Ask = bestAsk.Price;
                    }


                    if (bestBid != null)
                    {
                        newQuote.BidSize = bestBid.Size;
                        newQuote.Bid = bestBid.Price;
                    }

                    newQuote.RefreshMidPoint(SecurityMasterRecords.Where(x => x.Symbol == instr.InstrumentName).FirstOrDefault().MinPriceIncrement);

                    DoLog(string.Format("Inserting quotes for symbol {0}", instr.InstrumentName), MessageType.Information);
                    Quotes.Add(newQuote);
                    DoLog(string.Format("Quotes for symbol {0} inserted", instr.InstrumentName), MessageType.Information);

                    Thread ProcessQuoteThread = new Thread(NewQuoteThread);
                    ProcessQuoteThread.Start(new object[] { socket, instr, UUID });

                    if (!ProcessLastQuoteThreads.ContainsKey(instr.InstrumentName))
                        ProcessLastQuoteThreads.Add(instr.InstrumentName, ProcessQuoteThread);
                    else
                        ProcessLastQuoteThreads[instr.InstrumentName] = ProcessQuoteThread;

                }
            }

        }

        protected void ProcessOrderBookDepth(IWebSocketConnection socket, Subscribe subscrMsg)
        {

            try
            {
                ClientInstrument instr = GetInstrumentByServiceKey(subscrMsg.ServiceKey);
                List<DepthOfBook> depthOfBooks = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName).ToList();
                if (depthOfBooks != null && depthOfBooks.Count>0)
                {
                    depthOfBooks.ForEach(x => TranslateAndSendOldDepthOfBook(socket, x, instr, subscrMsg.UUID));
                    Thread.Sleep(1000);
                }

                if(SubscribedLQ)
                    UpdateQuotes(socket, instr, subscrMsg.UUID);
                ProcessSubscriptionResponse(socket, "LD", subscrMsg.ServiceKey, subscrMsg.UUID, msg: "success");
            }
            catch (Exception ex)
            {

                DoLog(string.Format(ex.Message), MessageType.Error);
                ProcessSubscriptionResponse(socket, "LD", subscrMsg.ServiceKey, subscrMsg.UUID, false, ex.Message);
            }
        }

        protected void ProcessCreditRecordUpdates(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate creditRecordUpdate = CreditRecordUpdates.Where(x => x.FirmId == subscrMsg.ServiceKey).FirstOrDefault();
            DGTLBackendMock.Common.DTO.Account.AccountRecord defaultAccount = AccountRecords.Where(x => x.EPFirmId == subscrMsg.ServiceKey).FirstOrDefault();

            if (creditRecordUpdate != null)
            {
                TranslateAndSendOldCreditRecordUpdate(socket, creditRecordUpdate, defaultAccount, subscrMsg.UUID);
                ProcessSubscriptionResponse(socket, "T", subscrMsg.ServiceKey, subscrMsg.UUID);
            }
            else
            {
                ProcessSubscriptionResponse(socket, "T", subscrMsg.ServiceKey, subscrMsg.UUID, success: false, msg: string.Format("Unknown Credit Record Update for FirmId {0}", subscrMsg.ServiceKey));
            }
        }

        protected void ProcessMyOrders(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            string instrumentId = "";
            string[] fields = subscrMsg.ServiceKey.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length >= 2)
                instrumentId = fields[1];
            else
                throw new Exception(string.Format("Invalid format from ServiceKey. Valid format:UserID@Symbol@[OrderID,*]. Received: {1}", subscrMsg.ServiceKey));


            
            List<LegacyOrderRecord> orders = null;
            if (instrumentId != "*")
            {
                ClientInstrument instr = GetInstrumentByServiceKey(instrumentId);
                orders = Orders.Where(x => x.InstrumentId == instr.InstrumentName).ToList();
            }
            else
                orders = Orders.ToList();

            DoLog(string.Format("Sending all orders for {0} subscription. Count={1}", subscrMsg.ServiceKey, orders.Count), MessageType.Information);

            orders.ForEach(x => TranslateAndSendOldLegacyOrderRecord(socket, subscrMsg.UUID, x));// Translate and send
            
            //Now we have to launch something to create deltas (insert, change, remove)
            //RefreshOpenOrders(socket, subscrMsg.ServiceKey, subscrMsg.UserId);
            ProcessSubscriptionResponse(socket, "Oy", subscrMsg.ServiceKey, subscrMsg.UUID);
        }

        protected void ProcessMyTrades(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            List<LegacyTradeHistory> trades = null;

            if (subscrMsg.ServiceKey != "*")
            {
                ClientInstrument instr = GetInstrumentByServiceKey(subscrMsg.ServiceKey);
                trades = Trades.Where(x => x.Symbol == instr.InstrumentName).ToList();
            }
            else
                trades = Trades.ToList();

            trades.ForEach(x => TranslateAndSendOldLegacyTradeHistory(socket, subscrMsg.UUID, x));
            //Now we have to launch something to create deltas (insert, change, remove)
            ProcessSubscriptionResponse(socket, "LT", subscrMsg.ServiceKey, subscrMsg.UUID);
        }

        protected void ProcessClientLogoutV2(IWebSocketConnection socket, string m)
        {
            ClientLogoutRequest wsLogout = JsonConvert.DeserializeObject<ClientLogoutRequest>(m);

            ClientLogoutResponse logout = new ClientLogoutResponse()
            {
                Msg = "ClientLogoutResponse",
                JsonWebToken = wsLogout.JsonWebToken,
                UUID = wsLogout.UUID,
                Time = wsLogout.Time,
                cSuccess = ClientLogoutResponse._STATUS_OK,
                Message = "Successfully logged out"

            };


            DoSend<ClientLogoutResponse>(socket, logout);
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

        protected void ProcessSubscriptions(IWebSocketConnection socket, string m)
        {
            Subscribe subscrMsg = JsonConvert.DeserializeObject<Subscribe>(m);

            DoLog(string.Format("Incoming subscription for service {0} - ServiceKey:{1}", subscrMsg.Service, subscrMsg.ServiceKey), MessageType.Information);

            if (subscrMsg.Action == Subscribe._ACTION_SUBSCRIBE)
            {
              
                if (subscrMsg.Service == "LS")
                {
                    if (subscrMsg.ServiceKey != null)
                        ProcessLastSale(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LQ")
                {
                    if (subscrMsg.ServiceKey != null)
                        ProcessQuote(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LD")
                {
                    ProcessOrderBookDepth(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "T")
                {
                    ProcessCreditRecordUpdates(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "Oy")
                {
                    ProcessMyOrders(socket, subscrMsg);
                }
                else if (subscrMsg.Service == "LT")
                {
                    ProcessMyTrades(socket, subscrMsg);
                }
                //else if (subscrMsg.Service == "FP")
                //{
                //    if (subscrMsg.ServiceKey != null)
                //        ProcessOficialFixingPrice(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "Ot")
                //{
                //    ProcessOpenOrderCount(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "TN")
                //{
                //    ProcessNotifications(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "FD")
                //{
                //    if (subscrMsg.ServiceKey != null)
                //        ProcessDailySettlement(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "Cm")
                //{
                //    ProcessCreditLimitUpdates(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "FO")
                //{
                //    ProcessFillOffers(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "PS")
                //{
                //    ProcessPlatformStatus(socket, subscrMsg);
                //}
             
                //else if (subscrMsg.Service == "rt")
                //{
                //    ProcessBlotterTrades(socket, subscrMsg);
                //}
            }
            else if (subscrMsg.Action == Subscribe._ACTION_UNSUBSCRIBE)
            {
                ProcessUnsubscriptions(subscrMsg);
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
                else if (wsResp.Msg == "ClientLogoutRequest")
                {
                    ProcessClientLogoutV2(socket, m);
                }
                else if (wsResp.Msg == "TokenRequest")
                {
                    ProcessTokenResponse(socket, m);
                }
                else if (wsResp.Msg == "ClientHeartbeat")
                {
                    //We do nothing//

                }
                else if (wsResp.Msg == "ClientOrderReq")
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
