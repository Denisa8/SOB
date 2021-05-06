using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentTracker.Data
{
    [Serializable]
    public class PieceReceive
    {
        public int Index { get; private set; }

        public PieceReceive(int index)
        {
            Index = index;
        }
    }
}
