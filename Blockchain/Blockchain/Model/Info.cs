﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainServer.Model
{
    public class Info
    {
        public int SensorId { get; set; }
        public string Address { get; set; }
        public bool MotionDetected { get; set; }

    }
}