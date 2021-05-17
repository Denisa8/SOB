using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class InitConnectToTracker : IpPort
    {
        public List<Guid> BannedPeers { get; private set; }
        public InitConnectToTracker(Guid idPeer,string iPPeer, int portPeer, Dictionary<string, FileTorrent> filesPeer,List<Guid> bannedPeersOnPeer) : base(idPeer,iPPeer, portPeer, filesPeer)
        {
            BannedPeers = bannedPeersOnPeer;
        }

        private InitConnectToTracker() { }
    }
}