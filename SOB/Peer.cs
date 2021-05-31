using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TorrentClient.Messages;

namespace TorrentClient
{
  public class Peer
  {
    private TcpClient client { get; set; }
    public IPEndPoint EndPoint { get; private set; }
    private NetworkStream stream { get; set; }
    public bool IsConnected; 
    public int Port { get { return EndPoint.Port; } }
    /*private*/
    public byte[] buffer;
    private int counter = 0;
    private int counterRead = 0;
    /*private*/
    public TorrentFileInfo torrent { get; set; } 
    public List<PendingMessage> Incoming { get; set; } = new List<PendingMessage>();
    public List<PendingMessage> Outgoing { get; set; } = new List<PendingMessage>(); // W tych dwoch listach sa wszystkie wiadomosci ktore trzeba przetworzyc

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
      Settings.isStopping = false;
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
          Incoming.Add(new PendingMessage(stream, buffer));
          buffer = new byte[torrent.PiecesLength + 9];


          /* // Tak bylo
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
          if (id < torrent.ReadPieces.Length)
          {
              torrent.ReadPieces[id] = true;//np tak oznaczaćm, ze mamy czy cos 
          }*/
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

    public void SendMessage(byte[] message)
    {
      stream.Write(message, 0, message.Length);
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
        message[4] = 1;  // 1 - przyslana czesc, 0 - prosba o wyslanie czesci
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
