using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace DGTLBackendMock.Common.Wrappers
{
    public class LegacyOrderMassCancelReqWrapper:Wrapper
    {
        public override object GetField(zHFT.Main.Common.Enums.Fields field)
        {
            throw new NotImplementedException();
        }

        public override zHFT.Main.Common.Enums.Actions GetAction()
        {
            return Actions.CANCEL_ALL_ORDERS;
        }
    }
}
