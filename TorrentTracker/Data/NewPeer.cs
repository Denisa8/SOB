using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentTracker.Data
{
    [Serializable]
    public class NewPeer
    {
        public string IP { get; private set; }
        public int Port { get; private set; }

        public NewPeer(string ip,int port)
        {
            IP = ip;
            Port = port;
        }
    }
}
