﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Auth;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Bitmex.BusinessEntities;
using zHFT.OrderRouters.Bitmex.Common.DTO.Events;
using zHFT.OrderRouters.Bitmex.Common.Wrappers;
using zHFT.OrderRouters.Bitmex.DataAccessLayer;
using zHFT.OrderRouters.Cryptos;
using zHFT.StrategyHandler.Common.Converters;

namespace zHFT.OrderRouters.Bitmex
{
    public class OrderRouter : BaseOrderRouter
    {
        #region Protected Attributes

        protected Common.Configuration.Configuration BitmexConfiguration { get; set; }

        protected OrderManager OrderManager { get; set; }

        protected  zHFT.OrderRouters.Bitmex.DataAccessLayer.Websockets.OrderManager WSOrderManager { get; set; }

        protected Dictionary<string, Order> BitMexActiveOrders { get; set; }

        protected List<Security> Securities { get; set; }

        protected SecurityListConverter SecurityListConverter { get; set; }



        #endregion


        #region Overriden Methods

        protected override BaseConfiguration GetConfig()
        {
            return BitmexConfiguration;
        }

        protected override string GetQuoteCurrency()
        {
            return null;
        }

        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            BitmexConfiguration = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);

        }

        #endregion

      
        #region Protected Methods

        protected void HandleGenericSubscription(WebSocketResponseMessage WebSocketResponseMessage)
        {
            WebSocketSubscriptionResponse resp = (WebSocketSubscriptionResponse)WebSocketResponseMessage;

            if (resp.success)
                DoLog(string.Format("Successfully subscribed to {0} event ",
                                            resp.GetSubscriptionEvent()), Main.Common.Util.Constants.MessageType.Information);
            else
                DoLog(string.Format("Error on subscription to {0} event:{!}",
                                            resp.GetSubscriptionEvent(), resp.error), Main.Common.Util.Constants.MessageType.Error);
        }

        protected void ExecutionReportSubscriptionResponse(WebSocketResponseMessage WebSocketResponseMessage)
        {
            HandleGenericSubscription(WebSocketResponseMessage);
        }

        protected void ProcessExecutionReports(WebSocketSubscriptionEvent subscrEvent)
        {
            WebSocketExecutionReportEvent reports = (WebSocketExecutionReportEvent)subscrEvent;
            foreach (zHFT.OrderRouters.Bitmex.Common.DTO.ExecutionReport execReportDTO in reports.data)
            {
                try
                {

                    string clOrdId = "";
                    if (OrderIdMappers.ContainsKey(execReportDTO.OrderID))
                    {
                        clOrdId = OrderIdMappers[execReportDTO.OrderID];

                        if (BitMexActiveOrders.ContainsKey(clOrdId))
                        {
                            Order order = BitMexActiveOrders[clOrdId];

                            lock (tLock)
                            {

                                if (execReportDTO.ExecType == ExecType.New.ToString())
                                {
                                    order.OrderId = execReportDTO.OrderID;
                                }

                                if (execReportDTO.ExecType == ExecType.Replaced.ToString())
                                {
                                    BitMexActiveOrders.Remove(clOrdId);
                                    order.ClOrdId = order.PendingClOrdId;
                                    order.PendingClOrdId = null;
                                    BitMexActiveOrders.Add(order.ClOrdId, order);
                                    OrderIdMappers[execReportDTO.OrderID] = order.ClOrdId;
                                }

                                if (execReportDTO.ExecType == ExecType.Canceled.ToString() ||
                                    execReportDTO.ExecType == ExecType.Stopped.ToString() ||
                                    execReportDTO.ExecType == ExecType.Rejected.ToString() ||
                                    execReportDTO.ExecType == ExecType.Suspended.ToString() ||
                                    execReportDTO.ExecType == ExecType.Expired.ToString())
                                {

                                    BitMexActiveOrders.Remove(order.ClOrdId);
                                    OrderIdMappers.Remove(order.OrderId);
                                }
                            }

                            WSExecutionReportWrapper wrapper = new WSExecutionReportWrapper(execReportDTO, order);
                            OnMessageRcv(wrapper);
                        }
                    }
                    else
                        DoLog(string.Format("Unknown order processing execution report for OrderId {0} ", execReportDTO.OrderID), Main.Common.Util.Constants.MessageType.Information);

                }
                catch (Exception ex)
                {
                    DoLog(string.Format("Error processing execution report for ClOrdId {0}:{1} ", execReportDTO.ClOrdID, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        protected Order GetOrder(Wrapper wrapper)
        {
            string symbol = (string)wrapper.GetField(OrderFields.Symbol);
            decimal? price = (decimal?)wrapper.GetField(OrderFields.Price);
            Side side = (Side)wrapper.GetField(OrderFields.Side);
            decimal orderQty = (decimal)wrapper.GetField(OrderFields.OrderQty);
            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
            //int decimalPrecission = (int)wrapper.GetField(OrderFields.DecimalPrecission);

            //orderQty tiene el valor monetario en USD de la posición
            //tenemos que obtener la cantidad de contratos a comprar

            Order order = new Order()
            {
                SymbolPair = symbol,
                Price = Convert.ToDecimal(price),
                Side = side,
                OrderQty = orderQty,
                //Currency = GetQuoteCurrency(),
                OrdType = OrdType.Limit,
                ClOrdId = clOrderId,
            };

            return order;
        }

        protected ExecutionReport GetRejectedExecutionReport(Order order,string error)
        {

            ExecutionReport rejectedER = new ExecutionReport();
            rejectedER.OrderID = order.OrderId;
            rejectedER.ClOrdID = order.ClOrdId;
            rejectedER.Symbol = order.SymbolPair;
            rejectedER.Side = order.Side.ToString();
            rejectedER.OrderQty = order.OrderQty;
            rejectedER.Price = order.Price.HasValue ? (double?)Convert.ToDouble(order.Price.Value) : null ;
            rejectedER.StopPx = order.StopPx.HasValue ? (double?)Convert.ToDouble(order.StopPx.Value) : null;
            rejectedER.Currency = order.Currency;
            rejectedER.OrdType = order.OrdType.ToString();
            rejectedER.ExecType = ExecType.Rejected.ToString();
            rejectedER.OrdStatus = OrdStatus.Rejected.ToString();
            rejectedER.OrdRejReason = OrdRejReason.Other.ToString();
            rejectedER.Text = error;
            rejectedER.Timestamp = DateTime.Now;
            rejectedER.TransactTime = DateTime.Now;
            rejectedER.Order = order;

            return rejectedER;
        }

        private void RunUpperTick(Order order)
        {
            if (order.Side == Side.Buy)
                order.Price += 0.5m;
            else
                order.Price -= 0.5m;
        
        
        }

        protected override CMState RouteNewOrder(Wrapper wrapper)
        {
            try
            {
                Order order = GetOrder(wrapper);
                try
                {
                    lock (tLock)
                    {
                        //RunUpperTick(order);
                        ExecutionReport exRep = OrderManager.PlaceOrder(order);
                        BitMexActiveOrders.Add(order.ClOrdId, order);
                        if (exRep.OrderID!=null)
                            OrderIdMappers.Add(exRep.OrderID, order.ClOrdId);
                        //The new ER will arrive through the websockets
                        //ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(exRep, order);
                        //OnMessageRcv(erWrapper);
                    }
                }
                catch (Exception ex)
                {

                    ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(GetRejectedExecutionReport(order, ex.Message), order);
                    OnMessageRcv(erWrapper);
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }

        }

        protected override CMState UpdateOrder(Wrapper wrapper)
        {

            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();
            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
            double? price = (double?)wrapper.GetField(OrderFields.Price);
            try
            {

                if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                    throw new Exception("Could not find OrigClOrdID for order updated");

                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                lock (tLock)
                {

                    if (BitMexActiveOrders.ContainsKey(origClOrderId) && price.HasValue)
                    {
                        Order order = BitMexActiveOrders[origClOrderId];
                        order.Price = Convert.ToDecimal(price.Value);
                        order.PendingClOrdId = clOrderId;
                        ExecutionReport exRep = OrderManager.UpdateOrder(order);
                        

                        //ExecutionReportWrapper exWrapper = new ExecutionReportWrapper(exRep, order);
                        //OnMessageRcv(exWrapper);
                    }
                    else
                        DoLog(string.Format("@{0}:Could not find order for origClOrderId  {1}!", BitmexConfiguration.Name, origClOrderId), Main.Common.Util.Constants.MessageType.Error);

                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error updating order {1}!:{2}", BitmexConfiguration.Name, origClOrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected override bool RunCancelOrder(zHFT.Main.BusinessEntities.Orders.Order order, bool update)
        {
            throw new NotImplementedException("Not implemented");
        }

        protected ExecutionReport RunCancelOrder(Order order, bool update)
        {
            ExecutionReport exReport = OrderManager.CancelOrder(order);
            //ExecutionReportWrapper wrapper = new ExecutionReportWrapper(exReport, order);
            //OnMessageRcv(wrapper);
            return exReport;
        }

        protected void ProcessCancelationError(ExecutionReport exReport, Order order, Wrapper wrapper)
        {
            if (!string.IsNullOrEmpty(exReport.error))
            {
                OrderCancelRejectWrapper reject = new OrderCancelRejectWrapper(order.OrderId,
                                            wrapper.GetField(OrderFields.OrigClOrdID).ToString(),
                                            wrapper.GetField(OrderFields.ClOrdID).ToString(),
                                            OrdStatus.Rejected,
                                            DateTime.Now,
                                            CxlRejReason.UnknownOrder,
                                            exReport.error,order.SymbolPair);
                OnMessageRcv(reject);
            }
        }

        protected CMState CancelOrderOnClOrderId(string  origClOrderId   ,Wrapper wrapper)
        {
            try
            {
                //New order id
                string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

                lock (tLock)
                {

                    if (BitMexActiveOrders.ContainsKey(origClOrderId))
                    {
                        Order order = BitMexActiveOrders[origClOrderId];
                        ExecutionReport exReport = RunCancelOrder(order, false);
                        ProcessCancelationError(exReport, order, wrapper);
                    }
                    else
                    {
                        string orderId = wrapper.GetField(OrderFields.OrderId) != OrderFields.NULL ? wrapper.GetField(OrderFields.OrderId).ToString() : "";
                        OrderCancelRejectWrapper reject = new OrderCancelRejectWrapper(orderId,
                                                                                        origClOrderId,
                                                                                        clOrderId,
                                                                                        OrdStatus.Rejected,
                                                                                        DateTime.Now,
                                                                                        CxlRejReason.UnknownOrder,
                                                                                        string.Format("ClOrdId not found: {0}", origClOrderId),
                                                                                        wrapper.GetField(OrderFields.Symbol).ToString());
                        OnMessageRcv(reject);
                        throw new Exception(string.Format("@{0}: Could not cancel order for OrigClOrdId {1} because it was not found", BitmexConfiguration.Name, origClOrderId));
                    }
                }
                return CMState.BuildSuccess();

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelig order {1}!:{2}", BitmexConfiguration.Name, origClOrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected CMState CancelOrderOnOrderId(string orderId, Wrapper wrapper)
        {
            try
            {
                lock (tLock)
                {

                    Order order = BitMexActiveOrders.Values.Where(x => x.OrderId == orderId).FirstOrDefault();

                    if (order!=null)
                    {
                        ExecutionReport exReport = RunCancelOrder(order, false);
                        ProcessCancelationError(exReport, order, wrapper);
                    }
                    else
                    {
                      
                        OrderCancelRejectWrapper reject = new OrderCancelRejectWrapper(orderId,
                                                                wrapper.GetField(OrderFields.OrigClOrdID).ToString(),
                                                                wrapper.GetField(OrderFields.ClOrdID).ToString(),
                                                                OrdStatus.Rejected,
                                                                DateTime.Now,
                                                                CxlRejReason.UnknownOrder,
                                                                string.Format("OrderId not found: {0}", orderId),
                                                                wrapper.GetField(OrderFields.Symbol).ToString());
                        OnMessageRcv(reject);



                        throw new Exception(string.Format("@{0}: Could not cancel order for OrderId {1} because it was not found", BitmexConfiguration.Name, orderId));
                    }
                }
                return CMState.BuildSuccess();

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelig order {1}!:{2}", BitmexConfiguration.Name, orderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected override CMState CancelOrder(Wrapper wrapper)
        {
            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();

            if (origClOrderId != null)
                return CancelOrderOnClOrderId(origClOrderId, wrapper);
            else
            {
                string orderId = wrapper.GetField(OrderFields.OrderId).ToString();

                if (orderId != null)
                    return CancelOrderOnOrderId(orderId, wrapper);
                else
                    throw new Exception("Could not cancel an order if you don't specify a ClOrderId or OrderId");
            }
        }

        protected override CMState ProcessSecurityList(Wrapper wrapper)
        {
            try
            {
                SecurityList secList = SecurityListConverter.GetSecurityList(wrapper, Config);
                Securities = secList.Securities;

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        #endregion

        #region Public Methods

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.ModuleConfigFile = configFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tLock = new object();

                    BitMexActiveOrders = new Dictionary<string,Order>();
                    OrderIdMappers = new Dictionary<string, string>();
                    OrderManager = new DataAccessLayer.OrderManager(BitmexConfiguration.RESTURL, BitmexConfiguration.ApiKey, BitmexConfiguration.Secret);
                    WSOrderManager = new DataAccessLayer.Websockets.OrderManager(BitmexConfiguration.WebsocketURL, new UserCredentials() { BitMexID = BitmexConfiguration.ApiKey, BitMexSecret = BitmexConfiguration.Secret });
                    SecurityListConverter = new StrategyHandler.Common.Converters.SecurityListConverter();

                    WSOrderManager.SubscribeResponseRequest(
                                                         DataAccessLayer.Websockets.OrderManager._EXECUTIONS,
                                                         ExecutionReportSubscriptionResponse,
                                                         new object[] { });

                    WSOrderManager.SubscribeEvents(DataAccessLayer.Websockets.OrderManager._EXECUTIONS, ProcessExecutionReports);

                    WSOrderManager.SubscribeExecutions();

                    SecurityListRequestWrapper slWrapper = new SecurityListRequestWrapper(SecurityListRequestType.AllSecurities, null);
                    OnMessageRcv(slWrapper);
                    
                    CanceledOrders = new List<string>();
                    
                    
                    return true;
                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing " + configFile + ":" + ex.Message, BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}
