using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class BanPeer
    {
        public Guid BanID { get; set; }
    }
}