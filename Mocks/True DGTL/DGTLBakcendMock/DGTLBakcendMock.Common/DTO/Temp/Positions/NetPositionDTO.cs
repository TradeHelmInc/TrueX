using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Temp.Positions
{
    public class NetPositionDTO
    {

        #region Private Attributes

        public static string _LONG = "LONG";
        public static string _SHORT = "SHORT";
        public static string _FLAT = "FLAT";

        #endregion

        #region Public Attributes

        public string FirmId { get; set; }

        public string AssetClass { get; set; }

        public string Symbol { get; set; }

        public DateTime MaturityDate { get; set; }

        public double NetContracts { get; set; }

        public string PositionExposure { get; set; }

        public double OpenPrice { get; set; }

        #endregion


        #region Public Methods


        public bool IsCoverOrFlipping(TradeDTO execution)
        {
            if (PositionExposure == NetPositionDTO._LONG && execution.Side == TradeDTO._TRADE_SELL)
                return true;
            if (PositionExposure == NetPositionDTO._SHORT && execution.Side == TradeDTO._TRADE_BUY)
                return true;


            return false;
        
        }

        public void UpdateExposure()
        {
            if (NetContracts > 0)
                PositionExposure = _LONG;

            if (NetContracts < 0)
                PositionExposure = _SHORT;

            if (NetContracts == 0)
                PositionExposure = _FLAT;
        
        }

        #endregion

    }
}
