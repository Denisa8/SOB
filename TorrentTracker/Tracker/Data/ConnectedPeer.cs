using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class ConnectedPeer : IpPort
    {
        public ConnectedPeer(Guid guid,string iP, int port, Dictionary<string, FileTorrent> files) : base(guid,iP, port, files)
        {
        }
        public ConnectedPeer(Peer peer) : base(peer.ID,peer.IP, peer.Port, peer.Files) { }

        protected ConnectedPeer()
        {
        }
    }
}