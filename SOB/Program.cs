using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Client.PiecePicking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TorrentTracker.Data;

namespace TorrentClient
{
    class Program
    {  
        static TorrentFileInfo torrentFileInfo = new TorrentFileInfo();
        private static TcpListener listener { get; set; }
        public static ConcurrentDictionary<int, Peer> Peers { get; } = new ConcurrentDictionary<int, Peer>();
        private static int port { get; set; }
        private static Guid ID = Guid.NewGuid();
        public static TcpClient clientTracker { get; private set; }
        public static bool Available { get; set; } = true;
        static Thread threadListen;

        private static void EnablePeerConnections(int Port)
        {
            port = Port;
            listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(HandlePeerConnection), null);
        }

        private static void HandlePeerConnection(IAsyncResult ar)
        {
            if (listener == null)
                return;

            TcpClient client = listener.EndAcceptTcpClient(ar);
            listener.BeginAcceptTcpClient(new AsyncCallback(HandlePeerConnection), null);
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

        public static string torrentsPath = @"C:\Users\Admin\Desktop\wyklady2.torrent";
        public static string PathSource = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wyklady.zip";
        public static string PathNew = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wykladyKopia2.zip";

        //public static string torrentsPath = "wyklady2.torrent";
        //public static string PathSource = @"C:\Users\Admin\Desktop\wyklady.zip";
        //public static string PathNew = @"c:\Users\Admin\Desktop\wyklady3.zip";

        static void ReceivePeerList(List<PeerListElement> peerLists)
        {

        }

        static async Task Main(string[] args)
        {
            var filePath = Path.Combine(Environment.CurrentDirectory, "Downloads"); 
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath); 
            if (torrentsPath.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var torrent = await Torrent.LoadAsync(torrentsPath);
                    if(torrent.AnnounceUrls!=null)
                        torrentFileInfo.TrackerUrl = torrent.AnnounceUrls[0].FirstOrDefault();
                    torrentFileInfo.TorrentHash = torrent.InfoHash.GetHashCode();
                    torrentFileInfo.PiecesLength = torrent.PieceLength;
                    torrentFileInfo.PiecesCount = torrent.Pieces.Count;
                    torrentFileInfo.PieceHashes = new byte[torrentFileInfo.PiecesCount][];
                    torrentFileInfo.PathSource = PathSource;
                    torrentFileInfo.PathNew = PathNew; 
                    EnablePeerConnections(1300);

                    clientTracker = new TcpClient();
                    clientTracker.Connect("127.0.0.1", 60000);
                    var stream = clientTracker.GetStream();
                    List<int> pieces = new List<int>();
                    for (int i = 0; i < torrent.Pieces.Count; i++) pieces.Add(i);
                    PortToConnectToPeer connSettings = new PortToConnectToPeer(1300, pieces, ID);
                    TransportObject ob = new TransportObject(connSettings);
                    ob.SendObject(stream);
                    ClientListener listener = new ClientListener(clientTracker,ReceivePeerList, AddPeer);
                    threadListen = new Thread(new ThreadStart(listener.Listen));
                    threadListen.Start();

                    Peer p1 = new Peer(torrentFileInfo);
                    p1.ConnectToPeer(1301); 
                    while (!p1.IsConnected && !Peers.Any()) { }
                    for (int i=0;i< torrent.Pieces.Count; i++)
                    {
                        var byteResult = torrent.Pieces.ReadHash(i);
                        torrentFileInfo.PieceHashes[i] = byteResult;
                        Piece p = torrentFileInfo.ReadFilePiece(i); 
                        if (p == null)
                            continue;
                        p1.SendPiece(p);
                        Thread.Sleep(250);
                        //torrentFileInfo.WriteFilePiece(i,p.data); 
                    }
                    Console.ReadLine();
                    listener.StopListener = true;
                    threadListen.Join();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            Console.ReadKey();
        }
        
    }
}
