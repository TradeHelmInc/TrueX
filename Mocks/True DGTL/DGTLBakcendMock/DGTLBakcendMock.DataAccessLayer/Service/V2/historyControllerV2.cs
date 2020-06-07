using DGTLBackendMock.Common.DTO.OrderRouting.Blotters;
using DGTLBackendMock.Common.DTO.OrderRouting.V2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DGTLBackendMock.DataAccessLayer.Service.V2
{
    public delegate void OnLog(string msg, MessageType type);

    public delegate ClientOrderRecord[] GetAllOrders(DateTime from, DateTime to, int pageNo = 1, int recordPage = 1, 
                                                     string sort = null, string filter = null);

    public delegate ClientTradeRecord[] GetAllTrades(DateTime from, DateTime to, int pageNo = 1, int recordPage = 1,
                                                     string sort = null, string filter = null);

    public class historyControllerV2 : ApiController
    {

        #region Public Static Attributs

        public static event OnLog OnLog ;

        public static event DGTLBackendMock.DataAccessLayer.Service.V2.GetAllOrders OnGetAllOrders;

        public static event DGTLBackendMock.DataAccessLayer.Service.V2.GetAllTrades OnGetAllTrades;

        #endregion

        #region Private Static Methods

        private static void ValidateDateTimes(string strFrom, string strTo)
        {
            if (strFrom == "2019-12-31" && strTo == "2019-12-31")
                throw new Exception("Invalid request. Could not process dates for 2019-12-31");
        }

        private static DateTime ConverDateTime(string strDate, bool isFrom)
        {

            if (!string.IsNullOrEmpty(strDate))
            {
                try
                {
                    //format yyyy-MM-dd
                    int year = Convert.ToInt32(strDate.Substring(0, 4));
                    int month = Convert.ToInt32(strDate.Substring(5, 2));
                    int day = Convert.ToInt32(strDate.Substring(8, 2));

                    return isFrom ? new DateTime(year, month, day, 0, 0, 0) : new DateTime(year, month, day, 23, 59, 59);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Invalid date format: {0}", strDate));
                }

            }
            else
                return isFrom ? DateTime.MinValue : DateTime.MaxValue;
        }


        #endregion

        #region Public Methods

        [HttpGet]
        public static HttpResponseMessage Get(HttpRequestMessage Request, string userid, string uuid, string recordtype, string condition = null,
                                              string receivedate = null, string fromDate = null, string toDate = null,
                                              int pageNo=1,int recordPage=1,string sort=null,string filter=null)

        //public static HttpResponseMessage Get(HttpRequestMessage Request,string requesterid, string userid, string uuid, string recordtype, string condition=null,
        //                               string receivedate=null, bool export=true, string fromDate=null, string toDate=null)
        {
            try
            {
                OnLog(string.Format("Received REST request for record type {0} fromDate={1} toDate={2} pageNo={3} recordPage={4} sort={5} filter={6}",
                                      recordtype, fromDate, toDate, pageNo, recordPage, sort, filter), MessageType.Information);
                DateTime from = ConverDateTime(fromDate, true);
                DateTime to = ConverDateTime(toDate, false);

                if (recordtype == "O")//RecordType O --> Orders
                {
                    ValidateDateTimes(fromDate, toDate);
                    ClientOrderRecord[] allOrders = OnGetAllOrders(from, to, pageNo, recordPage, sort, filter);
                    allOrders.ToList().ForEach(x => x.Uuid = uuid);
                    allOrders.ToList().ForEach(x => x.UserId = userid);
                    
                    //GetOrdersBlotterResponse ordersResp = new GetOrdersBlotterResponse() { Success = true, Uuid = uuid, Msg = "GetOrdersBlotterResponse", data = allOrders };
                    //HttpResponseMessage resp =  Request.CreateResponse(HttpStatusCode.OK);
                    //resp.Content = new StringContent(JsonConvert.SerializeObject(ordersResp), Encoding.UTF8, "application/json");


                    HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK);
                    resp.Content = new StringContent(JsonConvert.SerializeObject(allOrders), Encoding.UTF8, "application/json");

                    return resp;
                }
                else if (recordtype == "T") //RecordType T --> Executions
                {
                    ValidateDateTimes(fromDate, toDate);
                    ClientTradeRecord[] allTrades = OnGetAllTrades(from, to, pageNo, recordPage, sort, filter);
                    
                    //GetExecutionsBlotterResponse execResp = new GetExecutionsBlotterResponse() { Success = true, Uuid = uuid, Msg = "GetExecutionsBlotterResponse", data = allTrades };
                    //HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK);
                    //resp.Content = new StringContent(JsonConvert.SerializeObject(allTrades), Encoding.UTF8, "application/json");

                    HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK);
                    resp.Content = new StringContent(JsonConvert.SerializeObject(allTrades), Encoding.UTF8, "application/json");
                    
                    return resp;
                }
                else throw new Exception(string.Format("Unknown record type {0}", recordtype));
              
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                                                        new
                                                        {
                                                            IsOK = false,
                                                            Error = ex.Message,
                                                        });
            }


        }

        #endregion
    }
}
