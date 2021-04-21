using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace TorrentClient
{
    public class TorrentFileInfo
    {
        public int TorrentsPath { get; set; }
        public int TorrentHash { get; set; }
        public byte[][] PieceHashes { get; set; }
        public int PiecesCount { get; set; }
        public int PiecesLength { get; set; }
        public string PathSource { get; set; }
        public string PathNew { get; set; }


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
                var rs = CheckHash(truncArray, index);
                return rs;
            }
            var r = CheckHash(bytes, index);
            return r;
        }
        public bool CheckHash(byte[] bytes, int index)
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
        public void ReadFilePiece(int index)
        {
            try
            {
                //using (FileStream fsSource = new FileStream(PathSource,
                //    FileMode.Open, FileAccess.Read))
                //{
                byte[] bytes = new byte[PiecesLength];
                int numBytesToRead = PiecesLength;
                long numBytesRead = index * PiecesLength;
                //if (numBytesRead != 0)
                //    numBytesRead += 1;
                using (BinaryReader reader = new BinaryReader(new FileStream(PathSource, FileMode.Open)))
                {
                    reader.BaseStream.Seek(numBytesRead, SeekOrigin.Begin);
                    var receivedBytes = reader.Read(bytes, 0, numBytesToRead);
                    Console.WriteLine(receivedBytes);
                    CheckReceivedPiece(bytes,index,receivedBytes);
                }
                //while (numBytesToRead > 0)
                //{
                //    int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);
                //    if (n == 0)
                //        break;
                //    CheckReceivedPiece(bytes);
                //    numBytesRead += n;
                //    numBytesToRead -= n;
                //}
                //numBytesToRead = bytes.Length;

                //using (FileStream fsNew = new FileStream(pathNew,
                //    FileMode.Create, FileAccess.Write))
                //{
                //    fsNew.Write(bytes, 0, numBytesToRead);
                //}
                //}
            }
            catch (FileNotFoundException ioEx)
            {
                Console.WriteLine(ioEx.Message);
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
    }
}
