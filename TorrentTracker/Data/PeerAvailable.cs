using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentTracker.Data
{
    [Serializable]
    public class PeerAvailable
    {
        public bool Available { get; private set; }
        public PeerAvailable(bool available)
        {
            Available = available;
        }
    }
}
