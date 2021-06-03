using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
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