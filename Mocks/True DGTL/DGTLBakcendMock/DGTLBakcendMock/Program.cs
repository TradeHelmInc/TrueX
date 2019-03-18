using Fleck;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace DGTLBakcendMock
{
    class Program
    {
        #region Private Static Methods

        //This should be logged, but we will write it on the screen for simplicity
        private static void DoLog(string message)
        {
            Console.WriteLine(message);
        }

        #endregion
       

        static void Main(string[] args)
        {

            string WebSocketAdddress = ConfigurationManager.AppSettings["WebSocketAdddress"];

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12
                                                  | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            DoLog(string.Format("Instantiating backend service at {0}", WebSocketAdddress));

            try
            {
                DGTLBakcendMock.DataAccessLayer.DGTLWebSocketServer server = new DGTLBakcendMock.DataAccessLayer.DGTLWebSocketServer(WebSocketAdddress);

                server.Start();
              
                DoLog(" Service Successfully Started...");

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error initializing  Service: {0}", ex.Message));
            }

            Console.ReadKey();



        }
    }
}
