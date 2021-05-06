using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TorrentTracker.Data;

namespace TorrentTracker
{
    public delegate void AddPieceCallback(int index);

    public class PeerListener
    {
        private AddPieceCallback addPieceCallback;
        private TcpClient client;
        private CloseConnectionCallback CloseConnectionCallback;

        public PeerListener(AddPieceCallback callback, TcpClient client,CloseConnectionCallback close)
        {
            this.client = client;
            addPieceCallback = callback;
            CloseConnectionCallback = close;
        }

        public void Listen()
        {
            try
            {
                while (true)
                {
                    var ob = Tools.Receive(client.GetStream());
                    if (ob.Type.Equals(typeof(PieceReceive)))
                        addPieceCallback(ob.TryCast<PieceReceive>().Index);
                }
            }
            catch (SocketException) { }
            if(client.Connected)
                client.Close();
            CloseConnectionCallback();
        }
    }
}
