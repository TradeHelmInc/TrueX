using DGTLBackendMock.Common.DTO.OrderRouting.Blotters;
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

    public class historyController : ApiController
    {

        #region Public Static Attributs

        public static event OnLog OnLog ;

        public static event GetAllOrders OnGetAllOrders ;

        public static event GetAllTrades OnGetAllTrades;

        #endregion

        #region Public Methods

        [HttpGet]
        public HttpResponseMessage Get(string requesterid, string userid, string uuid, string recordtype, string condition,
                                       string recievedate, bool export)
        {
            try
            {
                //RecordType O --> Orders
                if (recordtype == "O")
                {
                    GetOrdersBlotterFulFilled allOrders = OnGetAllOrders();
                    HttpResponseMessage resp =  Request.CreateResponse(HttpStatusCode.OK);
                    resp.Content = new StringContent(JsonConvert.SerializeObject(allOrders), Encoding.UTF8, "application/json");
                    return resp;
                }
                else if (recordtype == "T") //RecordType T --> Executions
                {
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
