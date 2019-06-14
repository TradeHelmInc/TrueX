﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Platform
{
    public class PlatformStatus : WebSocketMessage
    {

        #region Public Static Consts

        public static char _STATE_PREOPEN = '1';
        public static char _STATE_OPEN = '2';
        public static char _STATE_PRECLOSE = '3';
        public static char _STATE_CLOSED = '4';
        public static char _STATE_SUSPENDED = '5';


        #endregion

        #region Public Attributes

        public long StatusTime { get; set; }

        //private byte state;
        public byte State
        {
            get { return Convert.ToByte(cState); }
        }//Valid Values: 1=PreOpen,2=>Open,3=PreClose,4=Closed,5=Suspended

        [JsonIgnore]
        public char cState
        {
            get { return Convert.ToChar(sState); }
        }

        public string sState { get; set; }

        #endregion
    }
}