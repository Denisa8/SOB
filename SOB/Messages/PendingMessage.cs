﻿using MiscUtil.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TorrentClient.Messages
{
    public class PendingMessage
    {
        public NetworkStream Stream { get; set; }
        public int PieceIndex { get; set; }
        public Guid Guid { get; set; }
        public byte[] EncodedMessage { get; set; }
        public int Type { get; set; }
        public int IndexPeer { get; set; }
        public void Send()
        {
            Stream.Write(EncodedMessage, 0, EncodedMessage.Length);
        }
    }
}