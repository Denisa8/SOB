using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class CheckAvailablePeer
    {
        public bool Available { get; set; }
    }
}