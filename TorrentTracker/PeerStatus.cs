using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TorrentTracker.Data;

namespace TorrentTracker
{
    public delegate void CloseConnectionCallback();
    public delegate void AddPieceEventHandler(object sender);
    public delegate void ChangeAvailableEventHandler(object sender);

    public class PeerStatus
    {
        public event AddPieceEventHandler AddPieceEvent;
        public event ChangeAvailableEventHandler ChangeAvailableEvent;
        public Guid ID { get; private set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public bool Available { 
            get => available; 
            set {
                if (available != value)
                {
                    available = value;
                    InformPeerAboutAvailable();
                }
            } 
        }
        public int[] Pieces { 
            get { 
                pieces.Sort(); return pieces.ToArray(); 
            } 
        }

        private List<int> pieces = new List<int>();
        private TcpClient client;
        private bool available;
        private RemovePeerCallBack RemovePeerCallBack;
        private Thread thread;

        public PeerStatus(TcpClient peer, int peerPort, RemovePeerCallBack remove, List<int> pieces)
        {
            ID = Guid.NewGuid();
            client = peer;
            IP = ((IPEndPoint)peer.Client.RemoteEndPoint).Address.ToString();
            Available = true;
            Port = peerPort;
            RemovePeerCallBack = remove;
            if (pieces != null)
                this.pieces = pieces;
            else
                this.pieces = new List<int>();

            PeerListener listener = new PeerListener(AddPiece, client, CloseConnection);
            thread = new Thread(new ThreadStart(listener.Listen));
        }
        private void CloseConnection()
        {
            RemovePeerCallBack(this);
        }
        private void AddPiece(int index)
        {
            pieces.Add(index);
            if (AddPieceEvent != null)
                AddPieceEvent(this);
        }

        public void Close()
        {
            client.Close();
        }

        public void InformAboutNewPeer(PeerStatus peer)
        {
            var stream = client.GetStream();
            new TransportObject(new NewPeer(peer.IP,peer.Port)).SendObject(stream);
        }

        private void InformPeerAboutAvailable()
        {
            var stream = client.GetStream();
            new TransportObject(new PeerAvailable(available)).SendObject(stream);
        }

        public void SendPeerList(List<PeerStatus> peers)
        {
            var stream = client.GetStream();
            List<PeerListElement> list = new List<PeerListElement>();
            foreach(var peer in peers)
            {
                list.Add(new PeerListElement(peer.IP, peer.Port));
            }
            new TransportObject(list).SendObject(stream);
        }

        public void CheckAvailable(CheckAvailableCallback callback)
        {
           new TransportObject(new CheckAvailablePeer()).SendObject(client.GetStream());
            
            bool tmp = available;
            try
            {
                var ob = Tools.Receive(client.GetStream(),3000).TryCast<CheckAvailablePeer>();
                available = ob.Available;
            }
            catch (IOException) {
                available = false;
            }

            callback(this, Available);

            if (ChangeAvailableEvent != null && tmp != available)
                ChangeAvailableEvent(this);            
        }
    }
}
