using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentTracker.Data
{
    [Serializable]
    public class PeerListElement
    {
        public string IP { get; private set; }
        public int Port { get; private set; }
        public List<int> Pieces { get; private set; }
        public Guid ID { get; private set; }

        public PeerListElement(string ip, int port, List<int> pieces,Guid guid)
        {
            IP = ip;
            Port = port;
            Pieces = pieces;
            ID = guid;
        }
    }
}
