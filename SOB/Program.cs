using MonoTorrent;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TorrentTracker.Tracker;
using TorrentTracker.Tracker.Data;

namespace TorrentClient
{
    class Program
    {
        static TorrentFileInfo torrentFileInfo = new TorrentFileInfo();
        public static ConcurrentDictionary<int, Peer> Peers { get; } = new ConcurrentDictionary<int, Peer>();
        public static ConcurrentDictionary<string, FileTorrent> Files { get; } = new ConcurrentDictionary<string, FileTorrent>();
        public static ConcurrentBag<ConnectedPeer> AvailablePeersOnTracker { get; } = new ConcurrentBag<ConnectedPeer>();
        private static int port { get; set; }

        public static readonly string peerIp = "127.0.0.1";
        private static Guid ID = Guid.NewGuid();
        public static TcpClient clientTracker { get; private set; }
        public static readonly string trackerIp = "127.0.0.1";
        public static readonly int trackerPort = 60000;
        static List<Guid> bannedPeers = new List<Guid>();
        static bool availablePeer = true;
        static bool sendCorrectData = true;
        static Timer timerTracker;
        static bool isStopping = false;
        static Peer peer;


        
           public static string torrentsPath;
           public static string PathSource;
           public static string PathNew;

        /*   public static string torrentsPath = @"D:\a\Bees.torrent";
           public static string PathSource = @"D:\a\Bees.txt";
           public static string PathNew = @"D:\a\Downloaded\Bees.torrent";*/

        /*  public static string torrentsPath = @"C:\Users\Admin\Desktop\wyklady2.torrent";
          public static string PathSource = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wyklady.zip";
          public static string PathNew = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wykladyKopia2.zip";*/

        //public static string torrentsPath = "wyklady2.torrent";
        //public static string PathSource = @"C:\Users\Admin\Desktop\wyklady.zip";
        //public static string PathNew = @"c:\Users\Admin\Desktop\wyklady3.zip";

