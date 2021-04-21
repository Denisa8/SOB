using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Client.PiecePicking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace TorrentClient
{
    class Program
    {  
        static TorrentFileInfo torrentFileInfo = new TorrentFileInfo();
        static async Task Main(string[] args)
        {
            var filePath = Path.Combine(Environment.CurrentDirectory, "Downloads");
            var torrentsPath = "uTorrent.exe.torrent";
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath); 
            if (torrentsPath.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var torrent = await Torrent.LoadAsync(torrentsPath);
                    torrentFileInfo.TorrentHash = torrent.InfoHash.GetHashCode();
                    torrentFileInfo.PiecesLength = torrent.PieceLength;
                    torrentFileInfo.PiecesCount = torrent.Pieces.Count;
                    torrentFileInfo.PieceHashes = new byte[torrentFileInfo.PiecesCount][];
                    torrentFileInfo.PathSource = @"C:\Users\Admin\Desktop\uTorrent.exe";
                    torrentFileInfo.PathNew = @"c:\Users\Admin\Desktop\uTorrent2.txt";
                    for (int i=0;i< torrent.Pieces.Count; i++)
                    {
                        var byteResult = torrent.Pieces.ReadHash(i);
                        torrentFileInfo.PieceHashes[i] = byteResult;
                        torrentFileInfo.ReadFilePiece(i);
                    }  
                    Console.ReadLine(); 
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        
    }
}
