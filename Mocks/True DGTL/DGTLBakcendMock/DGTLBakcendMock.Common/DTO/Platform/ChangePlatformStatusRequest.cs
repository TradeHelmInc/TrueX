using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Platform
{
    public class ChangePlatformStatusRequest : WebSocketMessage
    {
        #region Public Static Consts

        public static int _OPEN = 2;

        public static int _MARKET_CLOSED = 4;

        public static int _SYSTEM_CLOSED = 6;

        #endregion


        #region Public Attributes

        public int Status { get; set; }

        #endregion

        #region Public Methods

        public void AssingStatus(string strStatus)
        {

            try
            {
                Status = Convert.ToInt32(strStatus);


                if (Status != _OPEN && Status != _MARKET_CLOSED && Status != _SYSTEM_CLOSED)
                    throw new Exception(string.Format("Invalid status {0}", Status));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Invalid status format {0}", strStatus));
            
            }
        
        }

        #endregion
    }
}
