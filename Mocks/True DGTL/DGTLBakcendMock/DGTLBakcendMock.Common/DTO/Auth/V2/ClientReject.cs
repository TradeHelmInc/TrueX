using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth.V2
{
    public class ClientReject : WebSocketMessageV2
    {

        #region Public Static Contss

        public static int _GENERIC_REJECT_CODE = 83;

        #endregion

        #region Public Attributes

        public int Sender { get; set; }

        public int Time { get; set; }

        public int RejectCode { get; set; }

        public string Text { get; set; }

        public string Uuid { get; set; }

        #endregion
    }
}
