using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class IpPort
    {
        public Guid ID { get; private set; }
        public string IP { get; private set; }
        public int Port { get; private set; }
        public Dictionary<string, FileTorrent> Files { get; private set; }

        protected IpPort(Guid guid,string iP, int port, Dictionary<string, FileTorrent> files)
        {
            ID = guid;
            IP = iP;
            Port = port;
            Files = files;
        }

        protected IpPort() { }

    }
}