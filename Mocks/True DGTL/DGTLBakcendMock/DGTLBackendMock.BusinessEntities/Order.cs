using DGTLBackendMock.BusinessEntities.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.BusinessEntities
{
    public class Order
    {
        #region Public Attributes

        public long Id { get; set; }

        public string OrigClOrdId { get; set; }

        public string ClOrdId { get; set; }

        public string OrderId { get; set; }

        public Security Security { get; set; }

        public Side Side { get; set; }

        public string Exchange { get; set; }

        public OrdType OrdType { get; set; }

        public double OrderQty { get; set; }

        public double? Price { get; set; }

        public string Currency { get; set; }

        public TimeInForce? TimeInForce { get; set; }

        public DateTime? ExpireTime { get; set; }//Date and Time

        public DateTime? EffectiveTime { get; set; }

        public string Account { get; set; }

        public string Symbol
        {
            get
            {
                if (Security != null)
                    return Security.Symbol;
                else
                    return null;
            }
            set
            {
                if (Security == null)
                    Security = new Security();

                Security.Symbol = value;
            }

        }
        #endregion
    }
}
