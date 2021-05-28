using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace TorrentClient
{
  public class Peer
  { 
    private TcpClient client { get; set; }
    public static IPEndPoint EndPoint { get; private set; } 
    private NetworkStream stream { get; set; } 
    public bool IsConnected;
    private static int port { get; set; } 
    private static TcpListener listener { get; set; }
    public int Port { get { return EndPoint.Port; } }
    private byte[] buffer;
    private int counter = 0;
    private int counterRead = 0;
    public static ConcurrentDictionary<int, Peer> Peers { get; } = new ConcurrentDictionary<int, Peer>();
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
    public void EnablePeerConnections(int Port)
    {
      port = Port;
      listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
      listener.Start();
      listener.BeginAcceptTcpClient(new AsyncCallback(HandlePeerConnection), null);
    }

    public void HandlePeerConnection(IAsyncResult ar)
    {
      if (listener == null)
        return;

      TcpClient client = listener.EndAcceptTcpClient(ar);
      listener.BeginAcceptTcpClient(new AsyncCallback(HandlePeerConnection), null);
      var p = ((IPEndPoint)client.Client.RemoteEndPoint).Port;
      Console.WriteLine("Połączenie od:  " + p); //sprawdz czy dobry port
      AddPeer(new Peer(client, torrent),p);
    }
    public void AddPeer(Peer peer, int p)
    {
      Random rand = new Random();
      peer.ConnectToPeer(p);

      if (!Peers.TryAdd(rand.Next(), peer))
        peer.Disconnect();
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
      buffer = new byte[torrent.PiecesLength + 9];
      stream.BeginRead(buffer, 0, torrent.PiecesLength + 9, new AsyncCallback(HandleRead), null); //tutaj oczekuje asynchronicznie na jakieś kawałki pliku

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

          int id = BitConverter.ToInt32(buffer, 0); //tutaj masz, który kawałek Ci przyszedł
          int lenght = BitConverter.ToInt32(buffer, 5);
          byte[] b = buffer.Skip(9).Take(lenght).ToArray();
          //= new byte[lenght]; 
          //lenght = lenght - 1 < 0 ? 0 : (lenght - 1);
          //Buffer.BlockCopy(buffer, 9, b, 0, lenght);
          //var piece = Deserialize(buffer);
          buffer = new byte[torrent.PiecesLength + 9];
          Console.WriteLine("odczytano: " + id + " l " + lenght);
          torrent.WriteFilePiece(id, b); // ten odczytany fragment też mozna do jakiejs struktry zapisać do momentu aż odczytamy
          counter++;
          if(id<torrent.ReadPieces.Length)
          {
            torrent.ReadPieces[id] = true;//np tak oznaczaćm, ze mamy czy cos 
          }
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
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        return null;
      }
    } 
    public static void getType()
        {

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

  }
}
