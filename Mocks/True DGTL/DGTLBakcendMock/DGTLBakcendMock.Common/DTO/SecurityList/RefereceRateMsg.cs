﻿using DGTLBackendMock.Common.DTO.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.SecurityList
{
    public class RefereceRateMsg : WebSocketSubscribeMessage
    {
        public DateTime FixingDate { get; set; }

        public string FixingTime { get; set; }

        public decimal Price { get; set; }
    }
}
