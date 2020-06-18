using DGTLBackendMock.Common.DTO.SecurityList.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.Util.Security
{
    public class SecurityHandler
    {

        public static ClientInstrument GetInstrumentBySymbol(ClientInstrumentBatch InstrBatch ,string symbol)
        {
            return InstrBatch.messages.Where(x => x.InstrumentName == symbol).FirstOrDefault();
        }

        public static ClientInstrument GetInstrumentByIntInstrumentId(ClientInstrumentBatch InstrBatch, string instrumentId)
        {

            ClientInstrument instr = InstrBatch.messages.Where(x => x.InstrumentId == instrumentId).FirstOrDefault();
            return instr;
        }

        public static ClientInstrument GetInstrumentByServiceKey(ClientInstrumentBatch InstrBatch, string serviceKey)
        {
            if (InstrBatch == null)
                throw new Exception("Initial load for instrument not finished!");

            return GetInstrumentByIntInstrumentId(InstrBatch,serviceKey);
        }
    }
}
