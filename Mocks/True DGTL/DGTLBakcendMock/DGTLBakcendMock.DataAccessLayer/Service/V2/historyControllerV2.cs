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

    public delegate ClientOrderRecord[] GetAllOrders(DateTime from,DateTime to);

    public delegate ClientTradeRecord[] GetAllTrades(DateTime from, DateTime to);

    public class historyControllerV2 : ApiController
    {

        #region Public Static Attributs

        public static event OnLog OnLog ;

        public static event DGTLBackendMock.DataAccessLayer.Service.V2.GetAllOrders OnGetAllOrders;

        public static event DGTLBackendMock.DataAccessLayer.Service.V2.GetAllTrades OnGetAllTrades;

        #endregion

        #region Private Static Methods

        private static DateTime ConverDateTime(string strDate, bool isFrom)
        {

            if (!string.IsNullOrEmpty(strDate))
            {
                try
                {
                    int year = Convert.ToInt32(strDate.Substring(0, 4));
                    int month = Convert.ToInt32(strDate.Substring(4, 2));
                    int day = Convert.ToInt32(strDate.Substring(6, 2));

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


        public static HttpResponseMessage Get(HttpRequestMessage Request,string requesterid, string userid, string uuid, string recordtype, string condition=null,
                                       string receivedate=null, bool export=true, string fromDate=null, string toDate=null)
        {
            try
            {
                OnLog(string.Format("Received REST request for record type {0} fromDate={1} toDate={2}", recordtype, fromDate, toDate), MessageType.Information);
                DateTime from = ConverDateTime(fromDate, true);
                DateTime to = ConverDateTime(toDate, false);

                if (recordtype == "O")//RecordType O --> Orders
                {
                    ClientOrderRecord[] allOrders = OnGetAllOrders(from,to);
                    HttpResponseMessage resp =  Request.CreateResponse(HttpStatusCode.OK);
                    resp.Content = new StringContent(JsonConvert.SerializeObject(allOrders), Encoding.UTF8, "application/json");
                    return resp;
                }
                else if (recordtype == "T") //RecordType T --> Executions
                {
                    ClientTradeRecord[] allTrades = OnGetAllTrades(from, to);
                    HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK);
                    resp.Content = new StringContent(JsonConvert.SerializeObject(allTrades), Encoding.UTF8, "application/json");
                    return resp;
                }
                else throw new Exception(string.Format("Unknown record type {0}", recordtype));
              
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.OK,
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
