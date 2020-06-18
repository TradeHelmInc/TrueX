using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class AccountBalance : WebSocketMessageV2
    {
        #region Public Static Consts

        public static char _ACTIVE_STATUS = 'A';

        public static char _INACTIVE_STATUS = 'I';

        #endregion


        #region Public Attributes

        public string Uuid { get; set; }

        public string FirmId { get; set; }

        public string UserId { get; set; }

        public string AccountId { get; set; }

        public double Collateral { get; set; }

        public double PendingCollateral { get; set; }

        public double TodaysMargin { get; set; }

        public double PriorMargin { get; set; }

        public double IMRequirement { get; set; }

        public double VMRequirement { get; set; }

        public bool MarginCall { get; set; }

        public long LastUpdateTime { get; set; }

        private byte status;
        public byte Status
        {
            get { return status; }
            set
            {
                status = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cStatus { get { return Convert.ToChar(Status); } set { Status = Convert.ToByte(value); } }

        #endregion 
    }
}
