using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList.V2
{
    public class Instrument : WebSocketMessageV2
    {

        #region Private Static Consts

        private static char _NDF = 'N';
        private static char _DF = 'D';
        private static char _SPOT = 'S';
        private static char _NONE = 'X';

        private static string _STR_SPOT = "Spot";
        private static string _STR_SWP = "SWP";
        private static string _STR_NDF = "NDF";

        #endregion

        #region Protected Attributes

        public int ExchangeId { get; set; }

        public int InstrumentId { get; set; }

        public string InstrumentName { get; set; }

        public double MinLotSize { get; set; }

        public double MaxLotSize { get; set; }

        public decimal LotSize { get; set; }

        public decimal MinQuotePrice { get; set; }

        public decimal MaxQuotePrice { get; set; }

        public decimal MinPriceIncrement { get; set; }

        public decimal MaxNotionalValue { get; set; }

        public string InstrumentDate { get; set; } //format YYYYMMDD 

        public bool Test { get; set; }

        public string Description { get; set; }

        public long UpdatedAt { get; set; }

        public long CreatedAt { get; set; }

        public string LastUpdatedBy { get; set; }

        public string Currency1 { get; set; }

        public string Currency2 { get; set; }

        //missing CurrencyPair,


        private byte productType;
        public byte ProductType
        {
            get { return productType; }
            set
            {
                productType = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cProductType { get { return Convert.ToChar(ProductType); } set { ProductType = Convert.ToByte(value); } }

        #endregion

        #region Public Static Methods

        public static char GetProductType(string productType)
        {
            if (productType == _STR_NDF)
                return _NDF;
            else if (productType == _STR_SPOT)
                return _SPOT;
            else if (productType == _STR_SWP)
                return _NONE;
            else 
                return _NONE;
        
        }

        #endregion
    }
}
