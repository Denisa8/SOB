using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentClient
{
    [Serializable]
    public class Piece
    {
        public int index { get; set; }
        public int length { get; set; }
        public byte[] data { get; set; }
    }
}
