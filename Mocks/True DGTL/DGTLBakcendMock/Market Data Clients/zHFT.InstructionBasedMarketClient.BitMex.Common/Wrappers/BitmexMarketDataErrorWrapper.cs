using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.Wrappers
{
    public class BitmexMarketDataErrorWrapper : Wrapper
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public string Error { get; set; }

        #endregion

        #region Constructors

        public BitmexMarketDataErrorWrapper(string pSymbol ,string pError)
        {
            Symbol = pSymbol;
            Error = pError;
        }


        #endregion

        #region Wrapper Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            MarketDataFields mdField = (MarketDataFields)field;

           
            if (mdField == MarketDataFields.Error)
                return Error;
            else if (mdField == MarketDataFields.Symbol)
                return Symbol;
            return MarketDataFields.NULL;
        }


        public override Actions GetAction()
        {
            return Actions.MARKET_DATA_ERROR;
        }

        #endregion
    }
}
