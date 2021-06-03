using MiscUtil.Conversion;
using MonoTorrent;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TorrentClient.Messages;
using TorrentTracker.Tracker;
using TorrentTracker.Tracker.Data;

namespace TorrentClient
{
    public class Program
    {
        public static readonly int messageMetadataSize = 29;
        public static List<PendingMessage> Incoming { get; set; } = new List<PendingMessage>();
        public static List<PendingMessage> Outgoing { get; set; } = new List<PendingMessage>(); // W tych dwoch listach sa wszystkie wiadomosci ktore trzeba przetworzyc
        public static ConcurrentDictionary<int, Peer> Peers { get; } = new ConcurrentDictionary<int, Peer>();
        public static ConcurrentDictionary<string, FileTorrent> Files { get; } = new ConcurrentDictionary<string, FileTorrent>();
        public static ConcurrentBag<ConnectedPeer> AvailablePeersOnTracker { get; } = new ConcurrentBag<ConnectedPeer>();
        private static TcpListener listener { get; set; }
        public static TcpClient clientTracker { get; private set; }
        static List<Guid> bannedPeers = new List<Guid>();
        static Timer timerTracker;
        static Peer peer;
        public static string torrentsPath;
        public static string PathSource;
        public static string PathNew;
        public static void EnablePeerConnections(int Port)
        {
            Settings.port = Port;
            listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(HandlePeerConnection), null);
        }

        public static void HandlePeerConnection(IAsyncResult ar)
        {
            if (listener == null)
                return;

            TcpClient client = listener.EndAcceptTcpClient(ar);
            listener.BeginAcceptTcpClient(new AsyncCallback(HandlePeerConnection), null);
            var p = ((IPEndPoint)client.Client.RemoteEndPoint).Port;
            Console.WriteLine("Połączenie od:  " + p + "\t(moj port: " + Settings.port + ")"); //sprawdz czy dobry port
            AddPeer(new Peer(client), p);
        }
        public static void AddPeer(Peer peer, int p)
        {
            Random rand = new Random();
            peer.ConnectToPeer(p);
            if (!Program.Peers.TryAdd(rand.Next(), peer))
                peer.Disconnect();
        }
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
                        Settings.torrentFileInfo.TrackerUrl = torrent.AnnounceUrls[0].FirstOrDefault();
                    TorrentFileInfo.TorrentHash = torrent.InfoHash.GetHashCode();
                    Settings.torrentFileInfo.PiecesLength = torrent.PieceLength;
                    Settings.torrentFileInfo.PiecesCount = torrent.Pieces.Count;
                    TorrentFileInfo.PieceHashes = new byte[Settings.torrentFileInfo.PiecesCount][];
                    //Settings.torrentFileInfo.ReadPieces =  //dołożyłam, aby sprawdzać, który kawałek dostaliśmy
                    Settings.torrentFileInfo.PathSource = PathSource;
                    Settings.torrentFileInfo.PathNew = PathNew;
                    for (int i = 0; i < torrent.Pieces.Count; i++)
                    {
                        var byteResult = torrent.Pieces.ReadHash(i);
                        TorrentFileInfo.PieceHashes[i] = byteResult;
                    }
                    #endregion
                    #region Utworzenie peera
                    peer = new Peer();
                    Settings.ReadPieces = new bool[Settings.torrentFileInfo.PiecesCount];
                    EnablePeerConnections(Settings.port);
                    #endregion
                    #region Tracker
                    clientTracker = new TcpClient();
                    clientTracker.Connect(Settings.trackerIp, Settings.trackerPort);
                    Console.WriteLine("Connected to tracker");
                    var stream = clientTracker.GetStream();

                    List<int> pieces = new List<int>();
                    for (int i = 0; i < torrent.Pieces.Count; i++) pieces.Add(i);
                    FileTorrent file = new FileTorrent(TorrentFileInfo.TorrentHash.ToString(), pieces, pieces.Count);
                    Files.TryAdd(TorrentFileInfo.TorrentHash.ToString(), file);


                    if ((Settings.port == 1300)) // host na 1300 ma caly plik
                        for (int i = 0; i < Settings.ReadPieces.Length; i++)
                        {// TEMP 
                            Settings.ReadPieces[i] = true;
                        }

                    InitConnectToTracker connSettings = new InitConnectToTracker(Settings.ID, Settings.peerIp, Settings.port, Files.ToDictionary(x => x.Key, x => x.Value), bannedPeers);
                    TransportObject ob = new TransportObject(connSettings);
                    ob.SendObject(stream);

                    //uruchamianie listenera
                    timerTracker = new Timer(async (o) => await ListenTracker(), null, Timeout.Infinite, Timeout.Infinite);
                    timerTracker.Change(0, Timeout.Infinite);
                    #endregion

