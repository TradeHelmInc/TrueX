using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Auth
{
    public class Config
    {
        #region Public Attributes

        public bool SendMinOrdQty { get; set; }

        public bool SendMaxOrdQty { get; set; }

        public bool SendLotSize { get; set; }

        public bool SendMinPriceIncrement { get; set; }

        public bool SendMaxNotionalValue { get; set; }

        public double MarginPct { get; set; }

        public double OneWideCalDisc { get; set; }

        public double TwoWideCalDisc { get; set; }

        public double ThreeWideCalDisc { get; set; }

        public bool ImplementCalendarMarginDiscount { get; set; }


        #endregion

    }
}
