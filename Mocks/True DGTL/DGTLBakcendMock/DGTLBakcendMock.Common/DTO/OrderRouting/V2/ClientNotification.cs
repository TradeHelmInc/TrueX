﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.OrderRouting.V2
{
    public class ClientNotification : WebSocketMessageV2
    {
        #region Public Attributes


        public string Uuid { get; set; }

        public string UserId { get; set; }

        public string InstrumentId { get; set; }

        public string ExecId { get; set; }

        public double Price { get; set; }

        public double Size { get; set; }

        public byte Side { get; set; }

        [JsonIgnore]
        public char cSide { get { return Convert.ToChar(Side); } set { Side = Convert.ToByte(value); } }

        public string Timestamp { get; set; }

        #endregion
    }
}