                    #region wysylanie zapytan o kolejne czesci
                    new Thread(new ThreadStart(() =>
                    {
                        while (!Settings.isStopping)
                        {

                            Thread.Sleep(5000);
                            Console.WriteLine("---- client.port: " + Settings.port);
                            Console.WriteLine("Having " + Settings.ReadPieces.Where(ffs => ffs == true).Count() + " pieces");

                            var keys = Peers.Keys;
                            Console.WriteLine("---- Peer.Peers.count = " + keys.Count());
                            foreach (int key in keys)
                            {
                                Peers.TryGetValue(key, out Peer p);
                                Console.WriteLine("peer.Port: " + p.Port);
                            }

                            if (Settings.ReadPieces.Where(x => x == false).Count() == 0) // klient posiada wszystkie czesci
                            {
                                Thread.Sleep(10000); // narazie nie wiem co z tym zrobic. teraz jak wszystkie czesci sa pobrane to ten watek jest usypiany
                                continue;
                            }
                            for (int i = 0; i < Settings.ReadPieces.Length; i++)
                            {
                                if (Settings.ReadPieces[i] == false)
                                {
                                    foreach (int key in keys)
                                    {
                                        if (!Peers.TryGetValue(key, out Peer availablePeer))
                                            continue;

                                        Console.WriteLine("Sending request for ID: " + i + " to " + availablePeer.Port);
                                        //tutaj te kawałki sprawdzic
                                        Peers[key].SendMessage(i);

                                        Console.WriteLine("Request for ID: " + i + " sent");
                                    }
                                }
                            }
                        }
                    })).Start();
                    #endregion przetwarzanie zapytan wychodzacych
                    new Thread(new ThreadStart(() =>
                    {
                        while (!Settings.isStopping)
                        {
                            if (Outgoing.Count == 0)
                            {
                                Thread.Sleep(1000);
                                continue;
                            }
                            var pendingMessage = Outgoing[0];
                            Console.WriteLine("Sending ID: " + pendingMessage.PieceIndex + " from outgoing.");
                            pendingMessage.Send();
                            Outgoing.Remove(pendingMessage);
                        }
                    })).Start();
                    // przetwarzanie przychodzacych wiadomosci
                    new Thread(new ThreadStart(() =>
                    {
                        while (!Settings.isStopping)
                        {
                            Console.WriteLine("Incoming.Count = " + Incoming.Count);
                            if (Incoming.Count == 0)
                            {
                                Thread.Sleep(5000);
                                continue;
                            }
                            var pendingMessage = Incoming[0];
                            Console.WriteLine("Processing ID: " + pendingMessage.PieceIndex + " from incoming");
                            if (pendingMessage.Type == 1)
                            {
                                int id = EndianBitConverter.Big.ToInt32(pendingMessage.EncodedMessage, 0); //tutaj masz, który kawałek Ci przyszedł
                                Console.WriteLine(pendingMessage.PieceIndex + " --- TYPE 1");
                                if (Settings.ReadPieces[pendingMessage.PieceIndex] == true) // jesli juz mamy taka czesc to zignorowac wiadomosc
                                {
                                    Console.WriteLine(pendingMessage.PieceIndex + " --- Got it");
                                    Incoming.Remove(pendingMessage);
                                    continue;
                                }
                                Console.WriteLine("Saving piece: " + pendingMessage.PieceIndex + " from incoming");
                                Settings.torrentFileInfo.WriteFilePiece(pendingMessage.PieceIndex, pendingMessage.EncodedMessage);
                                if (pendingMessage.PieceIndex < Settings.ReadPieces.Length)
                                    Settings.ReadPieces[pendingMessage.PieceIndex] = true;

                                Incoming.Remove(pendingMessage);
                            }
                            else if (pendingMessage.Type == 0)
                            {
                                int type = 1;
                                var encodedMessage = Peer.EncodePiece(Settings.torrentFileInfo.ReadFilePiece(pendingMessage.PieceIndex), 1);// wczytaj piece jako tablice bajtow);
                                Console.WriteLine("Saving piece: " + pendingMessage.PieceIndex + " to outgoing");
                                Outgoing.Add(new PendingMessage
                                {
                                    PieceIndex = pendingMessage.PieceIndex,
                                    EncodedMessage = encodedMessage,
                                    Stream = pendingMessage.Stream,
                                    Type = 1
                                });
                            }
                            else
                            {
                                Console.WriteLine("Odebrano niepoprawna wiadomosc");
                            }
                            Incoming.Remove(pendingMessage);
                        }
                    })).Start();


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
            if (args.Length == 0) // jesli nie ma podanych argumentow to przyjmij domyslne wartosci
            {
                /*torrentsPath = @"D:\a\Bees.torrent";
             PathSource = @"D:\a\Bees.txt";
             PathNew = @"D:\a\Downloaded\Bees.torrent";*/
             Settings.port = 1301;
                torrentsPath = @"C:\Users\Admin\Desktop\wyklady2.torrent";
                PathSource = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wyklady.zip";
                PathNew = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wykladyKopia2.zip";

                //public static string torrentsPath = "wyklady2.torrent";
                //public static string PathSource = @"C:\Users\Admin\Desktop\wyklady.zip";
                //public static string PathNew = @"c:\Users\Admin\Desktop\wyklady3.zip";

                return;
            }