        static async Task Main(string[] args)
        {
            CheckArguments(args);

            var filePath = Path.Combine(Environment.CurrentDirectory, "Downloads");
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
            if (torrentsPath.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    #region pobranie danych z pliku torrent
                    var torrent = await Torrent.LoadAsync(torrentsPath);
                    if (torrent.AnnounceUrls != null)
                        torrentFileInfo.TrackerUrl = torrent.AnnounceUrls[0].FirstOrDefault();
                    torrentFileInfo.TorrentHash = torrent.InfoHash.GetHashCode();
                    torrentFileInfo.PiecesLength = torrent.PieceLength;
                    torrentFileInfo.PiecesCount = torrent.Pieces.Count;
                    torrentFileInfo.PieceHashes = new byte[torrentFileInfo.PiecesCount][];
                    torrentFileInfo.ReadPieces = new bool[torrentFileInfo.PiecesCount]; //dołożyłam, aby sprawdzać, który kawałek dostaliśmy
                    torrentFileInfo.PathSource = PathSource;
                    torrentFileInfo.PathNew = PathNew;
                    #endregion
                    #region Utworzenie peera
                    peer = new Peer(torrentFileInfo);
                    peer.EnablePeerConnections(port);
                    #endregion
                    #region Tracker
                    clientTracker = new TcpClient();
                    clientTracker.Connect(trackerIp, trackerPort);
                    Console.WriteLine("Connected to tracker");
                    var stream = clientTracker.GetStream();

                    List<int> pieces = new List<int>();
                    for (int i = 0; i < torrent.Pieces.Count; i++) pieces.Add(i);
                    FileTorrent file = new FileTorrent(torrentFileInfo.TorrentHash.ToString(), pieces, pieces.Count);
                    Files.TryAdd(torrentFileInfo.TorrentHash.ToString(), file);

                    InitConnectToTracker connSettings = new InitConnectToTracker(ID, peerIp, port, Files.ToDictionary(x => x.Key, x => x.Value), bannedPeers);
                    TransportObject ob = new TransportObject(connSettings);
                    ob.SendObject(stream);

                    //uruchamianie listenera
                    timerTracker = new Timer(async (o) => await ListenTracker(), null, Timeout.Infinite, Timeout.Infinite);
                    timerTracker.Change(0, Timeout.Infinite);
                    #endregion
                    while (true)
                    { }
                    //p1.ConnectToPeer(1301);
                    //while (!peer.IsConnected && !Peers.Any()) { }
                    //for (int i = 0; i < torrent.Pieces.Count; i++)
                    //{
                    //  var byteResult = torrent.Pieces.ReadHash(i);
                    //  torrentFileInfo.PieceHashes[i] = byteResult;
                    //  Piece p = torrentFileInfo.ReadFilePiece(i);

                    //  if (p == null)
                    //    continue;
                    //  peer.SendPiece(p);
                    //  Thread.Sleep(250);
                    //  //torrentFileInfo.WriteFilePiece(i,p.data); 
                    //}
                    Console.ReadLine();
                    clientTracker.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            Console.ReadKey();
        }

        private static void CheckArguments(string[] args)
        {
            try
            {
                port = Convert.ToInt32(args[0]);
                Console.WriteLine(args[0]);
            }
            catch (Exception)
            {
                Console.WriteLine("Niepoprawny port");
                return;
            }
            try
            {
                if (String.IsNullOrEmpty(args[2]))
                {
                    Console.WriteLine("Niepoprawna ścieżka do folderu");
                    return;
                }
                else
                {
                    torrentsPath = args[1];
                    PathSource = Path.Combine(args[2],"Bees.txt");
                    PathNew = Path.Combine(args[2], "Downloads", "Bees.txt");
                    torrentsPath = Path.Combine(args[1], "Bees.torrent");
                }

            }
            catch (Exception)
            {
                Console.WriteLine("Niepoprawna ścieżka do folderu");
                return;
            }
        }
        private static async Task ListenTracker()
        {
            try
            {
                TransportObject ob;
                try
                {
                    ob = Tools.Receive(clientTracker.GetStream(), 5000);
                }
                catch (Exception)
                {
                    timerTracker.Change(0, Timeout.Infinite);
                    return;
                }
                Console.Write(ID + " - ");
                if (ob != null)
                {
                    if (ob.Type == typeof(ChangeSendDataPeer))
                    {
                        var csdp = ob.TryCast<ChangeSendDataPeer>();
                        sendCorrectData = csdp.CorrectSendData;
                        Console.WriteLine("ChangeSendDataPeer: " + csdp.CorrectSendData);
                    }
                    else if (ob.Type == typeof(List<ConnectedPeer>))
                    {
                        var lcp = ob.TryCast<List<ConnectedPeer>>();

                        AvailablePeersOnTracker.Take(AvailablePeersOnTracker.Count);

                        Console.WriteLine("ListPeer");
                        foreach (var l in lcp)
                        {
                            AvailablePeersOnTracker.Add(l);
                            peer.ConnectToPeer(l.Port);
                            Console.WriteLine("peer: " + l.ID);
                        }
                    }
                    else if (ob.Type == typeof(ConnectedPeer))
                    {
                        var cp = ob.TryCast<ConnectedPeer>();
                        Console.WriteLine("Connected new peer: " + cp.ID);
                        AvailablePeersOnTracker.Add(cp);
                        peer.ConnectToPeer(cp.Port);
                    }
                    else if (ob.Type == typeof(CheckAvailablePeer))
                    {
                        Console.WriteLine("Check available");
                        var cap = ob.TryCast<CheckAvailablePeer>();
                        cap.Available = availablePeer;
                        try
                        {
                            Tools.Send(clientTracker.GetStream(), new TransportObject(cap));
                        }
                        catch (IOException)
                        {
                            if (!clientTracker.Connected)
                            {
                                await clientTracker.ConnectAsync(trackerIp, trackerPort);
                            }
                        }
                    }
                    else if (ob.Type == typeof(ChangeAvailablePeer))
                    {
                        var cap = ob.TryCast<ChangeAvailablePeer>();
                        Console.WriteLine("Change available peer: " + cap.Available);
                        availablePeer = cap.Available;
                    }
                    else if (ob.Type == typeof(ChangeSendDataPeer))
                    {
                        var csdp = ob.TryCast<ChangeSendDataPeer>();
                        Console.WriteLine("Change send data peer: " + csdp.CorrectSendData);
                        sendCorrectData = csdp.CorrectSendData;
                    }
                    else if (ob.Type == typeof(InformPeerAboutNewReceivePiece))
                    {
                        var ipa = ob.TryCast<InformPeerAboutNewReceivePiece>();
                        Console.WriteLine("Peer: " + ipa.ID + " file: " + ipa.File + " receive piece: " + ipa.Piece);
                        try
                        {
                            var p = AvailablePeersOnTracker.First(x => x.ID == ipa.ID);
                            if (p.Files[ipa.File].Pieces.Contains(ipa.Piece))
                            {
                                p.Files[ipa.File].Pieces.Add(ipa.Piece);
                            }
                        }
                        catch (Exception) { }
                    }
                }
                timerTracker.Change(0, Timeout.Infinite);
            }
            catch (Exception) { }
        }

    }
}
