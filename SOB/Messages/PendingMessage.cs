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
        public byte[] Message { get; set; }
        public byte Type { get { return Message[4]; } }

        public PendingMessage(NetworkStream stream, byte[] message)
        {
            this.Stream = stream;
            this.Message = message;
        }

        public void Send()
        {
            Stream.Write(Message, 0, Message.Length);
        }
    }
}
