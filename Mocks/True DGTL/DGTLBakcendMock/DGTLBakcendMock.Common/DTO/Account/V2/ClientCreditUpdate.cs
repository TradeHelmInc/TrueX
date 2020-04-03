using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientCreditUpdate : WebSocketMessageV2
    {

        #region Public Static Consts

        public static char _SEC_STATUS_TRADING = 'T';
        public static char _SEC_STATUS_SUSPENDE= 'S';

        public static char _UPDATE_REASON_DEFAULT = '0';
        public static char _UPDATE_REASON_CREDIT_LIMIT ='1';
        public static char _UPDATE_REASON_TRADE= '2';
        public static char _UPDATE_REASON_USAGE_UDPATE= '3';
        public static char _UPDATE_REASON_STATUS_CHANGE = '4';

        #endregion


        #region Public Attributes

        public string Uuid { get; set; }

        public long FirmId { get; set; }

        public long AccountId { get; set; }

        public double MaxNotional { get; set; }

        public double CreditLimit { get; set; }

        public double CreditUsed { get; set; }

        public double BuyExposure { get; set; }

        public double SellExposure { get; set; }

        private byte status;
        public byte Status
        {
            get { return status; }
            set
            {
                status = Convert.ToByte(value);

            }
        }//T: Trading, S: Suspended

        [JsonIgnore]
        public char cStatus { get { return Convert.ToChar(Status); } set { Status = Convert.ToByte(value); } }


        private byte updateReason;
        public byte UpdateReason
        {
            get { return updateReason; }
            set
            {
                updateReason = Convert.ToByte(value);

            }
        }//0: Default, 1-CreditLimit,2-> Trade, 3-> Usage Update,4-> Status Change

        [JsonIgnore]
        public char cUpdateReason { get { return Convert.ToChar(UpdateReason); } set { UpdateReason = Convert.ToByte(value); } }

        public long Timestamp { get; set; }

        #endregion
    }
}
