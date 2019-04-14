using DGTLBackendMock.BusinessEntities;
using DGTLBackendMock.Common.DTO;
using DGTLBackendMock.Common.DTO.Account;
using DGTLBackendMock.Common.DTO.Auth;
using DGTLBackendMock.Common.DTO.MarketData;
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

namespace DGTLBackendMock.DataAccessLayer
{
    
    public class DGTLWebSocketServer : DGTLWebSocketBaseServer
    {
        #region Protected Attributes

        protected SecurityMasterRecord[] SecurityMasterRecords { get; set; }

        protected LastSale[] LastSales { get; set; }

        protected Quote[] Quotes { get; set; }

        protected DailySettlementPrice[] DailySettlementPrices { get; set; }

        protected OfficialFixingPrice[] OfficialFixingPrices { get; set; }

        protected AccountRecord[] AccountRecords { get; set; }

        protected DepthOfBook[] DepthOfBooks { get; set; }

        protected DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate[] CreditRecordUpdates { get; set; }

        #endregion

        #region Constructors

        public DGTLWebSocketServer(string pURL)
        {
            URL = pURL;

            HeartbeatSeqNum = 1;

            UserLogged = false;

            Logger = new PerDayFileLogSource(Directory.GetCurrentDirectory() + "\\Log", Directory.GetCurrentDirectory() + "\\Log\\Backup")
            {
                FilePattern = "Log.{0:yyyy-MM-dd}.log",
                DeleteDays = 20
            };

            DoLog("Initializing Mock Server...", MessageType.Information);

            try
            {
                DoLog("Initializing all collections...", MessageType.Information);

                LoadUserRecords();

                LoadAccountRecords();

                LoadSecurityMasterRecords();

                LoadLastSales();

                LoadQuotes();

                LoadDailySettlementPrices();

                LoadOfficialFixingPrices();

                LoadCreditRecordUpdates();

                LoadDepthOfBooks();

                DoLog("Collections Initialized...", MessageType.Information);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error initializing collections: {0}...",ex.Message), MessageType.Error);
            }

        }

        #endregion

        #region Private Methods

        private void LoadAccountRecords()
        {
            string strAccountRecords = File.ReadAllText(@".\input\AccountRecord.json");

            //Aca le metemos que serialize el contenido
            AccountRecords = JsonConvert.DeserializeObject<AccountRecord[]>(strAccountRecords);
        }

        private void LoadCreditRecordUpdates()
        {
            string strCreditRecordUpdate = File.ReadAllText(@".\input\CreditRecordUpdate.json");

            //Aca le metemos que serialize el contenido
            CreditRecordUpdates = JsonConvert.DeserializeObject<DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate[]>(strCreditRecordUpdate);
        }

        private void LoadSecurityMasterRecords()
        {
            string strSecurityMasterRecords = File.ReadAllText(@".\input\SecurityMasterRecord.json");

            //Aca le metemos que serialize el contenido
            SecurityMasterRecords = JsonConvert.DeserializeObject<SecurityMasterRecord[]>(strSecurityMasterRecords);
        }

        private void LoadLastSales()
        {
            string strLastSales = File.ReadAllText(@".\input\LastSales.json");

            //Aca le metemos que serialize el contenido
            LastSales = JsonConvert.DeserializeObject<LastSale[]>(strLastSales);
        }

        private void LoadQuotes()
        {
            string strQuotes = File.ReadAllText(@".\input\Quotes.json");

            //Aca le metemos que serialize el contenido
            Quotes = JsonConvert.DeserializeObject<Quote[]>(strQuotes);
        }

        private void LoadDepthOfBooks()
        {
            string strDepthOfBooks = File.ReadAllText(@".\input\DepthOfBook.json");

            //Aca le metemos que serialize el contenido
            DepthOfBooks = JsonConvert.DeserializeObject<DepthOfBook[]>(strDepthOfBooks);
        }

        private void LoadDailySettlementPrices()
        {
            string strDaylySettlementPrices = File.ReadAllText(@".\input\DailySettlementPrice.json");

            //Aca le metemos que serialize el contenido
            DailySettlementPrices = JsonConvert.DeserializeObject<DailySettlementPrice[]>(strDaylySettlementPrices);
        }

        private void LoadOfficialFixingPrices()
        {
            string strOfficialFixingPrices = File.ReadAllText(@".\input\OfficialFixingPrice.json");

            //Aca le metemos que serialize el contenido
            OfficialFixingPrices = JsonConvert.DeserializeObject<OfficialFixingPrice[]>(strOfficialFixingPrices);
        }

