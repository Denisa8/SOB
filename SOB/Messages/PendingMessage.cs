using MiscUtil.Conversion;
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
        Guid guid { get; set; }
        public byte[] EncodedMessage { get; set; }
        public int Type { get; set; }

        public void Send()
        {
            Console.WriteLine("PENDING_MESSAGE: " + PieceIndex + "---" + EndianBitConverter.Big.ToInt32(EncodedMessage, 8));



            Stream.Write(EncodedMessage, 0, EncodedMessage.Length);
        }
    }
}