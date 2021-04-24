using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading; 
using MiscUtil.Conversion;

namespace TorrentClient
{
    public class Peer
    {

        private TcpClient client { get; set; }
        public static IPEndPoint EndPoint { get; private set; }
        private static TcpListener tcpListener { get; set; }
        private NetworkStream stream { get; set; }
        private static Socket socket { get; set; }
        private static Thread sendThread { get; set; }
        private static Thread receiveThread { get; set; }
        public bool IsConnected;
        private int port;
        private byte[] buffer;
        private int counter = 0;
        private int counterRead = 0;
        private TorrentFileInfo torrent { get; set; }
        private bool isStopping { get; set; }
        public Peer(TcpClient client, TorrentFileInfo torrent)
        {
            this.client = client;
            this.torrent = torrent;
        }
        public Peer(TcpClient client)
        {
            this.client = client;
        }
        public Peer(TorrentFileInfo torrent)
        {
            this.torrent = torrent;
        }
        public Peer()
        {
        }

        public void ConnectToPeer(int portPeer)
        {
            IPAddress hostadd = IPAddress.Parse("127.0.0.1");
            EndPoint = new IPEndPoint(hostadd, portPeer);
            if (client == null)
            {
                client = new TcpClient();
                try
                {
                    client.Connect(EndPoint);
                    IsConnected = true;
                }
                catch (Exception e)
                {
                    Disconnect();
                    return;
                }
            }

            stream = client.GetStream();
            isStopping = false;
            buffer = new byte[torrent.PiecesLength+ 9];
            stream.BeginRead(buffer, 0, torrent.PiecesLength + 9, new AsyncCallback(HandleRead), null);

        }
        private void HandleRead(IAsyncResult ar)
        { 
            int bytes = 0;
            try
            {
                bytes = stream.EndRead(ar);
                if (bytes > 0)
                {
                    Console.WriteLine("bytes: " + bytes);

                    int id = BitConverter.ToInt32(buffer, 0);
                    int lenght = BitConverter.ToInt32(buffer, 5);
                    byte[] b = buffer.Skip(9).Take(lenght).ToArray();
                        //= new byte[lenght]; 
                    //lenght = lenght - 1 < 0 ? 0 : (lenght - 1);
                    //Buffer.BlockCopy(buffer, 9, b, 0, lenght);
                    //var piece = Deserialize(buffer);
                    buffer = new byte[torrent.PiecesLength + 9];
                    Console.WriteLine("odczytano: " + id +" l " + lenght);
                    torrent.WriteFilePiece(id, b);
                    counter++;
                }
                //stream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //Disconnect();
                return;
            }

            try
            {
                stream = client.GetStream();
                stream.BeginRead(buffer, 0, torrent.PiecesLength + 9, new AsyncCallback(HandleRead), null);
            }
            catch (Exception e)
            {
                //Disconnect();
            }
        }
        public void Serve(int portPeer)
        {
            IPAddress hostadd = IPAddress.Parse("127.0.0.1");
            EndPoint = new IPEndPoint(hostadd, portPeer);
            if (client == null)
            {
                client = new TcpClient();
                try
                {
                    client.Connect(EndPoint);
                    IsConnected = true;
                }
                catch (Exception e)
                {
                    Disconnect();
                    return;
                }
            }
        }

        public static Piece ReadPacket(NetworkStream ns)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            ns.Seek(0, SeekOrigin.Begin);

            Piece np = (Piece)formatter.Deserialize(ns);
            return np;
        }
        public void SendPiece(Piece piece)
        {
            try
            { 
                var bytes = EncodePiece(piece);
                //formatter.Serialize(stream, piece);  
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
               // Disconnect();
            }
        }
        public static byte[] EncodePiece(Piece piece)
        {
            try
            {
                int length = piece.data.Length + 9;
                var lengthByte = BitConverter.GetBytes(piece.length);
                var indexByte = BitConverter.GetBytes(piece.index);
                byte[] message = new byte[length];
                Buffer.BlockCopy(indexByte, 0, message, 0, 4);
                Buffer.BlockCopy(lengthByte, 0, message, 5, 4);
                Buffer.BlockCopy(piece.data, 0, message, 9, piece.data.Length);
                return message;
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static byte[] SerializeToByteArray( Piece obj)
        {
            if (obj == null)
            {
                return null;
            }
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static Piece Deserialize (byte[] byteArray) 
        {
            if (byteArray == null)
            {
                return null;
            }
            using (var memStream = new MemoryStream(byteArray))
            {
                var binForm = new BinaryFormatter(); 
                var obj = (Piece)binForm.Deserialize(memStream);
                return obj;
            }
        }
     
        private void ReceiveFile()
        {
            TcpListener server = null;
            try
            {
                Int32 port = 13000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);
                server.Start();

                Byte[] bytes = new Byte[256];
                String data = null;

                while (true)
                {
                    Console.Write("Waiting for a connection... ");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    data = null;
                    NetworkStream stream = client.GetStream();
                    int i;
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);
                        data = data.ToUpper();

                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent: {0}", data);
                    }
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                server.Stop();
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                IsConnected = false;
            }
            if (client != null)
                client.Close();
        }
        public void SendPieces(byte[] bytes)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                stream.Write(bytes, 0, bytes.Length);

            }
            catch (Exception e)
            {
                Disconnect();
            }
        }

    }
}
