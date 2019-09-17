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
            
        }

        #endregion

        #region Protected Attributes

        protected ClientInstrumentBatch InstrBatch { get; set; }

        protected string LastTokenGenerated { get; set; }

        protected Thread HeartbeatThread { get; set; }

        protected bool SubscribedLQ { get; set; }

        public string LoggedFirmId { get; set; }

        public string LoggedUserId { get; set; }

        protected FirmsListResponse FirmListResp { get; set; }

        #endregion

        #region Private Methods

        private void EvalPriceLevels(IWebSocketConnection socket, ClientOrderRecord order,string UUID)
        {

            ClientInstrument instr = GetInstrumentByIntInstrumentId(order.InstrumentId);
            if (!order.Price.HasValue)
                return;

            TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

            if (order.cSide == LegacyOrderReq._SIDE_BUY)
            {
                DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._BID_ENTRY
                                                                       && x.Symbol == instr.InstrumentName
                                                                       && x.Price == Convert.ToDecimal(order.Price.Value)).FirstOrDefault();

                if (existingPriceLevel != null)
                {
                    decimal newSize = existingPriceLevel.Size - Convert.ToDecimal(order.LeavesQty);

                    DepthOfBook updPriceLevel = new DepthOfBook();
                    updPriceLevel.cAction = newSize > 0 ? DepthOfBook._ACTION_CHANGE : DepthOfBook._ACTION_REMOVE;
                    updPriceLevel.cBidOrAsk = DepthOfBook._BID_ENTRY;
                    updPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                    updPriceLevel.Index = existingPriceLevel.Index;
                    updPriceLevel.Msg = existingPriceLevel.Msg;
                    updPriceLevel.Price = existingPriceLevel.Price;
                    updPriceLevel.Sender = existingPriceLevel.Sender;
                    updPriceLevel.Size = newSize;
                    updPriceLevel.Symbol = existingPriceLevel.Symbol;

                    existingPriceLevel.Size = updPriceLevel.Size;

                    if (updPriceLevel.cAction == DepthOfBook._ACTION_REMOVE)
                        DepthOfBooks.Remove(existingPriceLevel);

                    DoLog(string.Format("Sending upd bid entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
                    TranslateAndSendOldDepthOfBook(socket, updPriceLevel, instr, UUID);
                    //DoSend<DepthOfBook>(socket, updPriceLevel);
                }
                else
                {

                    throw new Exception(string.Format("Critical Error: Cancelling an order for an unexisting bid price level: {0}", order.Price.Value));
                }

            }
            else if (order.cSide == LegacyOrderReq._SIDE_SELL)
            {
                DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._ASK_ENTRY
                                                                       && x.Symbol == instr.InstrumentName
                                                                       && x.Price == Convert.ToDecimal(order.Price.Value)).FirstOrDefault();

                if (existingPriceLevel != null)
                {
                    decimal newSize = existingPriceLevel.Size - Convert.ToDecimal(order.LeavesQty);

                    DepthOfBook updPriceLevel = new DepthOfBook();
                    updPriceLevel.cAction = newSize > 0 ? DepthOfBook._ACTION_CHANGE : DepthOfBook._ACTION_REMOVE;
                    updPriceLevel.cBidOrAsk = DepthOfBook._ASK_ENTRY;
                    updPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                    updPriceLevel.Index = existingPriceLevel.Index;
                    updPriceLevel.Msg = existingPriceLevel.Msg;
                    updPriceLevel.Price = existingPriceLevel.Price;
                    updPriceLevel.Sender = existingPriceLevel.Sender;
                    updPriceLevel.Size = existingPriceLevel.Size - Convert.ToDecimal(order.LeavesQty);
                    updPriceLevel.Symbol = existingPriceLevel.Symbol;

                    existingPriceLevel.Size = updPriceLevel.Size;

                    if (updPriceLevel.cAction == DepthOfBook._ACTION_REMOVE)
                        DepthOfBooks.Remove(existingPriceLevel);

                    DoLog(string.Format("Sending upd ask entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
                    TranslateAndSendOldDepthOfBook(socket, updPriceLevel, instr, UUID);
                    //DoSend<DepthOfBook>(socket, updPriceLevel);
                }
                else
                {
                    throw new Exception(string.Format("Critical Error: Cancelling an order for an unexisting ask price level: {0}", order.Price.Value));
                }
            }
        }

        //1.2.1-We update market data for a new trade
        private void EvalMarketData(IWebSocketConnection socket, LegacyTradeHistory newTrade,ClientInstrument instr,string UUID)
        {

            DoLog(string.Format("We update LastSale MD for Size={0} and Price={1} for symbol {2}", newTrade.TradeQuantity, newTrade.TradePrice, newTrade.Symbol), MessageType.Information);

            LastSale ls = LastSales.Where(x => x.Symbol == newTrade.Symbol).FirstOrDefault();

            if (ls == null)
            {
                ls = new LastSale() { Change = 0, DiffPrevDay = 0, Msg = "LastSale", Symbol = newTrade.Symbol, Volume = 0 };
                LastSales.Add(ls);

            }

            if (!ls.FirstPrice.HasValue)
                ls.FirstPrice = ls.LastPrice;

            ls.LastPrice = Convert.ToDecimal(newTrade.TradePrice);
            ls.LastShares = Convert.ToDecimal(newTrade.TradeQuantity);
            ls.LastTime = newTrade.TradeTimeStamp;
            ls.Volume += Convert.ToDecimal(newTrade.TradeQuantity);
            ls.Change = ls.LastPrice - ls.FirstPrice;
            ls.DiffPrevDay = ((ls.LastPrice / ls.FirstPrice) - 1) * 100;

            if (!ls.High.HasValue || Convert.ToDecimal(newTrade.TradePrice) > ls.High)
                ls.High = Convert.ToDecimal(newTrade.TradePrice);

            if (!ls.Low.HasValue || Convert.ToDecimal(newTrade.TradePrice) < ls.Low)
                ls.Low = Convert.ToDecimal(newTrade.TradePrice);

            DoLog(string.Format("LastSale updated..."), MessageType.Information);


            DoLog(string.Format("We udate Quotes MD for Size={0} and Price={1} for symbol {2}", newTrade.TradeQuantity, newTrade.TradePrice, newTrade.Symbol), MessageType.Information);
            UpdateQuotes(socket, instr, UUID);
            DoLog(string.Format("Quotes updated..."), MessageType.Information);
        }


        //1.1-We eliminate the price level (@DepthOfBook and @Orders and we send the message)
        private void RemovePriceLevel(DepthOfBook bestOffer, char bidOrAsk, IWebSocketConnection socket,ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            DoLog(string.Format("We eliminate the {2} price level {0} for symbol{1} (@DepthOfBook and @Orders and we send the message)",
                bestOffer.Price, bestOffer.Symbol, bidOrAsk == DepthOfBook._BID_ENTRY ? "bid" : "ask"), MessageType.Information);


            List<LegacyOrderRecord> updOrders = Orders.Where(x => x.InstrumentId == bestOffer.Symbol
                                                                  && x.Price.Value == Convert.ToDouble(bestOffer.Price)
                                                                  && x.cSide == (bidOrAsk == DepthOfBook._BID_ENTRY ? LegacyOrderRecord._SIDE_BUY : LegacyOrderRecord._SIDE_SELL)).ToList();

            foreach (LegacyOrderRecord order in updOrders)
            {
                order.SetFilledStatus();
                TranslateAndSendOldLegacyOrderRecord(socket, UUID, order);

                if (order.Price.HasValue)
                {
                    DoLog(string.Format("We send the Trade for my other affected filled order for symbol {0} Side={1} Price={2} ", order.InstrumentId, order.cSide == LegacyOrderRecord._SIDE_BUY ? "buy" : "sell", order.Price.Value), MessageType.Information);
                    SendNewTrade(order.cSide, Convert.ToDecimal(order.Price.Value), Convert.ToDecimal(order.OrdQty), socket, instr, UUID);
                }
            }


            DepthOfBooks.Remove(bestOffer);

            DoLog(string.Format("We send the DepthOfBookMessage removing the {2} price level {0} for symbol{1} (@DepthOfBook and @Orders and we send the message)",
                  bestOffer.Price, bestOffer.Symbol, bidOrAsk == DepthOfBook._BID_ENTRY ? "bid" : "ask"), MessageType.Information);
            DepthOfBook remPriceLevel = new DepthOfBook();
            remPriceLevel.cAction = DepthOfBook._ACTION_REMOVE;
            remPriceLevel.cBidOrAsk = bidOrAsk;
            remPriceLevel.DepthTime = Convert.ToInt64(elapsed.TotalMilliseconds);
            remPriceLevel.Index = 0;
            remPriceLevel.Msg = "DepthOfBook";
            remPriceLevel.Price = bestOffer.Price;
            remPriceLevel.Sender = 0;
            remPriceLevel.Size = 0;
            remPriceLevel.Symbol = bestOffer.Symbol;

            TranslateAndSendOldDepthOfBook(socket, remPriceLevel, instr, UUID);
            //DoSend<DepthOfBook>(socket, remPriceLevel);
        }

        //1.2- We send a Trade by <size>
        private void SendNewTrade(char side, decimal price, decimal size, IWebSocketConnection socket, ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            DoLog(string.Format("We send a Trade for Size={0} and Price={1} for symbol {2}", size, price, instr.InstrumentName), MessageType.Information);

            LegacyTradeHistory newTrade = new LegacyTradeHistory()
            {
                cMySide = side,
                Msg = "LegacyTradeHistory",
                MyTrade = true,
                Sender = 0,
                Symbol = instr.InstrumentName,
                TradeId = Guid.NewGuid().ToString(),
                TradePrice = Convert.ToDouble(price),
                TradeQuantity = Convert.ToDouble(size),
                TradeTimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds)

            };
            TranslateAndSendOldLegacyTradeHistory(socket, UUID, newTrade);
            //DoSend<LegacyTradeHistory>(socket, newTrade);

            //1.2.1-We update market data for a new trade
            EvalMarketData(socket, newTrade, instr, UUID);

            //1.2.2-We send a trade notification for the new trade
            //SendTradeNotification(newTrade, legOrdReq.UserId, socket);

        }

        //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
        private void UpdatePriceLevel(ref DepthOfBook bestOffer, char bidOrAsk, double tradeSize, IWebSocketConnection socket, ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            string symbol = bestOffer.Symbol;
            double bestOfferPrice = Convert.ToDouble(bestOffer.Price);

            DoLog(string.Format("We update the {2} price level {0} for symbol {1} (@DepthOfBook and @Orders and we send the message)",
                                bestOffer.Price, bestOffer.Symbol, bidOrAsk == DepthOfBook._BID_ENTRY ? "bid" : "ask"), MessageType.Information);

            bestOffer.Size -= Convert.ToDecimal(tradeSize);
            bestOffer.cAction = DepthOfBook._ACTION_CHANGE;

            DoLog(string.Format("Searching for affecter orders for symbol {0}", bestOffer.Symbol), MessageType.Information);

            List<LegacyOrderRecord> updOrders = Orders.Where(x => x.InstrumentId == symbol
                                                                 && x.Price.Value == bestOfferPrice
                                                                 && x.cSide == (bidOrAsk == DepthOfBook._BID_ENTRY ? LegacyOrderRecord._SIDE_BUY : LegacyOrderRecord._SIDE_SELL)).ToList();

            foreach (LegacyOrderRecord order in updOrders)
            {
                DoLog(string.Format("Updating order status for price level {0}", order.Price.Value), MessageType.Information);

                double prevFillQty = order.FillQty;
                order.SetPartiallyFilledStatus(ref tradeSize);
                TranslateAndSendOldLegacyOrderRecord(socket, UUID, order);
                //DoSend<LegacyOrderRecord>(socket, order);

                if (order.Price.HasValue)
                {
                    DoLog(string.Format("We send the Trade for my other affected {3} order for symbol {0} Side={1} Price={2} Trade Size={4} ", order.InstrumentId, order.cSide == LegacyOrderRecord._SIDE_BUY ? "buy" : "sell", order.Price.Value, order.cStatus, Convert.ToDecimal(order.FillQty - prevFillQty)), MessageType.Information);
                    SendNewTrade(order.cSide, Convert.ToDecimal(order.Price.Value), Convert.ToDecimal(order.FillQty - prevFillQty), socket, instr, UUID);
                }
            }


            DoLog(string.Format("We send the DepthOfBook Message updating the ask price level {0} for symbol {1} (@DepthOfBook and @Orders and we send the message)", bestOffer.Price, bestOffer.Symbol), MessageType.Information);

            TranslateAndSendOldDepthOfBook(socket, bestOffer, instr, UUID);
        }

        private void CreatePriceLevel(char side, decimal price, decimal size, IWebSocketConnection socket, ClientInstrument instr, string UUID)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            DepthOfBook newPriceLevel = new DepthOfBook();
            newPriceLevel.cAction = DepthOfBook._ACTION_INSERT;
            newPriceLevel.cBidOrAsk = side;
            newPriceLevel.DepthTime = Convert.ToInt64(elapsed.TotalMilliseconds);
            newPriceLevel.Index = 0;
            newPriceLevel.Msg = "DepthOfBook";
            newPriceLevel.Price = price;
            newPriceLevel.Sender = 0;
            newPriceLevel.Size = size;
            newPriceLevel.Symbol = instr.InstrumentName;


            DepthOfBooks.Add(newPriceLevel);

            TranslateAndSendOldDepthOfBook(socket, newPriceLevel, instr, UUID);
            //DoSend<DepthOfBook>(socket, newPriceLevel);
        }

        private void EvalNewOrder(IWebSocketConnection socket, ClientOrderReq ordReq, char cStatus, double fillQty, ClientInstrument instr, string UUID)
        {
            lock (Orders)
            {
                TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

                LegacyOrderRecord OyMsg = new LegacyOrderRecord();

                OyMsg.ClientOrderId = ordReq.ClientOrderId;
                OyMsg.cSide = ordReq.cSide;
                OyMsg.cStatus = cStatus;
                OyMsg.cTimeInForce = LegacyOrderRecord._TIMEINFORCE_DAY;
                OyMsg.FillQty = fillQty;
                OyMsg.InstrumentId = instr.InstrumentName;
                OyMsg.LvsQty = Convert.ToDouble(ordReq.Quantity) - fillQty;
                OyMsg.Msg = "LegacyOrderRecord";
                //OyMsg.OrderId = Guid.NewGuid().ToString();
                OyMsg.OrderId = ordReq.ClientOrderId;
                OyMsg.OrdQty = Convert.ToDouble(ordReq.Quantity);
                OyMsg.Price = (double?)ordReq.Price;
                OyMsg.Sender = 0;
                OyMsg.UpdateTime = Convert.ToInt64(elaped.TotalMilliseconds);
                OyMsg.UserId = ordReq.UserId;

                TranslateAndSendOldLegacyOrderRecord(socket, UUID, OyMsg);

                DoLog(string.Format("Creating new order in Orders collection for ClOrderId = {0}", OyMsg.ClientOrderId), MessageType.Information);
                Orders.Add(OyMsg);

                //RefreshOpenOrders(socket, OyMsg.InstrumentId, OyMsg.UserId);

            }
        }

        private void EvalPriceLevelsIfNotTrades(IWebSocketConnection socket, ClientOrderReq ordReq, ClientInstrument instr)
        {
            lock (Orders)
            {
                TimeSpan elaped = DateTime.Now - new DateTime(1970, 1, 1);

                if (ordReq.cSide == LegacyOrderReq._SIDE_BUY)
                {
                    DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._BID_ENTRY
                                                                           && x.Symbol == instr.InstrumentName
                                                                           && x.Price == ordReq.Price).FirstOrDefault();

                    if (existingPriceLevel != null)
                    {

                        DepthOfBook updPriceLevel = new DepthOfBook();
                        updPriceLevel.cAction = DepthOfBook._ACTION_CHANGE;
                        updPriceLevel.cBidOrAsk = DepthOfBook._BID_ENTRY;
                        updPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                        updPriceLevel.Index = existingPriceLevel.Index;
                        updPriceLevel.Msg = existingPriceLevel.Msg;
                        updPriceLevel.Price = existingPriceLevel.Price;
                        updPriceLevel.Sender = existingPriceLevel.Sender;
                        updPriceLevel.Size = existingPriceLevel.Size + ordReq.Quantity;
                        updPriceLevel.Symbol = existingPriceLevel.Symbol;

                        existingPriceLevel.Size = updPriceLevel.Size;
                        DoLog(string.Format("Sending upd bid entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
                        TranslateAndSendOldDepthOfBook(socket, updPriceLevel, instr, ordReq.UUID);
                    }
                    else
                    {

                        DepthOfBook newPriceLevel = new DepthOfBook();
                        newPriceLevel.cAction = DepthOfBook._ACTION_INSERT;
                        newPriceLevel.cBidOrAsk = DepthOfBook._BID_ENTRY;
                        newPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                        newPriceLevel.Index = 0;
                        newPriceLevel.Msg = "DepthOfBook";
                        newPriceLevel.Price = ordReq.Price.HasValue ? ordReq.Price.Value : 0;
                        newPriceLevel.Sender = 0;
                        newPriceLevel.Size = ordReq.Quantity;
                        newPriceLevel.Symbol = instr.InstrumentName;

                        DepthOfBooks.Add(newPriceLevel);
                        DoLog(string.Format("Sending new bid entry: Price={0} Size={1} ...", newPriceLevel.Price, newPriceLevel.Size), MessageType.Information);
                        TranslateAndSendOldDepthOfBook(socket, newPriceLevel, instr, ordReq.UUID);
                    }

                }
                else if (ordReq.cSide == LegacyOrderReq._SIDE_SELL)
                {
                    DepthOfBook existingPriceLevel = DepthOfBooks.Where(x => x.cBidOrAsk == DepthOfBook._ASK_ENTRY
                                                                           && x.Symbol == instr.InstrumentName
                                                                           && x.Price == ordReq.Price).FirstOrDefault();

                    if (existingPriceLevel != null)
                    {

                        DepthOfBook updPriceLevel = new DepthOfBook();
                        updPriceLevel.cAction = DepthOfBook._ACTION_CHANGE;
                        updPriceLevel.cBidOrAsk = DepthOfBook._ASK_ENTRY;
                        updPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                        updPriceLevel.Index = existingPriceLevel.Index;
                        updPriceLevel.Msg = existingPriceLevel.Msg;
                        updPriceLevel.Price = existingPriceLevel.Price;
                        updPriceLevel.Sender = existingPriceLevel.Sender;
                        updPriceLevel.Size = existingPriceLevel.Size + ordReq.Quantity;
                        updPriceLevel.Symbol = existingPriceLevel.Symbol;

                        existingPriceLevel.Size = updPriceLevel.Size;
                        DoLog(string.Format("Sending upd ask entry: Price={0} Size={1} ...", updPriceLevel.Price, updPriceLevel.Size), MessageType.Information);
                        TranslateAndSendOldDepthOfBook(socket, updPriceLevel, instr, ordReq.UUID);
                    }
                    else
                    {

                        DepthOfBook newPriceLevel = new DepthOfBook();
                        newPriceLevel.cAction = DepthOfBook._ACTION_INSERT;
                        newPriceLevel.cBidOrAsk = DepthOfBook._ASK_ENTRY;
                        newPriceLevel.DepthTime = Convert.ToInt64(elaped.TotalMilliseconds);
                        newPriceLevel.Index = 0;
                        newPriceLevel.Msg = "DepthOfBook";
                        newPriceLevel.Price = ordReq.Price.HasValue ? ordReq.Price.Value : 0;
                        newPriceLevel.Sender = 0;
                        newPriceLevel.Size = ordReq.Quantity;
                        newPriceLevel.Symbol = instr.InstrumentName;

                        DepthOfBooks.Add(newPriceLevel);
                        DoLog(string.Format("Sending new ask entry: Price={0} Size={1} ...", newPriceLevel.Price, newPriceLevel.Size), MessageType.Information);
                        TranslateAndSendOldDepthOfBook(socket, newPriceLevel, instr, ordReq.UUID);
                    }

                }
            }
        }

        private bool EvalTrades(ClientOrderReq orderReq,ClientInstrument instr, string UUID, IWebSocketConnection socket)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            lock (Orders)
            {
                //1- Main algo for updating price levels and publish the trades
                if (orderReq.cSide == LegacyOrderReq._SIDE_BUY)
                {
                    DepthOfBook bestAsk = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName &&
                                                                 x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();

                    if (bestAsk == null)
                        return false;

                    DoLog(string.Format("Best ask for buy order (Limit Price={1}) found at price {0}", bestAsk.Price, orderReq.Price.Value), MessageType.Information);
                    if (orderReq.Price.HasValue && orderReq.Price.Value >= bestAsk.Price)
                    {
                        //we had a trade
                        decimal leftQty = orderReq.Quantity;

                        bool fullFill = false;
                        while (leftQty > 0 && bestAsk != null && orderReq.Price.Value >= bestAsk.Price)
                        {
                            decimal prevLeftQty = leftQty;
                            leftQty -= bestAsk.Size;

                            if (leftQty >= 0)
                            {
                                //1.1-We eliminate the price level (@DepthOfBook and @Orders and we send the message)
                                RemovePriceLevel(bestAsk, DepthOfBook._ASK_ENTRY, socket, instr, UUID);

                                //1.2- We send a Trade by bestAsk.Size
                                SendNewTrade(orderReq.cSide, bestAsk.Price, bestAsk.Size, socket, instr, UUID);

                                //1.3-Calculamos el nuevo bestAsk
                                bestAsk = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName
                                                                 && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();

                                fullFill = leftQty == 0;

                            }
                            else//order fully filled
                            {
                                //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
                                UpdatePriceLevel(ref bestAsk, DepthOfBook._ASK_ENTRY, Convert.ToDouble(prevLeftQty), socket, instr, UUID);

                                //1.2- We send a trade by prevLeftQty
                                SendNewTrade(orderReq.cSide, bestAsk.Price, prevLeftQty, socket, instr, UUID);

                                fullFill = true;
                            }

                            bestAsk = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName && x.cBidOrAsk == DepthOfBook._ASK_ENTRY).ToList().OrderBy(x => x.Price).FirstOrDefault();
                        }

                        if (leftQty > 0)
                        {
                            DoLog(string.Format("We create the remaining buy price level {1}  for size {0}", leftQty, orderReq.Price.Value), MessageType.Information);

                            //2-We send the new price level for the remaining order
                            CreatePriceLevel(DepthOfBook._BID_ENTRY, orderReq.Price.Value, leftQty, socket,instr,UUID);
                        }
                        else
                        {
                            DoLog(string.Format("Final leftQty=0 out of loop for buy order at price level {0}", orderReq.Price.Value), MessageType.Information);
                        }


                        //3-We send the full fill/partiall fill for the order --> Oy:LegacyOrderRecord
                        EvalNewOrder(socket, orderReq,
                                     fullFill ? LegacyOrderRecord._STATUS_FILLED : /*LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/LegacyOrderRecord._STATUS_OPEN,
                                     Convert.ToDouble(fullFill ? orderReq.Quantity : orderReq.Quantity - leftQty),
                                     instr,UUID);

                        //4-We update the final quotes state
                        UpdateQuotes(socket,instr,UUID);

                        return true;
                    }
                    else
                    {
                        DoLog(string.Format("Could not find matching sell price for symbol {0} and order price {1}", instr.InstrumentName, orderReq.Price), MessageType.Information);
                        return false;
                    }
                }
                else if (orderReq.cSide == LegacyOrderReq._SIDE_SELL)
                {
                    DepthOfBook bestBid = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName
                                                                  && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();

                    if (bestBid == null)
                        return false;

                    DoLog(string.Format("Best bid for sell order (Limit Price={1}) found at price {0}", bestBid.Price, orderReq.Price.Value), MessageType.Information);
                    if (orderReq.Price.HasValue && orderReq.Price.Value <= bestBid.Price)
                    {
                        //we had a trade
                        decimal leftQty = orderReq.Quantity;

                        bool fullFill = false;
                        while (leftQty > 0 && bestBid != null && orderReq.Price.Value <= bestBid.Price)
                        {
                            decimal prevLeftQty = leftQty;
                            leftQty -= bestBid.Size;

                            if (leftQty >= 0)
                            {
                                //1.1-We eliminate the price level (@DepthOfBook and @Orders and we send the message)
                                RemovePriceLevel(bestBid, DepthOfBook._BID_ENTRY, socket, instr, UUID);

                                //1.2- We send a Trade by bestBid.Size
                                SendNewTrade(orderReq.cSide, bestBid.Price, bestBid.Size, socket, instr, UUID);

                                //1.3-Calculamos el nuevo bestBid
                                bestBid = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName
                                                                 && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();

                                fullFill = leftQty == 0;

                            }
                            else//order fully filled
                            {
                                //1.1- we update the price level to be: Size=mod(leftQty) ->(@DepthOfBook, and we send the message)
                                UpdatePriceLevel(ref bestBid, DepthOfBook._BID_ENTRY, Convert.ToDouble(prevLeftQty), socket,instr,UUID);

                                //1.2- We send a trade by prevLeftQty
                                SendNewTrade(orderReq.cSide, bestBid.Price, prevLeftQty, socket,instr, UUID);

                                fullFill = true;
                            }

                            bestBid = DepthOfBooks.Where(x => x.Symbol == instr.InstrumentName && x.cBidOrAsk == DepthOfBook._BID_ENTRY).ToList().OrderByDescending(x => x.Price).FirstOrDefault();
                        }

                        if (leftQty > 0)
                        {
                            DoLog(string.Format("We create the remaining buy price level {1}  for size {0}", leftQty, orderReq.Price.Value), MessageType.Information);

                            //2-We send the new price level for the remaining order
                            CreatePriceLevel(DepthOfBook._ASK_ENTRY, orderReq.Price.Value, leftQty, socket,instr,UUID);
                        }
                        else
                        {
                            DoLog(string.Format("Final leftQty=0 out of loop for sell order at price level {0}", orderReq.Price.Value), MessageType.Information);
                        }

                        //3-We send the full fill/partiall fill for the order --> Oy:LegacyOrderRecord
                        EvalNewOrder(socket, orderReq,
                                     fullFill ? LegacyOrderRecord._STATUS_FILLED : /* LegacyOrderRecord._STATUS_PARTIALLY_FILLED*/ LegacyOrderRecord._STATUS_OPEN,
                                     Convert.ToDouble(fullFill ? orderReq.Quantity : orderReq.Quantity - leftQty),
                                     instr,UUID);

                        //4-We update the final quotes state
                        UpdateQuotes(socket, instr,UUID);
                        return true;
                    }
                    else
                    {
                        DoLog(string.Format("Could not find matching buy price for symbol {0} and order price {1}", instr.InstrumentName, orderReq.Price), MessageType.Information);
                        return false;
                    }
                }
                else
                {
                    DoLog(string.Format("Could not process side {1} for symbol {0} ", instr.InstrumentName, orderReq.cSide), MessageType.Information);
                    return false;
                }
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
                    OrderId = 0,
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

        private void ProcessFirmsCreditLimitUpdateRequest(IWebSocketConnection socket, string m)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            FirmsCreditLimitUpdateRequest wsFirmCreditLimitUpdRq = JsonConvert.DeserializeObject<FirmsCreditLimitUpdateRequest>(m);


            if (FirmListResp != null)
            {
                ClientFirmRecord firm = FirmListResp.Firms.Where(x => x.Id == wsFirmCreditLimitUpdRq.FirmId).FirstOrDefault();

                if (firm != null)
                {
                    try
                    {
                        firm.CreditLimit[0].TradingStatus = wsFirmCreditLimitUpdRq.TradingStatus;
                        firm.CreditLimit[0].Total = wsFirmCreditLimitUpdRq.CreditLimitTotal;
                        //Balance = Total - Usage
                        firm.CreditLimit[0].Usage = wsFirmCreditLimitUpdRq.CreditLimitUsage;
                        firm.CreditLimit[0].MaxTradeSize = wsFirmCreditLimitUpdRq.CreditLimitMaxTradeSize;

                        if (wsFirmCreditLimitUpdRq.CreditLimitBalance != (wsFirmCreditLimitUpdRq.CreditLimitTotal - wsFirmCreditLimitUpdRq.CreditLimitUsage))
                            throw new Exception("Balance must be Total - Usage");

                        FirmsCreditLimitUpdateResponse resp = new FirmsCreditLimitUpdateResponse()
                        {
                            cSuccess = FirmsCreditLimitUpdateResponse._SUCCESS_TRUE,
                            FirmId = wsFirmCreditLimitUpdRq.FirmId,
                            JsonWebToken = wsFirmCreditLimitUpdRq.JsonWebToken,
                            Message = "success",
                            Msg = "FirmsCreditLimitUpdateResponse",
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            UUID = wsFirmCreditLimitUpdRq.UUID,
                            Firm = firm
                        };

                        DoSend<FirmsCreditLimitUpdateResponse>(socket, resp);

                        FirmsCreditLimitRecord newCreditLimit = new FirmsCreditLimitRecord()
                        {
                            Msg = "FirmsCreditLimitRecord",
                            Firm = firm,
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            UUID = wsFirmCreditLimitUpdRq.UUID
                        };

                        DoSend<FirmsCreditLimitRecord>(socket, newCreditLimit);
                    }
                    catch (Exception ex)
                    {
                        FirmsCreditLimitUpdateResponse resp = new FirmsCreditLimitUpdateResponse()
                        {
                            cSuccess = FirmsCreditLimitUpdateResponse._SUCCESS_FALSE,
                            FirmId = wsFirmCreditLimitUpdRq.FirmId,
                            JsonWebToken = wsFirmCreditLimitUpdRq.JsonWebToken,
                            Message = string.Format("Error updating firm Id {0} not found:{1}", wsFirmCreditLimitUpdRq.FirmId, ex.Message),
                            Msg = "FirmsCreditLimitUpdateResponse",
                            Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                            UUID = wsFirmCreditLimitUpdRq.UUID
                        };

                        DoSend<FirmsCreditLimitUpdateResponse>(socket, resp);

                    }
                }
                else
                {
                    FirmsCreditLimitUpdateResponse resp = new FirmsCreditLimitUpdateResponse()
                    {
                        cSuccess = FirmsCreditLimitUpdateResponse._SUCCESS_FALSE,
                        FirmId = wsFirmCreditLimitUpdRq.FirmId,
                        JsonWebToken = wsFirmCreditLimitUpdRq.JsonWebToken,
                        Message = string.Format("Firm Id {0} not found", wsFirmCreditLimitUpdRq.FirmId),
                        Msg = "FirmsCreditLimitUpdateResponse",
                        Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                        UUID = wsFirmCreditLimitUpdRq.UUID

                    };

                    DoSend<FirmsCreditLimitUpdateResponse>(socket, resp);
                }
            }
            else
            {
                FirmsCreditLimitUpdateResponse resp = new FirmsCreditLimitUpdateResponse()
                {
                    cSuccess = FirmsCreditLimitUpdateResponse._SUCCESS_FALSE,
                    FirmId = wsFirmCreditLimitUpdRq.FirmId,
                    JsonWebToken = wsFirmCreditLimitUpdRq.JsonWebToken,
                    Message = string.Format("You must invoke FirmListRequest before invoking CreditLimitUpdateRequest "),
                    Msg = "FirmsCreditLimitUpdateResponse",
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    UUID = wsFirmCreditLimitUpdRq.UUID

                };

                DoSend<FirmsCreditLimitUpdateResponse>(socket, resp);
            
            }
        
        }

        private void CreateFirmListCreditStructure(string UUID, string token, int pageNo,int pageRecords)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            Dictionary<string, ClientFirmRecord> firms = new Dictionary<string, ClientFirmRecord>();
            List<ClientFirmRecord> finalList = new List<ClientFirmRecord>();

            foreach (AccountRecord accRecord in AccountRecords)
            {
                if (!firms.ContainsKey(accRecord.EPFirmId))
                {
                    //1- We create the accounts list
                    List<AccountRecord> firmAccounts = AccountRecords.Where(x => x.EPFirmId == accRecord.EPFirmId).ToList();
                    List<ClientAccountRecord> v2accountList = new List<ClientAccountRecord>();
                    firmAccounts.ForEach(x => v2accountList.Add(GetClientAccountRecordFromV1AccountRecord(x)));

                    //2- We creat the credit list
                    List<CreditUICreditLimit> creditLimits = new List<CreditUICreditLimit>();
                    CreditRecordUpdate creditRecord = CreditRecordUpdates.Where(x => x.FirmId == accRecord.EPFirmId).ToList().FirstOrDefault();
                    DGTLBackendMock.Common.DTO.Account.AccountRecord defaultAccount = AccountRecords.Where(x => x.EPFirmId == accRecord.EPFirmId && x.Default).FirstOrDefault();
                    CreditUICreditLimit creditLimit = new CreditUICreditLimit()
                    {
                        CurrencyRootId = accRecord.RouteId,
                        cTradingStatus = CreditUICreditLimit._TRADING_STATUS_TRUE,
                        FirmCreditId = accRecord.EPFirmId,
                        MaxQtySize = accRecord.MaxNotional,
                        MaxTradeSize = accRecord.MaxNotional,
                        PotentialExposure = accRecord.MaxNotional,
                        Total = defaultAccount != null ? defaultAccount.CreditLimit : -1,
                        Usage = creditRecord != null ? creditRecord.CreditUsed : 0
                    };

                    creditLimits.Add(creditLimit);

                    ClientFirmRecord firm = new ClientFirmRecord()
                    {
                        Id = Convert.ToInt64(accRecord.EPFirmId),
                        Name = accRecord.CFirmName,
                        ShortName = accRecord.CFSortName,
                        Accounts = v2accountList.ToArray(),
                        CreditLimit = creditLimits.ToArray()
                    };

                    firms.Add(accRecord.EPFirmId, firm);
                    finalList.Add(firm);
                }
            }


            double totalPages = Math.Ceiling(Convert.ToDouble(finalList.Count / pageRecords));
            FirmListResp = new FirmsListResponse()
            {
                Msg = "FirmsListResponse",
                cSuccess = FirmsListResponse._SUCCESS_TRUE,
                Firms = finalList.Skip(pageNo * pageRecords).Take(pageRecords).ToArray(),
                JsonWebToken = token,
                UUID = UUID,
                Message = "success",
                PageNo = pageNo,
                Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                TotalPages = Convert.ToInt32(totalPages),
            };
        
        }

        private void ProcessFirmsListRequest (IWebSocketConnection socket, string m)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            FirmsListRequest wsFirmListRq = JsonConvert.DeserializeObject<FirmsListRequest>(m);
            try
            {
                if(FirmListResp==null)
                    CreateFirmListCreditStructure(wsFirmListRq.UUID, wsFirmListRq.JsonWebToken, wsFirmListRq.PageNo, wsFirmListRq.PageRecords);
                DoSend<FirmsListResponse>(socket, FirmListResp);
            }
            catch (Exception ex)
            {

                FirmsListResponse firmListResp = new FirmsListResponse()
                {
                    Msg = "FirmsListResponse",
                    cSuccess = FirmsListResponse._SUCCESS_FALSE,
                    JsonWebToken = wsFirmListRq.JsonWebToken,
                    UUID = wsFirmListRq.UUID,
                    Message = ex.Message,
                    PageNo = wsFirmListRq.PageNo,
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    TotalPages = 0,
                };

                DoSend<FirmsListResponse>(socket, firmListResp);
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
                cSuccess = TokenResponse._STATUS_OK,
                Time = wsTokenReq.Time
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
                Success = false,
                Time = wsLogin.Time,
                UserId = 0
            };

            DoSend<ClientLoginResponse>(socket, reject);
        }

        private void SendCRMInstruments(IWebSocketConnection socket, string Uuid)
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
            UserRecord userRecord = UserRecords.Where(x => x.UserId == login).FirstOrDefault();

            if (userRecord != null)
            {
                ClientUserRecord userRecordMsg = new ClientUserRecord();
                userRecordMsg.Address = "";
                userRecordMsg.cConnectionType = '0';
                userRecordMsg.City = "";
                userRecordMsg.cUserType = '0';
                userRecordMsg.Email = "";
                userRecordMsg.FirmId = Convert.ToInt64(userRecord.FirmId);
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

        private void SendMarketStatus(IWebSocketConnection socket, string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);

            ClientMarketState marketStateMsg = new ClientMarketState();
            marketStateMsg.cExchangeId = ClientMarketState._DEFAULT_EXCHANGE_ID;
            marketStateMsg.cReasonCode = '0';
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

        private ClientAccountRecord GetClientAccountRecordFromV1AccountRecord(DGTLBackendMock.Common.DTO.Account.AccountRecord accountRecord)
        {
            ClientAccountRecord v2AccountRecordMsg = new ClientAccountRecord();
            v2AccountRecordMsg.Msg = "ClientAccountRecord";
            v2AccountRecordMsg.AccountId = accountRecord.AccountId;
            v2AccountRecordMsg.FirmId = Convert.ToInt64(accountRecord.EPFirmId);
            v2AccountRecordMsg.SettlementFirmId = "1";
            v2AccountRecordMsg.AccountName = accountRecord.EPNickName;
            v2AccountRecordMsg.AccountAlias = accountRecord.AccountId;


            v2AccountRecordMsg.AccountNumber = "";
            v2AccountRecordMsg.AccountType = 0;
            v2AccountRecordMsg.cStatus = ClientAccountRecord._STATUS_ACTIVE;
            v2AccountRecordMsg.cUserType = ClientAccountRecord._DEFAULT_USER_TYPE;
            v2AccountRecordMsg.cCti = ClientAccountRecord._CTI_OTHER;
            v2AccountRecordMsg.Lei = "";
            v2AccountRecordMsg.Currency = "";
            v2AccountRecordMsg.IsSuspense = false;
            v2AccountRecordMsg.UsDomicile = true;
            v2AccountRecordMsg.UpdatedAt = 0;
            v2AccountRecordMsg.CreatedAt = 0;
            v2AccountRecordMsg.LastUpdatedBy = "";
            v2AccountRecordMsg.WalletAddress = "";
            //accountRecordMsg.UUID = Uuid;

            return v2AccountRecordMsg;
        
        }

        private void SendCRMAccounts(IWebSocketConnection socket, string login, string Uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            UserRecord userRecord = UserRecords.Where(x => x.UserId == login).FirstOrDefault();

            if (userRecord != null)
            {
                List<DGTLBackendMock.Common.DTO.Account.AccountRecord> accountRecords = AccountRecords.Where(x => x.EPFirmId == userRecord.FirmId).ToList();

                List<ClientAccountRecord> accRecordList = new List<ClientAccountRecord>();
                foreach (DGTLBackendMock.Common.DTO.Account.AccountRecord accountRecord in accountRecords)
                {
                    ClientAccountRecord accountRecordMsg = GetClientAccountRecordFromV1AccountRecord(accountRecord);
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

        private void SendCRMMessages(IWebSocketConnection socket, string login, string Uuid = null)
        {
            SendCRMInstruments(socket, Uuid);

            SendCRMUsers(socket, login, Uuid);

            SendMarketStatus(socket, Uuid);

            SendCRMAccounts(socket, login, Uuid);
        }

        #endregion

        #region Protected Methods

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
        
        protected void ProcessClientLoginV2(IWebSocketConnection socket, string m)
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
                    Success = true,
                    Time = wsLogin.Time,
                    UserId = GUIDToLongConverter.GUIDToLong(memUserRecord.DeskId)
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

        protected void ProcessClientOrderCancelReq(IWebSocketConnection socket, string m)
        {

            DoLog(string.Format("Processing ClientOrderCancelReq"), MessageType.Information);
            ClientOrderCancelReq ordCxlReq = JsonConvert.DeserializeObject<ClientOrderCancelReq>(m);

            //TODO: Implement cancellation rejections
            try
            {
                lock (Orders)
                {
                    TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

                    DoLog(string.Format("Searching order by ClientOrderId={0} )", ordCxlReq.ClientOrderId), MessageType.Information);
                    LegacyOrderRecord order = Orders.Where(x => x.ClientOrderId == ordCxlReq.ClientOrderId).FirstOrDefault();

                    if (order != null)
                    {

                        ClientInstrument instr = GetInstrumentBySymbol(order.InstrumentId);

                        //1-We send el CancelAck
                        ClientOrderCancelResponse ack = new ClientOrderCancelResponse();
                        ack.ClientOrderId = ordCxlReq.ClientOrderId;
                        ack.FirmId = ordCxlReq.FirmId;
                        ack.Message = "Just cancelled @ mock v2";
                        ack.Msg = "ClientOrderCancelResponse";
                        ack.OrderId = GUIDToLongConverter.GUIDToLong(order.OrderId);
                        ack.Success = true;
                        ack.TimeStamp = Convert.ToInt64(elapsed.TotalMilliseconds);
                        ack.UserId = ordCxlReq.UserId;
                        ack.UUID = ordCxlReq.UUID;

                        DoLog(string.Format("Sending cancellation ack for ClOrdId: {0}", ordCxlReq.ClientOrderId), MessageType.Information);
                        DoSend<ClientOrderCancelResponse>(socket, ack);


                        //2-Actualizamos el PL
                        DoLog(string.Format("Evaluating price levels for ClOrdId: {0}", ordCxlReq.ClientOrderId), MessageType.Information);
                        EvalPriceLevels(socket, new ClientOrderRecord() { InstrumentId = instr.InstrumentId, Price = order.Price, cSide = order.cSide, LeavesQty = order.LvsQty }, ordCxlReq.UUID);

                        //3-Upd orders in mem
                        DoLog(string.Format("Updating orders in mem"), MessageType.Information);
                        Orders.Remove(order);

                        //4- Update Quotes
                        DoLog(string.Format("Updating quotes on order cancelation"), MessageType.Information);
                        UpdateQuotes(socket, instr, ordCxlReq.UUID);

                        //5-
                        //RefreshOpenOrders(socket, ordCxlReq.InstrumentId, ordCxlReq.UserId);

                        //6-Send LegacyOrderRecord
                        CanceledLegacyOrderREcord(order, socket);

                    }
                    else
                    {
                        //TODO: LegacyOrderCancelRejAck
                        DoLog(string.Format("Rejecting cancelation because client orderId not found: {0}", ordCxlReq.ClientOrderId), MessageType.Information);
                        //RefreshOpenOrders(socket, ordCxlReq.InstrumentId, ordCxlReq.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO: LegacyOrderCancelRejAck
                DoLog(string.Format("Rejecting cancelation because of an error: {0}", ex.Message), MessageType.Information);
            }
        


        }

        protected void ProcessOrderReqMock(IWebSocketConnection socket, string m)
        {
            try
            {
                lock (Orders)
                {
                    TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                    ClientOrderReq clientOrderReq = JsonConvert.DeserializeObject<ClientOrderReq>(m);

                    if (!ProcessRejectionsForNewOrders(clientOrderReq, socket))
                    {

                        ClientInstrument instr =  GetInstrumentByServiceKey(clientOrderReq.InstrumentId.ToString());

                        ClientOrderResponse clientOrdAck = new ClientOrderResponse()
                        {
                            Msg = "ClientOrderResponse",
                            ClientOrderId = clientOrderReq.ClientOrderId,
                            InstrumentId = clientOrderReq.InstrumentId,
                            Message = "success",
                            Success = true,
                            OrderId = GUIDToLongConverter.GUIDToLong(Guid.NewGuid().ToString()),
                            UserId=clientOrderReq.UserId,
                            Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                            UUID = clientOrderReq.UUID
                        };
                        DoLog(string.Format("Sending ClientOrderResponse ..."), MessageType.Information);
                        DoSend<ClientOrderResponse>(socket, clientOrdAck);

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



                        if (!EvalTrades(clientOrderReq, instr, clientOrderReq.UUID, socket))
                        {
                            DoLog(string.Format("Evaluating price levels ..."), MessageType.Information);
                            EvalPriceLevelsIfNotTrades(socket, clientOrderReq, instr);
                            DoLog(string.Format("Evaluating LegacyOrderRecord ..."), MessageType.Information);
                            EvalNewOrder(socket, clientOrderReq, LegacyOrderRecord._STATUS_OPEN, 0, instr, clientOrderReq.UUID);
                            DoLog(string.Format("Updating quotes ..."), MessageType.Information);
                            UpdateQuotes(socket, instr, clientOrderReq.UUID);
                        }
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


        protected ClientInstrument GetInstrumentByIntInstrumentId(long instrumentId)
        {

            ClientInstrument instr = InstrBatch.messages.Where(x => x.InstrumentId == instrumentId).FirstOrDefault();
            return instr;
        }


        protected ClientInstrument GetInstrumentByServiceKey(string serviceKey)
        {
            if (InstrBatch == null)
                throw new Exception("Initial load for instrument not finished!");

            long filterIntrId=0;
            try
            {
                filterIntrId = Convert.ToInt64(serviceKey);
            }
            catch(Exception ex)
            {
                throw new Exception(string.Format("Wrong format for instrumentId (serviceKey): {}. The instrumentId has to be an integer",serviceKey));
            }

            return GetInstrumentByIntInstrumentId(filterIntrId);
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
                    UserId = LoggedUserId,
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
                    UserId = LoggedUserId,
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
                    Offer = legacyLastQuote.Ask,
                    OfferSize = legacyLastQuote.AskSize,
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

        private void TranslateAndSendOldCreditRecordUpdate(IWebSocketConnection socket, Subscribe subscrMsg)
        {
            if(FirmListResp==null)
                CreateFirmListCreditStructure(subscrMsg.UUID, subscrMsg.JsonWebToken, 0, 10000);

            ClientFirmRecord firm = FirmListResp.Firms.Where(x => x.Id == Convert.ToInt32(subscrMsg.ServiceKey)).FirstOrDefault();

            if (firm != null)
            {
               

                TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
                ClientCreditUpdate ccUpd = new ClientCreditUpdate()
                {
                    Msg = "ClientCreditUpdate",
                    AccountId = 0,
                    CreditLimit = firm.CreditLimit[0].Total,
                    CreditUsed = firm.CreditLimit[0].Usage,
                    cStatus = firm.CreditLimit[0].cTradingStatus,
                    cUpdateReason = ClientCreditUpdate._UPDATE_REASON_DEFAULT,
                    FirmId = firm.Id,
                    MaxNotional = firm.CreditLimit[0].MaxTradeSize,
                    Timestamp = Convert.ToInt64(elapsed.TotalMilliseconds),
                    UUID = subscrMsg.UUID
                };

                DoSend<ClientCreditUpdate>(socket, ccUpd);
            }
            else
                throw new Exception(string.Format("Unknown firm {0}", subscrMsg.ServiceKey));
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

                        TranslateAndSendOldQuote(socket, subscrMsg.UUID, legacyLastQuote, instr);
                        Thread.Sleep(3000);//3 seconds
                    }
                    if (!subscResp)
                    {
                        ProcessSubscriptionResponse(socket, "LQ", subscrMsg.ServiceKey, subscrMsg.UUID);
                        Thread.Sleep(2000);
                        subscResp = true;
                        SubscribedLQ = true;
                    }

                    if (legacyLastQuote == null)
                        break;
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
                        //DoSend<Quote>(socket, quote);

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
            try
            {
                TranslateAndSendOldCreditRecordUpdate(socket, subscrMsg);
                ProcessSubscriptionResponse(socket, "T", subscrMsg.ServiceKey, subscrMsg.UUID);
            }
            catch (Exception ex)
            {

                ProcessSubscriptionResponse(socket, "T", subscrMsg.ServiceKey, subscrMsg.UUID, success: false, msg: ex.Message);
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
                Success = true,
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
                else if (wsResp.Msg == "FirmsListRequest")
                {
                    ProcessFirmsListRequest(socket, m);
                }
                else if (wsResp.Msg == "FirmsCreditLimitUpdateRequest")
                {
                    ProcessFirmsCreditLimitUpdateRequest(socket, m);
                }
                else if (wsResp.Msg == "ClientHeartbeat")
                {
                    //We do nothing//

                }
                else if (wsResp.Msg == "ClientOrderReq")
                {

                    ProcessOrderReqMock(socket, m);

                }
                else if (wsResp.Msg == "ClientOrderCancelReq")
                {
                    ProcessClientOrderCancelReq(socket, m);
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
