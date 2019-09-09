using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.Auth.V2;
using DGTLBackendMock.Common.DTO.MarketData;
using DGTLBackendMock.Common.DTO.MarketData.V2;
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
        
        }

        #endregion

        #region Protected Attributes

        protected ClientInstrumentBatch InstrBatch { get; set; }

        protected string LastTokenGenerated { get; set; }

        protected Thread HeartbeatThread { get; set; }

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
                //We reject the messageas a convention, we cannot send messages lower than 6000 USD
                ClientOrderRej reject = new ClientOrderRej()
                {
                    Msg = "ClientOrderRej",
                    cRejectCode='0',
                    ExchangeId=0,
                    UUID=clientOrderReq.UUID,
                    TransactionTimes=Convert.ToInt64(elapsed.TotalMilliseconds)

                };

                DoSend<ClientOrderRej>(socket, reject);

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
                        //We send the mock ack
                        ClientOrderAck clientOrdAck = new ClientOrderAck()
                        {
                            Msg = "ClientOrderAck",
                            ClientOrderId = clientOrderReq.ClientOrderId,
                            OrderId = Guid.NewGuid().ToString(),
                            TransactionTime = Convert.ToInt64(elapsed.TotalMilliseconds),
                            UUID = clientOrderReq.UUID,
                            UserId = clientOrderReq.UserId
                        };

                        DoLog(string.Format("Sending ClientOrderAck ..."), MessageType.Information);
                        DoSend<ClientOrderAck>(socket, clientOrdAck);


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
                            UUID = subscrMsg.UUID,
                            Volume = legacyLastSale.Volume

                        };

                        //EmulatePriceChanges(i, lastSale, ref initialPrice);
                        DoSend<ClientLastSale>(socket, lastSale);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.UUID);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        string errMsg = string.Format("LastSale not found for InstrumentId {0} - Symbol {1} ...", subscrMsg.ServiceKey, instr.InstrumentName);
                        ProcessSubscriptionResponse(socket, "LS", subscrMsg.ServiceKey, subscrMsg.UUID, success: false, msg: errMsg);
                        DoLog(errMsg, MessageType.Information);
                        break;
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing last sales message: {0}...", ex.Message), MessageType.Error);
            }
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
                            MidPoint = legacyLastQuote.MidPoint,
                            UUID = subscrMsg.UUID,
                        };

                        DoSend<Quote>(socket, legacyLastQuote);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        string errMsg = string.Format("Quotes not found for InstrumentId {0} - Symbol {1} ...", subscrMsg.ServiceKey, instr.InstrumentName);
                        ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID, success: false, msg: errMsg);
                        DoLog(errMsg, MessageType.Information);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing quote message: {0}...", ex.Message), MessageType.Error);
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
              
                //else if (subscrMsg.Service == "CU")
                //{
                //    ProcessCreditRecordUpdates(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "Cm")
                //{
                //    ProcessCreditLimitUpdates(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "LD")
                //{
                //    ProcessOrderBookDepth(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "Oy")
                //{
                //    ProcessMyOrders(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "FO")
                //{
                //    ProcessFillOffers(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "PS")
                //{
                //    ProcessPlatformStatus(socket, subscrMsg);
                //}
                //else if (subscrMsg.Service == "LT")
                //{
                //    ProcessMyTrades(socket, subscrMsg);
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
