using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class InformPeerAboutNewReceivePiece
    {
        public Guid ID { get; private set; }
        public string File { get; private set; }
        public int Piece { get; private set; }

        public InformPeerAboutNewReceivePiece(Guid iD, string file, int piece)
        {
            ID = iD;
            File = file;
            Piece = piece;
        }
    }
}