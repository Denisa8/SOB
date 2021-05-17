using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class FileTorrent
    {
        public string ID { get; private set; }
        public List<int> Pieces { get; private set; }
        public int countPieces { get; private set; }

        public FileTorrent(string id, List<int> pieces, int countPieces)
        {
            ID = id;
            Pieces = pieces;
            this.countPieces = countPieces;
        }
    }
}