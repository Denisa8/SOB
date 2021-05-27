using System;

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
