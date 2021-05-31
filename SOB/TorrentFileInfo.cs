using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace TorrentClient
{
  public class TorrentFileInfo
  {
    public string TrackerUrl { get; set; }
    public int TorrentsPath { get; set; }
    public static int TorrentHash { get; set; }
    public static byte[][] PieceHashes { get; set; }
    public int PiecesCount { get; set; }
    public int PiecesLength { get; set; }
    public string PathSource { get; set; }
    public string PathNew { get; set; }

    static object lockObject = new object();

    public bool CheckReceivedPiece(byte[] bytes, int index, int receivedBytes)
    {
      if (receivedBytes > PiecesLength)
      {
        Console.WriteLine("Otrzymano za dużo danych.");
        return false;
      }
      else if (receivedBytes < PiecesLength)
      {
        byte[] truncArray = new byte[receivedBytes];
        Array.Copy(bytes, truncArray, truncArray.Length);
        var rs = CheckPieceHash(truncArray, index);
        return rs;
      }
      var r = CheckPieceHash(bytes, index);
      return r;
    }
    
    public static bool CheckPieceHash(byte[] bytes, int index)
    {
      var hash = Hash(bytes);
      var result = hash.SequenceEqual(PieceHashes[index]);
      if (!result)
      {
        Console.WriteLine("Nie otrzymano prawidłowego fragmentu.");
        return false;
      }
      Console.WriteLine("Otrzymano prawidłowy fragment.");
      return true;
    }
    public Piece ReadFilePiece(int index)
    {
      try
      {
        Piece p = new Piece();
        p.index = index;
        byte[] bytes = new byte[PiecesLength];
        int numBytesToRead = PiecesLength;
        long numBytesRead = index * PiecesLength;
        using (BinaryReader reader = new BinaryReader(new FileStream(PathSource, FileMode.Open)))
        {
          reader.BaseStream.Seek(numBytesRead, SeekOrigin.Begin);
          var receivedBytes = reader.Read(bytes, 0, numBytesToRead);
          p.length = receivedBytes;
          Console.WriteLine(receivedBytes);
          p.data = bytes;
          CheckReceivedPiece(bytes, index, receivedBytes);
        }
        return p;
      }
      catch (FileNotFoundException ioEx)
      {
        Console.WriteLine(ioEx.Message);
        return null;
      }
    }
    public void SendFilePiece(int index)
    {
      try
      {
        using (FileStream fsSource = new FileStream(PathSource,
            FileMode.Open, FileAccess.Read))
        {
          byte[] bytes = new byte[PiecesLength];
          int numBytesToRead = PiecesLength;
          int numBytesRead = index * PiecesLength;
          while (numBytesToRead > 0)
          {
            int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);
            if (n == 0)
              break;
            var hash = Hash(bytes);
            var result = hash.SequenceEqual(PieceHashes[0]);
            if (!result)
            {
              Console.WriteLine("Nie otrzymano prawidłowego fragmentu.");
              return;
            }
            else
            {
              Console.WriteLine("Otrzymano prawidłowy fragment.");
            }
            numBytesRead += n;
            numBytesToRead -= n;
          }
          numBytesToRead = bytes.Length;

          //using (FileStream fsNew = new FileStream(pathNew,
          //    FileMode.Create, FileAccess.Write))
          //{
          //    fsNew.Write(bytes, 0, numBytesToRead);
          //}
        }
      }
      catch (FileNotFoundException ioEx)
      {
        Console.WriteLine(ioEx.Message);
      }
    }
    public static byte[] Hash(byte[] temp)
    {
      using (SHA1Managed sha1 = new SHA1Managed())
      {
        return sha1.ComputeHash(temp);
      }
    }

    public void WriteFilePiece(int index, byte[] bytes)
    {
      try
      {
        CheckReceivedPiece(bytes, index, bytes.Length);
        long numBytesRead = index * PiecesLength;
        lock (lockObject)
        {
          using (Stream stream = new FileStream(PathNew, FileMode.OpenOrCreate, FileAccess.ReadWrite, System.IO.FileShare.ReadWrite |
              System.IO.FileShare.Read | System.IO.FileShare.Write | System.IO.FileShare.Delete))
          {
            stream.Seek(numBytesRead, SeekOrigin.Begin);
            stream.Write(bytes, 0, bytes.Length);
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
    }
  }
}
