using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TorrentTracker.Data;

namespace TorrentTracker
{
    public delegate void RemovePeerCallBack(PeerStatus status);
    public delegate void CheckAvailableCallback(PeerStatus status, bool available);
    public class Tracker
    {
        private List<PeerStatus> peers = new List<PeerStatus>();
        private TcpListener listener;
        private Thread threadListener;
        private Thread threadCheck;
        private TrackerListener l;

        public Tracker(string ip,int port)
        {
            listener = new TcpListener(IPAddress.Parse(ip), port);
            listener.Start();
            Console.WriteLine("Tracker start listening: " + ip + ":" + port);
            var l = new TrackerListener(listener, AddPeer);
            this.l = l;
            threadListener = new Thread(new ThreadStart(l.Listen));
            threadListener.Start();
            threadCheck = new Thread(new ThreadStart(CheckPeers));
            threadCheck.Start();
        }

        private void CheckAvailable(PeerStatus status, bool available)
        {
            Console.WriteLine("Peer " + status.ID + " is " + (available ? "available." : "not available."));
            /*if (!available)
            {
                status.Close();
                peers.Remove(status);
            }*/
        }

        private void CheckPeers()
        {
            while (true)
            {
                for(int i = 0; i < peers.Count; i++)
                {
                    try
                    {
                        peers[i].CheckAvailable(CheckAvailable);
                    }
                    catch (Exception) { }
                }
                Thread.Sleep(5000);
            }
        }

        private void AddPeer(TcpClient client, int port,List<int> pieces,Guid guid)
        {
            PeerStatus status = new PeerStatus(client, port, Remove, pieces,guid);
            status.SendPeerList(peers);
            foreach (var peer in peers)
                peer.InformAboutNewPeer(status);
            if(peers.Count(x=>x.ID == guid)==1)
            {
                var p = peers.First(x => x.ID == guid);
                p.Close();
                peers.Remove(p);
            }
            peers.Add(status);
        }

        private void Remove(PeerStatus status)
        {
            peers.Remove(status);
        }

        public void Close()
        {
            threadCheck.Abort();
            l.stop = true;
            listener.Stop();
            threadListener.Join();
            Console.WriteLine("Tracker is turned off.");
        }
    }
}
