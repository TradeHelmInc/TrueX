﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2.Credit_UI
{
    public class FirmsTradingStatusUpdateResponse : WebSocketMessageV2
    {
        #region Pubic Attributes

        public string JsonWebToken { get; set; }

        public string UUID { get; set; }

        public long FirmId { get; set; }

        private byte tradingStatus;
        public byte TradingStatus
        {
            get { return tradingStatus; }
            set
            {
                tradingStatus = Convert.ToByte(value);

            }
        }

        [JsonIgnore]
        public char cTradingStatus { get { return Convert.ToChar(TradingStatus); } set { TradingStatus = Convert.ToByte(value); } }

        public bool Success { get; set; }

        public string Message { get; set; }

        public long Time { get; set; }

        #endregion
    }
}
