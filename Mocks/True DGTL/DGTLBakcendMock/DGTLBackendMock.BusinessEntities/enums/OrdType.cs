﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.BusinessEntities.enums
{
    public enum OrdType
    {
        Market = '1',
        Limit = '2',
        Stop = '3',
        StopLimit = '4',
        MarketOnClose = '5',
        LimitOnClose = 'B'
    }
}
