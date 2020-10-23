using DGTLBackendMock.Common.DTO.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList
{


    public class SecurityMasterRecord : WebSocketMessage
    {
        #region Public Static Consts

        public static string _AS_SPOT = "Spot";

        #endregion
        #region Public Attributes

        public int InstrumentId { get; set; }

        public string Symbol { get; set; }

        public string Description { get; set; }

        public string SecurityType { get; set; }

        public string ProductType { get; set; }

        public string AssetClass { get; set; }

        public string CurrencyPair { get; set; }

        public decimal MinPrice { get; set; }

        public decimal MaxPrice { get; set; }

        public decimal MinPriceIncrement { get; set; }

        public decimal MinSize { get; set; }

        public decimal MaxSize { get; set; }

        public decimal LotSize { get; set; }

        public string Platform { get; set; }

        public string MaturityDate { get; set; }

        public string Status { get; set; }

        public decimal? MaxNotional { get; set; }

        #endregion

        #region Public Methods

        public DateTime GetMaturityDate()
        {
            if (!string.IsNullOrEmpty(MaturityDate))
                return DateTime.ParseExact(MaturityDate, "yyyyMMdd", null);
            else
                return DateTime.MinValue;

        }

        public string GetMaturityDateFromSymbol()
        {

            string monthCode = Symbol.Substring(0, 1);
            string year = Symbol.Substring(1, 2);
            string month = "";

            if (monthCode == "F")
                month= "01";
            else if (monthCode == "G")
                month = "02";
            else if (monthCode == "H")
                month = "03";
            else if (monthCode == "J")
                month = "04";
            else if (monthCode == "K")
                month = "05";
            else if (monthCode == "M")
                month = "06";
            else if (monthCode == "N")
                month = "07";
            else if (monthCode == "Q")
                month = "08";
            else if (monthCode == "U")
                month = "09";
            else if (monthCode == "V")
                month = "10";
            else if (monthCode == "X")
                month = "11";
            else if (monthCode == "Z")
                month = "12";


            return "20" + year + month + "15";
        
        }

        #endregion
    }
}
