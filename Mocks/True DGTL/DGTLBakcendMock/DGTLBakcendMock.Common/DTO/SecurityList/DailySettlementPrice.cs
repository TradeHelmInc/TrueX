﻿using DGTLBackendMock.Common.DTO.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList
{
    public class DailySettlementPrice : SubscriptionMsg
    {
        public DateTime DSPDate { get; set; }

        public string DSPTime { get; set; }

        public decimal Price { get; set; }
    }
}