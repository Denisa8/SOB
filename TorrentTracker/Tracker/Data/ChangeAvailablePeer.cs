using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class ChangeAvailablePeer
    {
        public bool Available { get; private set; }

        public ChangeAvailablePeer(bool available)
        {
            Available = available;
        }
    }
}