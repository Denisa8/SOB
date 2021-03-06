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
        public static int PeerCount = 1; 
        public static readonly int messageMetadataSize = 29;
        public static List<PendingMessage> Incoming { get; set; } = new List<PendingMessage>();
        public static List<PendingMessage> Outgoing { get; set; } = new List<PendingMessage>(); // W tych dwoch listach sa wszystkie wiadomosci ktore trzeba przetworzyc
        public static ConcurrentDictionary<int, Peer> Peers { get; } = new ConcurrentDictionary<int, Peer>();
        public static ConcurrentDictionary<string, FileTorrent> Files { get; } = new ConcurrentDictionary<string, FileTorrent>();
        public static ConcurrentBag<ConnectedPeer> AvailablePeersOnTracker { get; } = new ConcurrentBag<ConnectedPeer>();
        private static TcpListener listener { get; set; }
        public static TcpClient clientTracker { get; private set; }
        public static List<Guid> bannedPeers = new List<Guid>();
        static Guid lastConnectedGuid;
        static Timer timerTracker;
        //static Peer peer;
        public static string torrentsPath;
        public static string PathSource;
        public static string PathNew;
        private static DateTime LastSentRequest = new DateTime();
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

            Console.WriteLine("Połączenie od:  " + p + "\t(moj port: " + Settings.port + ") -- GUID = " + lastConnectedGuid); //sprawdz czy dobry port
            AddPeer(new Peer(client, lastConnectedGuid), p); 
        }
        public static void AddPeer(Peer peer, int p)
        {
            Random rand = new Random();
            peer.ConnectToPeer(p);
            peer.LastActive = DateTime.Now;
            if (!Program.Peers.TryAdd(PeerCount, peer))
                peer.Disconnect();
            if (peer.GUID == Guid.Empty)
            {
                peer.SendGUIDRequest(PeerCount);
            }
            PeerCount++;
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
                    TorrentFileInfo.PiecesLength = torrent.PieceLength;
                    TorrentFileInfo.PiecesCount = torrent.Pieces.Count;
                    TorrentFileInfo.PieceHashes = new byte[TorrentFileInfo.PiecesCount][];
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
                    //peer = new Peer();
                    Settings.ReadPieces = new bool[TorrentFileInfo.PiecesCount];
                    EnablePeerConnections(Settings.port);
                    #endregion
                    #region Tracker
                    clientTracker = new TcpClient();
                    clientTracker.Connect(Settings.trackerIp, Settings.trackerPort);
                    Console.WriteLine("Connected to tracker");
                    var stream = clientTracker.GetStream();
                 
                    List<int> pieces = new List<int>();
                    int pc = Settings.torrentFileInfo.GetSavedPiecesCount();

                    for (int i = 0; i < pc; i++)
                    {
                        Settings.ReadPieces[i] = true;
                        pieces.Add(i);
                    }

                    FileTorrent file = new FileTorrent(TorrentFileInfo.TorrentHash.ToString(), pieces, TorrentFileInfo.PiecesCount);
                    Files.TryAdd(TorrentFileInfo.TorrentHash.ToString(), file);


                    //for (int i = 0; i < torrent.Pieces.Count; i++) { pieces.Add(i); };
                    //FileTorrent file = new FileTorrent(TorrentFileInfo.TorrentHash.ToString(), pieces, pieces.Count);
                    //Files.TryAdd(TorrentFileInfo.TorrentHash.ToString(), file);


                    //if ((Settings.port == 1300)) // host na 1300 ma caly plik
                    //    for (int i = 0; i < Settings.ReadPieces.Length; i++)
                    //    {// TEMP 
                    //        Settings.ReadPieces[i] = true;
                    //    }

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
                            if (Settings.availablePeer)
                            {
                                Thread.Sleep(5000);
                                Console.WriteLine("---- client.port: " + Settings.port);
                                Console.WriteLine("Having " + Settings.ReadPieces.Where(ffs => ffs == true).Count() + " pieces");

                                if (Settings.ReadPieces.Where(x => x == false).Count() == 0) // klient posiada wszystkie czesci
                                {
                                    Thread.Sleep(10000); // narazie nie wiem co z tym zrobic. teraz jak wszystkie czesci sa pobrane to ten watek jest usypiany
                                    continue;
                                }
                                for (int i = 0; i < Settings.ReadPieces.Length;)
                                {
                                    if (Settings.ReadPieces[i] == false)
                                    {
                                        List<Peer> peersThatICanFinallySendRequestTo = new List<Peer>(); // Do tej listy zbierane sa wszystkie peery z danym kawalkiem
                                        foreach (ConnectedPeer connectedPeer in AvailablePeersOnTracker)
                                        {
                                            if (connectedPeer.Files.TryGetValue(TorrentFileInfo.TorrentHash.ToString(), out FileTorrent fileTorrent))
                                            {
                                                if (fileTorrent.Pieces.Contains(i)) // ustalenie czy dany peer ma kawalek
                                                {
                                                    foreach (KeyValuePair<int, Peer> pair in Peers)
                                                    {
                                                        if (((pair.Value.GUID == connectedPeer.ID) && (!bannedPeers.Contains(pair.Value.GUID)) && (!pair.Value.PieceRequestSent.Contains(i))))
                                                            peersThatICanFinallySendRequestTo.Add(pair.Value);
                                                    }
                                                }
                                            }
                                        }
                                        if (peersThatICanFinallySendRequestTo.Count == 0) // jesli nie ma zadnych dostepnych peerow 
                                        {
                                            //Console.WriteLine("No peers with needed piece");
                                            var x = (DateTime.Now - LastSentRequest).TotalMinutes;
                                            if (x > 2) // oraz ostatni request zostal wyslany 2 minuty temu
                                            {
                                                foreach (KeyValuePair<int, Peer> p in Peers) // resetuj status wyslania requestu o czesc u kazdego polaczonego peera 
                                                {
                                                    if (p.Value.PieceRequestSent.Contains(i))
                                                        p.Value.PieceRequestSent.Remove(i);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Random r = new Random();
                                            var index = r.Next(peersThatICanFinallySendRequestTo.Count);
                                            Console.WriteLine("Sending request to peer with index: " + index + " for piece: " + i);
                                            peersThatICanFinallySendRequestTo[index].SendMessage(i); // wysylanie zapytania do losowego peera z listy, zeby rozkladac ruch
                                            peersThatICanFinallySendRequestTo[index].PieceRequestSent.Add(i);
                                            LastSentRequest = DateTime.Now;
                                        }
                                        if (Settings.ReadPieces[i] == true)
                                        {
                                            Console.WriteLine("Piece " + i + " aquired. Moving to the next piece.");
                                            i++;
                                        }
                                    }
                                    else
                                        i++;
                                }
                            }
                        }
                    })).Start();
                    #endregion
                    # region przetwarzanie zapytan wychodzacych
                    new Thread(new ThreadStart(() =>
                    {
                        while (!Settings.isStopping)
                        {
                            if (Settings.availablePeer)
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
                        }
                    })).Start();
                    #endregion
                    #region przetwarzanie przychodzacych wiadomosci
                    new Thread(new ThreadStart(() =>
                    {
                        while (!Settings.isStopping)
                        {
                            if (Settings.availablePeer) {
                            Console.WriteLine("Incoming.Count = " + Incoming.Count);
                            if (Incoming.Count == 0)
                            {
                                Thread.Sleep(5000);
                                continue;
                            }

                            var pendingMessage = Incoming[0];
                            if (bannedPeers.Contains(pendingMessage.Guid))
                            {
                                Incoming.Remove(pendingMessage);
                                continue;
                            }
                            if (pendingMessage.Type == 1)
                            {
                                Console.WriteLine("Got piece with index: " + pendingMessage.PieceIndex);
                                int id = EndianBitConverter.Big.ToInt32(pendingMessage.EncodedMessage, 0); //tutaj masz, który kawałek Ci przyszedł
                                if (Settings.ReadPieces[pendingMessage.PieceIndex] == true) // jesli juz mamy taka czesc to zignorowac wiadomosc
                                {
                                    Console.WriteLine(pendingMessage.PieceIndex + " --- Got it");
                                    Incoming.Remove(pendingMessage);
                                }
                                else
                                {
                                    //  Console.WriteLine("Saving piece: " + pendingMessage.PieceIndex + " from incoming");
                                    Settings.torrentFileInfo.WriteFilePiece(pendingMessage.PieceIndex, pendingMessage.EncodedMessage);
                                    Settings.ReadPieces[pendingMessage.PieceIndex] = true;

                                    //  Console.WriteLine("Wysylanie na tracker ze otrzymano " + pendingMessage.PieceIndex + "czesc pliku " + torrent.Name);
                                    var receivePieceFile = new ReceivePieceFile(TorrentFileInfo.TorrentHash.ToString(), pendingMessage.PieceIndex);  
                                    Tools.Send(clientTracker.GetStream(), new TransportObject((object)receivePieceFile));
                                    Incoming.Remove(pendingMessage);

                                }
                            }
                            else if (pendingMessage.Type == 0)
                            {
                                byte[] encodedMessage;
                                Console.WriteLine("Got request for piece: " + pendingMessage.PieceIndex);
                                if (Settings.sendCorrectData)
                                    encodedMessage = Peer.EncodePiece(Settings.torrentFileInfo.ReadFilePiece(pendingMessage.PieceIndex), 1);// wczytaj piece jako tablice bajtow);
                                else
                                {
                                    encodedMessage = Peer.EncodeWrongPiece(pendingMessage.PieceIndex, 1);// wczytaj piece jako tablice bajtow); 
                                    Console.WriteLine("Wysyłanie błędnej wiadomości");
                                }
                                Outgoing.Add(new PendingMessage
                                {
                                    PieceIndex = pendingMessage.PieceIndex,
                                    EncodedMessage = encodedMessage,
                                    Stream = pendingMessage.Stream,
                                    Type = 1
                                });

                                Console.WriteLine("Saving piece: " + pendingMessage.PieceIndex + " to outgoing");
                                
                                Incoming.Remove(pendingMessage);
                            }
                            else if (pendingMessage.Type == 2)
                            {
                                Outgoing.Add(new PendingMessage
                                {
                                    EncodedMessage = Peer.EncodeGUIDResponse(pendingMessage.IndexPeer),
                                    Type = 3,
                                    Stream = pendingMessage.Stream,
                                    IndexPeer = pendingMessage.IndexPeer
                                });
                                Incoming.Remove(pendingMessage);

                            }
                            else if (pendingMessage.Type == 3)
                            {
                                bool success = false;

                                foreach (KeyValuePair<int, Peer> peer in Peers)
                                {
                                    if (peer.Key == pendingMessage.IndexPeer)
                                    {
                                        peer.Value.GUID = pendingMessage.Guid;
                                        success = true;
                                        break;
                                    }
                                }
                                if (!success)
                                    Console.WriteLine("Couldnt save GUID");

                                Incoming.Remove(pendingMessage);
                            }
                            else
                            {
                                Console.WriteLine("Odebrano niepoprawna wiadomosc");
                            }
                            Incoming.Remove(pendingMessage);
                            }
                        }
                    })).Start();
                    #endregion
                    #region zarzadzanie peerami
                    new Thread(new ThreadStart(() =>
                    { 
                        while(!Settings.isStopping)
                        {
                            Thread.Sleep(5000);
                            var peers = Peers.ToList();
                            foreach (KeyValuePair<int, Peer> peer in peers)
                            {
                                var lastActiveInMinutes = DateTime.Now - peer.Value.LastActive;
                                if (lastActiveInMinutes.TotalMinutes > Settings.peerTimeoutInMinutes) // jezeli peer jest nieaktywny przez zadany czas 
                                {
                                    Console.WriteLine("peer " + peer.Value.GUID + " timed out");
                                    if (Peers.TryRemove(peer.Key, out Peer p))
                                    {
                                        peer.Value.Disconnect(); // rozlacz peera
                                        Console.WriteLine("peer " + p.GUID + " disconnected");
                                    }
                                }
                            }
                        }
                    })).Start();
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
            torrentsPath = @"C:\Users\Admin\Desktop\wyklady2.torrent";
            if (args.Length == 0) // jesli nie ma podanych argumentow to przyjmij domyslne wartosci
            {/*
               torrentsPath = @"D:\a\Bees.torrent";
              PathSource = @"D:\a\Bees.txt";
              PathNew = @"D:\a\Downloaded\Bees.torrent";*/
                Settings.port = 1301; 
                PathSource = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wykladyKopia5.zip";
                PathNew = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wykladyKopia5.zip"; 

                //public static string torrentsPath = "wyklady2.torrent";
                //public static string PathSource = @"C:\Users\Admin\Desktop\wyklady.zip";
                //public static string PathNew = @"c:\Users\Admin\Desktop\wyklady3.zip";

                return;
            }

            if (args.Length == 1) // jesli nie ma podanych argumentow to przyjmij domyslne wartosci
            {/*
               torrentsPath = @"D:\a\Bees.torrent";
               PathSource = @"D:\a\Bees.txt";
               PathNew = @"D:\a\Downloaded\Bees.torrent"; */
                Settings.port = int.Parse(args[0]);
                PathSource = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wyklady.zip";
                PathNew = @"C:\Users\Admin\Desktop\SOB - projekt\plik\wykladyKopia.zip";
                Console.WriteLine(PathSource);

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
                if (String.IsNullOrEmpty(args[1]))
                {
                    Console.WriteLine("Niepoprawna ścieżka do folderu");
                    return;
                }
                else
                { 
                    PathSource = PathNew = Path.Combine(args[1],  String.Format("w{0}.zip", Settings.port));
                    Console.WriteLine(PathSource);
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
                            Peer p1 = new Peer(l.ID);
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
                        Peer p1 = new Peer(cp.ID);
                        //lastConnectedGuid = p1.GUID;
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
                                p.Files.Add(ipa.File.ID, ipa.File);
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
