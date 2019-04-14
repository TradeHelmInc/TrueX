﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public enum OrdRejReason
    {
        Broker = 0,
        UnknownSymbol = 1,
        InvalidInvestorID = 10,
        UnsupportedOrderCharacteristic = 11,
        SurveillanceOption = 12,
        IncorrectQuantity = 13,
        IncorrectAllocatedQuantity = 14,
        UnkwnownAccount = 15,
        ExchangeClosed = 2,
        OrderExceedsLimit = 3,
        TooLateToEnter = 4,
        UnknownOrder = 5,
        DuplicateOrder = 6,
        DuplicateAVerballyCommunicatedOrder = 7,
        StaleOrder = 8,
        TradeAlongRequired = 9,
        Other = 99,
        InvalidPriceIncrement = 18

    }
}
