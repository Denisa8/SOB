using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TorrentTracker.Data;

namespace TorrentTracker
{
    public delegate void PortClientCallback(TcpClient client,int port,List<int> receivePieces,Guid guid);

    public class TrackerListener
    {
        private TcpListener listener;
        private PortClientCallback PortClientCallback;
        public bool stop = false;

        public TrackerListener(TcpListener listener, PortClientCallback callback)
        {
            this.listener = listener;
            PortClientCallback = callback;
        }

        public void Listen()
        {
            while (!stop)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    var stream = client.GetStream();
                    var ob = Tools.Receive(stream).TryCast<PortToConnectToPeer>();
                    PortClientCallback(client, ob.Port,ob.Pieces,ob.Guid);
                }
                catch (SocketException) { }
                catch (InvalidCastException e) {
                    Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
                }
            }
        }        
    }
}
