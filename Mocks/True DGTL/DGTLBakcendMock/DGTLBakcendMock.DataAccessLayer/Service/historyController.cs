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

namespace DGTLBackendMock.DataAccessLayer.Service
{

    public delegate void OnLog(string msg, MessageType type);

    public delegate GetOrdersBlotterFulFilled GetAllOrders();

    public delegate GetExecutionsBlotterFulFilled GetAllTrades();

    public delegate HttpResponseMessage OnGet(HttpRequestMessage Request,  string userid, string uuid, string recordtype, string condition = null,
                                       string receivedate = null, string fromDate = null, string toDate = null);


    //public delegate HttpResponseMessage OnGet(HttpRequestMessage Request,string requesterid, string userid, string uuid, string recordtype, string condition = null,
    //                                       string receivedate = null, bool export = true, string fromDate = null, string toDate = null);

    public class historyController : ApiController
    {

        #region Public Static Attributs

        public static event OnLog OnLog;

        public static event GetAllOrders OnGetAllOrders;

        public static event GetAllTrades OnGetAllTrades;

        public static event OnGet OverridenGet;

        #endregion

        #region Public Methods

        [HttpGet]
        public HttpResponseMessage Get( string userid, string uuid, string recordtype, string condition = null,
                               string receivedate = null,  string fromDate = null, string toDate = null)

        //public HttpResponseMessage Get(string requesterid, string userid, string uuid, string recordtype, string condition = null,
        //                               string receivedate = null, bool export = true, string fromDate = null, string toDate = null)
        {
            try
            {

                //RecordType O --> Orders
                if (OverridenGet != null)
                {
                    return OverridenGet(Request, userid, uuid, recordtype, condition, receivedate,fromDate, toDate);

                    //return OverridenGet(Request,requesterid, userid, uuid, recordtype, condition, receivedate, export, fromDate, toDate);
                }
                else if (recordtype == "O")
                {
                    OnLog(string.Format("Received REST request for record type {0} fromDate={1} toDate={2}", recordtype, fromDate, toDate), MessageType.Information);

                    GetOrdersBlotterFulFilled allOrders = OnGetAllOrders();
                    HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK);
                    resp.Content = new StringContent(JsonConvert.SerializeObject(allOrders), Encoding.UTF8, "application/json");
                    return resp;
                }
                else if (recordtype == "T") //RecordType T --> Executions
                {
                    OnLog(string.Format("Received REST request for record type {0} fromDate={1} toDate={2}", recordtype, fromDate, toDate), MessageType.Information);

                    GetExecutionsBlotterFulFilled allTrades = OnGetAllTrades();
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
