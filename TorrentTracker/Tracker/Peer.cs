using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TorrentTracker.Tracker.Data;

namespace TorrentTracker.Tracker
{
    public class Peer
    {
        public Dictionary<string, FileTorrent> Files { get; private set; }
        public List<Guid> BannedPeers { get; private set; }
        public Guid ID { get; private set; }
        public bool Available { get => available; set { faultCheckAvailable = value ? 0 : faultCheckAvailable; available = value; } }
        public bool SendCorrectData { get; set; }
        public string IP { get; private set; }
        public int Port { get; private set; }
        private PeerListener listener;
        private CancellationTokenSource cts;
        private TcpClient client;
        private bool available;
        private int faultCheckAvailable;

        public Peer(Guid iD, string iP, int port, TcpClient client, Dictionary<string, FileTorrent> files, List<Guid> bannedPeers)
        {
            ID = iD;
            IP = iP;
            Port = port;
            this.client = client;
            Files = files;
            BannedPeers = bannedPeers;
            Available = true;
            SendCorrectData = true;
            listener = new PeerListener(client, this);
            cts = new CancellationTokenSource();
            Task.Run(() => listener.Run(cts.Token), cts.Token);
        }

        public void Close()
        {
            listener.Close();
            cts.Cancel();
        }

        public void CheckAvailable()
        {
            Available = false;
            var s = client.GetStream();
            try
            {
                if (faultCheckAvailable < 5)
                    Tools.Send(s, new TransportObject(new CheckAvailablePeer()), 1000);
            }
            catch (IOException) { faultCheckAvailable++; }
        }

        public void InformAboutNewConnectedPeer(Peer peer)
        {
            if (!available) return;
            var s = client.GetStream();
            try
            {
                Tools.Send(s, new TransportObject(new ConnectedPeer(peer)), 1000);
            }
            catch (IOException) { }
        }

        internal void SetClient(TcpClient client)
        {
            this.client = client;
        }

        public void InformPeerAboutAvailable(bool available)
        {
            Available = available;
            Tools.Send(client.GetStream(), new TransportObject(new ChangeAvailablePeer(available)), 1000);
        }

        public void InformPeerAboutSendCorrectData(bool sendCorrectData)
        {
            SendCorrectData = sendCorrectData;
            Tools.Send(client.GetStream(), new TransportObject(new ChangeSendDataPeer(sendCorrectData)), 1000);
        }

        public void SendPeerList(List<Peer> peers)
        {
            List<ConnectedPeer> cp = new List<ConnectedPeer>();
            foreach (var peer in peers)
            {
                if (peer.ID != ID && peer.available)
                    cp.Add(new ConnectedPeer(peer));
            }
            try
            {
                if (cp.Count > 0)
                    Tools.Send(client.GetStream(), new TransportObject(cp));
            }
            catch (IOException) { }
        }

        public void InformPeerAboutReceiveNewPieceFile(Guid peer,string file,int index)
        {
            Tools.Send(client.GetStream(), new TransportObject(new InformPeerAboutNewReceivePiece(peer, file, index)),1000);
        }

        public void InformPeerAboutNewFile(Guid peer, FileTorrent file)
        {
            Tools.Send(client.GetStream(), new TransportObject(new PeerReceiveFile(peer, file)));
        }
    }
}