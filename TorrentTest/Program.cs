using MonoTorrent;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TorrentClient;

namespace TorrentTest
{
    class Program
    {
        static TorrentFileInfo torrentFileInfo = new TorrentFileInfo();
        private static TcpListener listener { get; set; }
        public static ConcurrentDictionary<int, Peer> Peers { get; } = new ConcurrentDictionary<int, Peer>();
        private static int port { get; set; }

        private static void EnablePeerConnections(int Port)
        {
            port = Port;

            listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(HandleNewConnection), null);
        }

        private static void HandleNewConnection(IAsyncResult ar)
        {
            if (listener == null)
                return;

            TcpClient client = listener.EndAcceptTcpClient(ar);
            listener.BeginAcceptTcpClient(new AsyncCallback(HandleNewConnection), null);
            Console.WriteLine("DODANO " + port);
            AddPeer(new Peer(client,torrentFileInfo));
        }
        private static void AddPeer(Peer peer)
        {
            Random rand = new Random();
            peer.ConnectToPeer(1300);

            if (!Peers.TryAdd(rand.Next(), peer))
                peer.Disconnect();
        }
        static async Task Main(string[] args)
        {
            var filePath = Path.Combine(Environment.CurrentDirectory, "Downloads");
            var torrentsPath = "TO.torrent";
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
                    torrentFileInfo.PathSource = @"C:\Users\Admin\Desktop\TO2.mp4";
                    torrentFileInfo.PathNew = @"c:\Users\Admin\Desktop\TO6.mp4";

                    EnablePeerConnections(1301);
                    for (int i = 0; i < torrent.Pieces.Count; i++)
                    {
                        var byteResult = torrent.Pieces.ReadHash(i);
                        torrentFileInfo.PieceHashes[i] = byteResult;
                        //var bytes = torrentFileInfo.ReadFilePiece(i);
                        //Piece p = new Piece();
                        //p.index = i;
                        //p.data = bytes;
                        //p1.SendP`iece(p);
                        //torrentFileInfo.WriteFilePiece(i,bytes); 
                    }
                    Peer p1 = new Peer(torrentFileInfo);
                    p1.ConnectToPeer(1300);
                    while (!p1.IsConnected&& !Peers.Any())
                    { }
                    
                    while (true)
                    {

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
