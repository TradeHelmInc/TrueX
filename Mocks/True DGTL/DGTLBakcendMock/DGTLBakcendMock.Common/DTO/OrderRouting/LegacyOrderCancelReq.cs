﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting
{
    public class LegacyOrderCancelReq : WebSocketMessage
    {

        #region Public Attributes

        public string User { get; set; }

        public string ClOrderId { get; set; }

        public string OrigClOrderId { get; set; }

        public string InstrumentId { get; set; }

        public byte Side { get; set; }

        public string JsonWebToken { get; set; }

        #endregion
    }
}