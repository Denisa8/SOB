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
    public class PeerListener
    {
        private TcpClient client;
        private Peer peer;
        private bool stop;

        public PeerListener(TcpClient client, Peer owner)
        {
            this.client = client;
            peer = owner;
        }

        public void Run(CancellationToken token)
        {
            token.Register(Close);
            while (!stop)
            {
                try
                {
                    var ob = Tools.Receive(client.GetStream());

                    token.ThrowIfCancellationRequested();
                    if (ob.Type == typeof(ReceivePieceFile))
                    {
                        var rpf = ob.TryCast<ReceivePieceFile>();
                        if (!peer.Files[rpf.Filename].Pieces.Contains(rpf.Index))
                        {
                            peer.Files[rpf.Filename].Pieces.Add(rpf.Index);
                            foreach(var p in Tracker.Peers)
                            {
                                if (peer.ID != p.Key)
                                    p.Value.InformPeerAboutReceiveNewPieceFile(peer.ID, rpf.Filename, rpf.Index);
                            }
                        }
                    }
                    else if (ob.Type == typeof(CheckAvailablePeer))
                    {
                        peer.Available = ob.TryCast<CheckAvailablePeer>().Available;
                    }
                    else if (ob.Type == typeof(BanPeer))
                    {
                        var bp = ob.TryCast<BanPeer>();
                        if(!peer.BannedPeers.Contains(bp.BanID))
                            peer.BannedPeers.Add(bp.BanID);
                    }
                    else if (ob.Type == typeof(FileTorrent))
                    {
                        var f = ob.TryCast<FileTorrent>();
                        peer.Files.Add(f.ID, f);
                        foreach (var p in Tracker.Peers)
                        {
                            if (peer.ID != p.Key)
                                p.Value.InformPeerAboutNewFile(peer.ID, f);
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        public void Close()
        {
            stop = true;
            if (client.Connected)
                client.Close();
        }
    }
}