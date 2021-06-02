using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TorrentTracker.Tracker.Data;

namespace TorrentTracker.Tracker
{
    [Serializable]
    public class PeerReceiveFile
    {
        public Guid ID { get; private set; }
        public FileTorrent File { get; private set; }

        public PeerReceiveFile(Guid iD, FileTorrent file)
        {
            ID = iD;
            File = file;
        }
    }
}