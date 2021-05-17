using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Web;
using TorrentTracker.Tracker.Data;

namespace TorrentTracker.Tracker
{

    public class TrackerListener
    {
        private TcpListener listener;
        private bool stop = false;

        public TrackerListener(TcpListener listener)
        {
            this.listener = listener;
        }

        public void Run(CancellationToken token)
        {
            token.Register(Close);
            while (!stop)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    var stream = client.GetStream();
                    TransportObject ob;
                    try
                    {
                        ob = Tools.Receive(stream, 5000);
                    }
                    catch (Exception) { continue; }
                    if (ob.Type != typeof(InitConnectToTracker)) continue;
                    var init = ob.TryCast<InitConnectToTracker>();
                    Peer peer = new Peer(init.ID, init.IP, init.Port, client, init.Files, init.BannedPeers);
                    var pl = Tracker.Peers;
                    token.ThrowIfCancellationRequested();
                    if (!pl.Keys.Contains(init.ID))
                    {
                        pl.TryAdd(peer.ID, peer);
                        foreach(var p in pl)
                        {
                            if (p.Key != peer.ID)
                                p.Value.InformAboutNewConnectedPeer(peer);
                        }                        
                    }
                    else
                    {
                        pl[init.ID].SetClient(client);
                    }
                    peer.SendPeerList(pl.Values.ToList());
                }
                catch (Exception) {  }
            }
        }

        public void Close()
        {
            stop = true;
            try
            {
                listener.Stop();
            }
            catch (Exception) { }
        }
    }
}