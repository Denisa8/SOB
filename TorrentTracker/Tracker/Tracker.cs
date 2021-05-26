using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TorrentTracker.Models.DTO;
using TorrentTracker.Tracker.Data;

namespace TorrentTracker.Tracker
{
    public class Tracker
    {
        public static ConcurrentDictionary<Guid, Peer> Peers { get; private set; } = new ConcurrentDictionary<Guid, Peer>();
        private TrackerListener listener;
        private CancellationTokenSource cts;
        private static Tracker tracker;
        private Timer timer;

        private Tracker() { }

        private Tracker(string ip, int port)
        {
            TcpListener listener = new TcpListener(IPAddress.Parse(ip), port);
            this.listener = new TrackerListener(listener);
            listener.Start();
            cts = new CancellationTokenSource();
            Task.Run(() => this.listener.Run(cts.Token), cts.Token);
            timer = new Timer(CheckAvailable, null, Timeout.Infinite, Timeout.Infinite);
            timer.Change(0, Timeout.Infinite);
        }

        private void CheckAvailable(object state)
        {
            foreach (var kv in Peers)
            {
                try
                {
                    kv.Value.CheckAvailable();
                }
                catch (Exception) { }
            }

            timer.Change(10000, Timeout.Infinite);
        }

        public static void Init(string ip, int port)
        {
            if (tracker == null)
                tracker = new Tracker(ip, port);
        }

        public static Tracker GetInstance()
        {
            if (tracker == null)
                throw new InvalidOperationException("Tracker nie został zainicjowany.");
            return tracker;
        }

        public void Close()
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();
            listener.Close();
            cts.Cancel();

            {
                foreach (var p in Peers)
                {
                    p.Value.Close();
                }
            }
        }

        public void ChangeAvailablePeer(Guid guid, bool available)
        {
            if (!Peers.ContainsKey(guid)) throw new Exception("Nie istnieje peer o ID: "+guid);
            Peers[guid].InformPeerAboutAvailable(available);
        }

        public void ChangeSendDataPeer(Guid guid, bool correctData)
        {
            if (!Peers.ContainsKey(guid)) throw new Exception("Nie istnieje peer o ID: " + guid);
            Peers[guid].InformPeerAboutSendCorrectData(correctData);
        }

        public static List<PeerDTO> GetPeerList()
        {
            List<PeerDTO> l = new List<PeerDTO>();

            foreach (var ob in Peers)
            {
                var p = new PeerDTO();
                p.Available = ob.Value.Available;
                p.BannedPeers = ob.Value.BannedPeers;
                p.CorrectSendData = ob.Value.SendCorrectData;
                p.Files = new List<FileDTO>();
                foreach (var o in ob.Value.Files)
                    p.Files.Add(new FileDTO() { Filename = o.Key, Progress = (int)(o.Value.Pieces.Count / (float)o.Value.countPieces * 100.0) });
                p.ID = ob.Value.ID.ToString();
                l.Add(p);
            }
            return l;
        }
    }
}