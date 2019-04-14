﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public enum ExecType
    {
        New = '0',
        DoneForDay = '3',
        Canceled = '4',
        Replaced = '5',
        PendingCancel = '6',
        Stopped = '7',
        Rejected = '8',
        Suspended = '9',
        PendingNew = 'A',
        Calculated = 'B',
        Expired = 'C',
        Restated = 'D',
        PendingReplace = 'E',
        Trade = 'F',
        TradeCorrect = 'G',
        TradeCancel = 'H',
        OrderStatus = 'I',
        Unknown = 'W'

    }
}
