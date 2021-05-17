using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class ReceivePieceFile
    {
        public string Filename { get; private set; }
        public int Index { get; private set; }

        public ReceivePieceFile(string filename, int index)
        {
            Filename = filename;
            Index = index;
        }
    }
}