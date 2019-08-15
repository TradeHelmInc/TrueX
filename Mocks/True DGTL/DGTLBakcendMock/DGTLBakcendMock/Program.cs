using DGTLBackendMock.DataAccessLayer;
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
            string RESTAdddress = ConfigurationManager.AppSettings["RESTAdddress"];
            string mode = ConfigurationManager.AppSettings["Mode"];
            string marketDataConfigFile = ConfigurationManager.AppSettings["MarketDataConfigFile"];
            string marketDataModule = ConfigurationManager.AppSettings["MarketDataModule"];

            string orderRoutingConfigFile = ConfigurationManager.AppSettings["OrderRoutingConfigFile"];
            string orderRoutingModule = ConfigurationManager.AppSettings["OrderRoutingModule"];

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12
                                                  | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            DoLog(string.Format("Instantiating backend service at {0}", WebSocketAdddress));

            try
            {
                if (mode.ToUpper() == "JSON")
                {
                    DGTLWebSocketServer server = new DGTLWebSocketServer(WebSocketAdddress, RESTAdddress);
                    server.Start();
                    DoLog(" Service Successfully Started...");
                }
                else if (mode.ToUpper() == "JSONV2")
                {
                    DGTLWebSocketV2Server server = new DGTLWebSocketV2Server(WebSocketAdddress, RESTAdddress);
                    server.Start();
                    DoLog(" Service Successfully Started...");
                }
                else if (mode.ToUpper() == "SIMULATED")
                {
                    DGTLWebSocketSimulatedServer server = new DGTLWebSocketSimulatedServer(WebSocketAdddress, 
                                                                                           marketDataModule, 
                                                                                           marketDataConfigFile,
                                                                                           orderRoutingModule,
                                                                                           orderRoutingConfigFile);
                    server.Start();
                    DoLog(" Service Successfully Started...");
                }
                else
                    throw new Exception(string.Format("Unknown mock mode {0}",mode));

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error initializing  Service: {0}", ex.Message));
            }

            Console.ReadKey();



        }
    }
}
