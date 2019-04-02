using DGTLBackendMock.Common.DTO;
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

namespace DGTLBakcendMock.DataAccessLayer
{
    public class DGTLWebSocketServer
    {
        #region Protected Attributes

        protected string URL { get; set; }

        protected Fleck.WebSocketServer WebSocketServer { get; set; }

        protected SecurityMasterRecord[] SecurityMasterRecords { get; set; }

        protected LastSale[] LastSales { get; set; }

        protected Quote[] Quotes { get; set; }

        protected DailySettlementPrice[] DailySettlementPrices { get; set; }

        protected OfficialFixingPrice[] OfficialFixingPrices { get; set; }

        public int HeartbeatSeqNum { get; set; }

        public bool UserLogged { get; set; }

        #endregion


        #region Constructors

        public DGTLWebSocketServer(string pURL)
        {
            URL = pURL;

            HeartbeatSeqNum = 1;

            UserLogged = false;

            LoadSecurityMasterRecords();

            LoadLastSales();

            LoadQuotes();

            LoadDailySettlementPrices();

            LoadOfficialFixingPrices();

        }


        #endregion

        #region Private Methods

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

        private void ProcessClientLoginMock(IWebSocketConnection socket, string m)
        {
            WebSocketLoginMessage wsLogin = JsonConvert.DeserializeObject<WebSocketLoginMessage>(m);

            if (wsLogin.UserId == "user1" && wsLogin.Password == "test123")
            {
                ClientLoginResponse resp = new ClientLoginResponse()
                {
                    Msg = "ClientLoginResponse",
                    Sender = wsLogin.Sender,
                    UUID = wsLogin.UUID,
                    UserId = wsLogin.UserId,
                    JsonWebToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE1NTEzODY5NjksImV4cCI"


                };

                string respMsg = JsonConvert.SerializeObject(resp, Newtonsoft.Json.Formatting.None,
                                               new JsonSerializerSettings
                                               {
                                                   NullValueHandling = NullValueHandling.Ignore
                                               });

                UserLogged = true;
                socket.Send(respMsg);
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

                string rejMsg = JsonConvert.SerializeObject(reject, Newtonsoft.Json.Formatting.None,
                       new JsonSerializerSettings
                       {
                           NullValueHandling = NullValueHandling.Ignore
                       });
                socket.Send(rejMsg);
                socket.Close();
            }
        }

        private void ProcessClientLogoutMock(IWebSocketConnection socket)
        {

            ClientLogoutResponse logout = new ClientLogoutResponse()
            {
                Msg = "ClientLogoutResponse",
                UserId = "0",
                Sender = 1,
                Time = 0
            };

            string logoutRespMsg = JsonConvert.SerializeObject(logout, Newtonsoft.Json.Formatting.None,
                       new JsonSerializerSettings
                       {
                           NullValueHandling = NullValueHandling.Ignore
                       });
            socket.Send(logoutRespMsg);
            socket.Close();
        
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
                        //TODO: Log no tenemos MD para este symbol
                        break;
                    }
                }
            }
            catch (Exception ex)
            { 
                //TODO: Log: Hubo algún problema procesando los LastSales--> Desconectar y todo
            
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
                        //TODO: Log no tenemos MD para este symbol
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO: Log: Hubo algún problema procesando los LastSales--> Desconectar y todo

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
                        //TODO: Log no tenemos MD para este symbol
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO: Log: Hubo algún problema procesando los LastSales--> Desconectar y todo

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
                        //TODO: Log no tenemos MD para este symbol
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO: Log: Hubo algún problema procesando los LastSales--> Desconectar y todo

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
            Thread ProcessDailySettlementThread = new Thread(DailySettlementThread);
            ProcessDailySettlementThread.Start(new object[] { socket, symbol });

        }

        private void ProcessOficialFixingPrice(IWebSocketConnection socket, string symbol)
        {
            Thread ProcessDailyOfficialFixingPriceThread = new Thread(DailyOfficialFixingPriceThread);
            ProcessDailyOfficialFixingPriceThread.Start(new object[] { socket, symbol });
        }

        private void ProcessSubscriptionResponse(IWebSocketConnection socket, string service,string serviceKey)
        {
            SubscriptionResponse resp = new SubscriptionResponse()
            {
                Message = "",
                Success = true,
                Service = service,
                ServiceKey = serviceKey,
                Msg = "SubscriptionResponse"

            };

            string strSubscResp = JsonConvert.SerializeObject(resp, Newtonsoft.Json.Formatting.None,
                         new JsonSerializerSettings
                         {
                             NullValueHandling = NullValueHandling.Ignore
                         });

            socket.Send(strSubscResp);
        
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

        }

        #endregion

        #region Thread Methods

        private void ClientHeartbeatThread(object param)
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


                        string strHeartbeatReq = JsonConvert.SerializeObject(heartbeatReq, Newtonsoft.Json.Formatting.None,
                              new JsonSerializerSettings
                              {
                                  NullValueHandling = NullValueHandling.Ignore
                              });
                        socket.Send(strHeartbeatReq);
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

        #region Protected Methods

        protected  void OnOpen(IWebSocketConnection socket)
        {
            //socket.Send("Connection Opened");
            Thread heartbeatThread = new Thread(ClientHeartbeatThread);

            heartbeatThread.Start(socket);
        }

        protected void OnClose(IWebSocketConnection socket)
        {


        }

        protected void OnMessage(IWebSocketConnection socket, string m)
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
