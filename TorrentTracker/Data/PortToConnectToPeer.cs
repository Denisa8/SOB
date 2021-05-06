﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentTracker.Data
{
    [Serializable]
    public class PortToConnectToPeer
    {
        public int Port { get; private set; }
        public List<int> Pieces { get; private set; }

        public PortToConnectToPeer(int port, List<int> pieces)
        {
            Port = port;
            Pieces = pieces;
        }
    }
}
