using DGTLBackendMock.Common.DTO.SecurityList.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.Util.Security
{
    public class SecurityStateHandler
    {
        public static  char GetStateOnSymbol(ClientInstrument instr)
        {
            if (instr.InstrumentName == "ADA-USD")
                return ClientInstrumentState._STATE_CLOSE;
            else if (instr.InstrumentName == "XMR-USD")
                return ClientInstrumentState._STATE_INACTIVE;
            else if (instr.InstrumentName == "XRP-USD")
                return ClientInstrumentState._STATE_UNKNOWN;
            else
                return ClientInstrumentState._STATE_HALT;
        }
    }
}
