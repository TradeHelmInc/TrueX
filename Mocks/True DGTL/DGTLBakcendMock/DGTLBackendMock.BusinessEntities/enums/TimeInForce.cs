using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.BusinessEntities.enums
{
    public enum TimeInForce
    {
        Day = '0',
        GoodTillCancel = '1',
        AtTheOpening = '2',
        ImmediateOrCancel = '3',
        FillOrFill = '4',
        GoodTillCrossing = '5',
        GoodTillDate = '6',
        AtTheClose = '7'
    }
}