        private void LastSaleThread(object param) 
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            string symbol = (string)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    LastSale lastSale = LastSales.Where(x => x.Symbol == symbol).FirstOrDefault();
                    if (lastSale != null)
                    {
                        string strLastSale = JsonConvert.SerializeObject(lastSale, Newtonsoft.Json.Formatting.None,
                                new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                });

                        socket.Send(strLastSale);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "LS", symbol);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Last Sales not found for symbol {0}...", symbol), MessageType.Information);
                        break;
                    }
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
            string symbol = (string)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    Quote quote = Quotes.Where(x => x.Symbol == symbol).FirstOrDefault();
                    if (quote != null)
                    {
                        string strQuote = JsonConvert.SerializeObject(quote, Newtonsoft.Json.Formatting.None,
                                new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                });

                        socket.Send(strQuote);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "LQ", symbol);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Quotes not found for symbol {0}...", symbol), MessageType.Information);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing quote message: {0}...", ex.Message), MessageType.Error);
            }
        }

        private void DailySettlementThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            string symbol = (string)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    DailySettlementPrice dailySettl= DailySettlementPrices.Where(x => x.Symbol == symbol).FirstOrDefault();
                    if (dailySettl != null)
                    {
                        string strDailySettl = JsonConvert.SerializeObject(dailySettl, Newtonsoft.Json.Formatting.None,
                                new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                });

                        socket.Send(strDailySettl);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "FP", symbol);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Daily Settlement Price not found for symbol {0}...", symbol), MessageType.Information);
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

        private void DailyOfficialFixingPriceThread(object param)
        {
            object[] paramArray = (object[])param;
            IWebSocketConnection socket = (IWebSocketConnection)paramArray[0];
            string symbol = (string)paramArray[1];
            bool subscResp = false;

            try
            {
                while (true)
                {
                    OfficialFixingPrice officialFixingPrice = OfficialFixingPrices.Where(x => x.Symbol == symbol).FirstOrDefault();
                    if (officialFixingPrice != null)
                    {
                        string strOfficialFixingPrice  = JsonConvert.SerializeObject(officialFixingPrice, Newtonsoft.Json.Formatting.None,
                                new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                });

                        socket.Send(strOfficialFixingPrice);
                        Thread.Sleep(3000);//3 seconds
                        if (!subscResp)
                        {
                            ProcessSubscriptionResponse(socket, "FD", symbol);
                            Thread.Sleep(2000);
                            subscResp = true;
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Official Fixing Price not found for symbol {0}...", symbol), MessageType.Information);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing daily settlement price message: {0}...", ex.Message), MessageType.Error);

            }
        }

        private void ProcessLastSale(IWebSocketConnection socket,string symbol)
        {
            Thread ProcessLastSaleThread = new Thread(LastSaleThread);
            ProcessLastSaleThread.Start(new object[] { socket, symbol });
        
        }

        private void ProcessQuote(IWebSocketConnection socket, string symbol)
        {
            Thread ProcessQuoteThread = new Thread(QuoteThread);
            ProcessQuoteThread.Start(new object[] { socket, symbol });

        }

        private void ProcessDailySettlement(IWebSocketConnection socket, string symbol)
        {
            EvalDailySettlementPriceWarnings(symbol);
            Thread ProcessDailySettlementThread = new Thread(DailySettlementThread);
            ProcessDailySettlementThread.Start(new object[] { socket, symbol });

        }

        

        private void ProcessAccountRecord(IWebSocketConnection socket, string key)
        {
            if (key != "*")
            {
                if (key.EndsWith("@*"))
                    key = key.Replace("@*", "");

                List<AccountRecord> accountRecords = AccountRecords.Where(x => x.AccountKey == key).ToList();

                accountRecords.ForEach(x => DoSend<AccountRecord>(socket,x));
            }
            else
                AccountRecords.ToList().ForEach(x => DoSend<AccountRecord>(socket, x));

            ProcessSubscriptionResponse(socket, "TD", key);
        }

        private void ProcessOrderBookDepth(IWebSocketConnection socket, string key)
        {

            List<DepthOfBook> depthOfBooks = DepthOfBooks.Where(x => x.Symbol == key).ToList();

            depthOfBooks.ForEach(x => DoSend<DepthOfBook>(socket, x));

            //Now we have to launch something to create deltas (insert, change, remove)

            ProcessSubscriptionResponse(socket, "LD", key);
        }


        private void ProcessCreditRecordUpdates(IWebSocketConnection socket, string key)
        {
            if (key != "*")
            {
                if (key.EndsWith("@*"))
                    key = key.Replace("@*", "");

                DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate creditRecordUpdate = CreditRecordUpdates.Where(x => x.FirmId == key).FirstOrDefault();

                if (creditRecordUpdate != null)
                    DoSend<DGTLBackendMock.Common.DTO.Account.CreditRecordUpdate>(socket, creditRecordUpdate);
            }
            

            ProcessSubscriptionResponse(socket, "CU", key);
        }

        private void ProcessOficialFixingPrice(IWebSocketConnection socket, string symbol)
        {
            EvalDailyOfficialFixingPriceWarnings(symbol);
            Thread ProcessDailyOfficialFixingPriceThread = new Thread(DailyOfficialFixingPriceThread);
            ProcessDailyOfficialFixingPriceThread.Start(new object[] { socket, symbol });
        }

        private void ProcessSecurityMasterRecord(IWebSocketConnection socket)
        {
            foreach (SecurityMasterRecord sec in SecurityMasterRecords)
            {
                string secMasterRecord = JsonConvert.SerializeObject(sec, Newtonsoft.Json.Formatting.None,
                          new JsonSerializerSettings
                          {
                              NullValueHandling = NullValueHandling.Ignore
                          });


                socket.Send(secMasterRecord);
            }
            Thread.Sleep(2000);
            ProcessSubscriptionResponse(socket, "SubscriptionResponse", "*");
        }

        private void ProcessSubscriptions(IWebSocketConnection socket,string m)
        {
            SubscriptionMsg subscrMsg = JsonConvert.DeserializeObject<SubscriptionMsg>(m);

            DoLog(string.Format("Incoming subscription for service {0}", subscrMsg.Service), MessageType.Information);


            if (subscrMsg.Service == "TA")
            {
                ProcessSecurityMasterRecord(socket);
                
            }
            else if (subscrMsg.Service == "LS")
            {
                if(subscrMsg.ServiceKey!=null)
                    ProcessLastSale(socket,subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "LQ")
            {
                if (subscrMsg.ServiceKey != null)
                    ProcessQuote(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "FP")
            {
                if (subscrMsg.ServiceKey != null)
                    ProcessOficialFixingPrice(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "FD")
            {
                if (subscrMsg.ServiceKey != null)
                    ProcessDailySettlement(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "TB")
            {
                ProcessUserRecord(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "TD")
            {
                ProcessAccountRecord(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "CU")
            {
                ProcessCreditRecordUpdates(socket, subscrMsg.ServiceKey);
            }
            else if (subscrMsg.Service == "LD")
            {
                ProcessOrderBookDepth(socket, subscrMsg.ServiceKey);
            }

        }

        #endregion

        #region Protected Methods

        protected override  void OnOpen(IWebSocketConnection socket)
        {
            //socket.Send("Connection Opened");
            Thread heartbeatThread = new Thread(ClientHeartbeatThread);

            heartbeatThread.Start(socket);
        }

        protected override void OnClose(IWebSocketConnection socket)
        {


        }

        protected override void OnMessage(IWebSocketConnection socket, string m)
        {
            try
            {
                WebSocketMessage wsResp = JsonConvert.DeserializeObject<WebSocketMessage>(m);

                if (wsResp.Msg == "ClientLogin")
                {
                    ProcessClientLoginMock(socket, m);
                }
                else if (wsResp.Msg == "ClientHeartbeatResponse")
                { 
                    //We do nothing as the DGTL server does
                
                }
                else if (wsResp.Msg == "ClientLogout")
                {

                    ProcessClientLogoutMock(socket);
                   
                }
                else if (wsResp.Msg == "Subscribe")
                {

                    ProcessSubscriptions(socket, m);

                }
                else
                {
                    UnknownMessage unknownMsg = new UnknownMessage()
                    {
                        Msg = "MessageReject",
                        Reason = string.Format("Unknown message type {0}", wsResp.Msg)

                    };

                    string strUnknownMsg = JsonConvert.SerializeObject(unknownMsg, Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                    socket.Send(strUnknownMsg);
                }

            }
            catch (Exception ex)
            {
                UnknownMessage errorMsg = new UnknownMessage()
                {
                    Msg = "MessageReject",
                    Reason = string.Format("Error processing message: {0}", ex.Message)

                };

                string strErrorMsg = JsonConvert.SerializeObject(errorMsg, Newtonsoft.Json.Formatting.None,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                socket.Send(strErrorMsg);
            }

        }

        #endregion

    }
}
