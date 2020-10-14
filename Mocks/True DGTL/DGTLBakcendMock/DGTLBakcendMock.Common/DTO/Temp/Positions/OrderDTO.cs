using DGTLBackendMock.Common.DTO.OrderRouting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Temp.Positions
{
    public class OrderDTO
    {
        #region Public static Consts

        public static char _STATUS_OPEN = 'O';

        public static char _STATUS_REJECTED = 'R';

        public static char _STATUS_CANCELED = 'C';

        public static char _STATUS_FILLED = 'F';

        //public static char _STATUS_PARTIALLY_FILLED = 'P';

        public static char _STATUS_EXPIRED = 'E';

        public static char _SIDE_BUY = 'B';

        public static char _SIDE_SELL = 'S';

        public static char _TIMEINFORCE_DAY = '0';

        public static char _ORDER_TYPE_LIMIT = '1';

        #endregion

        #region Protected Attributes

        public char cSide { get; set; }

        public double LvsQty { get; set; }

        public char cStatus { get; set; }

        public string UserId { get; set; }

        public string InstrumentId { get; set; }

        #endregion

        #region Public Static Methods


        public static List<OrderDTO> GetOrders(List<LegacyOrderRecord> legacyOrders)
        {

            List<OrderDTO> orders = new List<OrderDTO>();

            foreach (LegacyOrderRecord legacyOrder in legacyOrders)
            {

                OrderDTO order = new OrderDTO()
                {
                    LvsQty = legacyOrder.LvsQty,
                    cSide = legacyOrder.cSide,
                    cStatus = legacyOrder.cStatus,
                    UserId = legacyOrder.UserId,
                    InstrumentId = legacyOrder.InstrumentId

                };

                orders.Add(order);
            }

            return orders;
        
        }

        #endregion


    }
}