            if (args.Length == 1) // jesli nie ma podanych argumentow to przyjmij domyslne wartosci
            {/*
                torrentsPath = @"D:\a\Bees.torrent";
                PathSource = @"D:\a\Bees.txt";
                PathNew = @"D:\a\Downloaded\Bees.torrent";*/
                Settings.port = int.Parse(args[0]);
                torrentsPath = @"C:\Users\Admin\Desktop\wyklady2.torrent";
                  PathSource = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wyklady.zip";
                  PathNew = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wykladyKopia2.zip";

                //public static string torrentsPath = "wyklady2.torrent";
                //public static string PathSource = @"C:\Users\Admin\Desktop\wyklady.zip";
                //public static string PathNew = @"c:\Users\Admin\Desktop\wyklady3.zip";

                return;
            }


            try
            {
                Settings.port = Convert.ToInt32(args[0]);
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
                    PathSource = Path.Combine(args[2], "Bees.txt");
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
                Console.Write(Settings.ID + " - ");
                if (ob != null)
                {
                    if (ob.Type == typeof(List<ConnectedPeer>))
                    {
                        var lcp = ob.TryCast<List<ConnectedPeer>>();

                        AvailablePeersOnTracker.Take(AvailablePeersOnTracker.Count);

                        Console.WriteLine("ListPeer");
                        foreach (var l in lcp)
                        {
                            AvailablePeersOnTracker.Add(l);
                            Peer p1 = new Peer(); 
                            p1.ConnectToPeer(l.Port);
                            //if (p1.IsConnected)
                            //{
                            //  Random rand = new Random(); 
                            //  if (!Program.Peers.TryAdd(rand.Next(), p1))
                            //    p1.Disconnect();
                            //} 
                            Console.WriteLine("peer: " + l.ID);
                        }
                    }
                    else if (ob.Type == typeof(ConnectedPeer))
                    {
                        var cp = ob.TryCast<ConnectedPeer>();
                        Console.WriteLine("Connected new peer: " + cp.ID);
                        AvailablePeersOnTracker.Add(cp);
                        Peer p1 = new Peer();
                        p1.ConnectToPeer(cp.Port);
                        //if (p1.IsConnected)
                        //{
                        //  Random rand = new Random();
                        //  if (!Program.Peers.TryAdd(rand.Next(), p1))
                        //    p1.Disconnect();
                        //}
                    }
                    else if (ob.Type == typeof(CheckAvailablePeer))
                    {
                        Console.WriteLine("Check available");
                        var cap = ob.TryCast<CheckAvailablePeer>();
                        cap.Available = Settings.availablePeer;
                        try
                        {
                            Tools.Send(clientTracker.GetStream(), new TransportObject(cap));
                        }
                        catch (IOException)
                        {
                            if (!clientTracker.Connected)
                            {
                                await clientTracker.ConnectAsync(Settings.trackerIp, Settings.trackerPort);
                            }
                        }
                    }
                    else if (ob.Type == typeof(ChangeAvailablePeer))
                    {
                        var cap = ob.TryCast<ChangeAvailablePeer>();
                        Console.WriteLine("Change available peer: " + cap.Available);
                        Settings.availablePeer = cap.Available;
                    }
                    else if (ob.Type == typeof(ChangeSendDataPeer))
                    {
                        var csdp = ob.TryCast<ChangeSendDataPeer>();
                        Console.WriteLine("Change send data peer: " + csdp.CorrectSendData);
                        Settings.sendCorrectData = csdp.CorrectSendData;
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
                    else if (ob.Type == typeof(PeerReceiveFile))
                    {
                        var ipa = ob.TryCast<PeerReceiveFile>();
                        Console.WriteLine("Receive file: " + ipa.ID);
                        try
                        {
                            var p = AvailablePeersOnTracker.First(x => x.ID == ipa.ID);
                            if (!p.Files.ContainsKey(ipa.File.ID))
                            {
                                p.Files.Add(ipa.File.ID,ipa.File);
                            }
                        }
                        catch (Exception) { }
                    }
                }
                timerTracker.Change(0, Timeout.Infinite);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }
}
