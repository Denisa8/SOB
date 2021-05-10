using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TorrentTracker;
using TorrentTracker.Data;

namespace TorrentClient
{
    public delegate void ReceivePeerList(List<PeerListElement> list);
    public delegate void ReceiveNewPeer(Peer peer);

    public class ClientListener
    {
        private TcpClient tracker;
        public bool StopListener { get; set; }
        private ReceivePeerList peerList;
        private ReceiveNewPeer newPeer;

        public ClientListener(TcpClient tracker,ReceivePeerList callback,ReceiveNewPeer receiveNewPeer)
        {
            this.tracker = tracker;
            peerList = callback;
            newPeer = receiveNewPeer;
        }

        public void Listen()
        {
            var stream = tracker.GetStream();
            stream.ReadTimeout = 10000;
            while (!StopListener)
            {
                try
                {
                    var ob = Tools.Receive(stream);
                    if(ob.Type.Equals(typeof(CheckAvailablePeer)))
                    {
                        var cap = ob.TryCast<CheckAvailablePeer>();
                        cap.Available = true;
                        if(Program.Available)
                            new TransportObject(cap).SendObject(stream);
                    }
                    else if (ob.Type.Equals(typeof(List<PeerListElement>)))
                    {
                        var list = ob.TryCast<List<PeerListElement>>();
                        peerList(list);
                    }
                    else if (ob.Type.Equals(typeof(NewPeer))){
                        var np = ob.TryCast<NewPeer>();
                        var c = new TcpClient();
                        c.Connect(np.IP, np.Port);
                        //do zapisania lista części pliku dostępnych u peera
                        newPeer(new Peer(c));
                    }
                    else if (ob.Type.Equals(typeof(PeerAvailable)))
                    {
                        var pa = ob.TryCast<PeerAvailable>();
                        Program.Available = pa.Available;
                    }
                }
                catch (IOException) { }
            }
        }
    }
}
