using MiscUtil.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public Peer(TcpClient client)
        {
            this.client = client;
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
            buffer = new byte[Settings.torrentFileInfo.PiecesLength + 26];
            stream.BeginRead(buffer, 0, Settings.torrentFileInfo.PiecesLength + 26, new AsyncCallback(HandleRead), null); //tutaj oczekuje asynchronicznie na jakieś kawałki pliku
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
                    int length = EndianBitConverter.Big.ToInt32(buffer, 4);
                    int torrentHash = EndianBitConverter.Big.ToInt32(buffer, 8);
                    int type = buffer[9];
                    var idS = buffer.Skip(9).Take(16).ToArray();
                    Guid guid = new Guid(idS);
                    Console.WriteLine(Settings.ID);
                    byte[] b = buffer.Skip(26).Take(length).ToArray();

                    buffer = new byte[Settings.torrentFileInfo.PiecesLength + 26];
                    Console.WriteLine("odczytano: " + id + " l " + length);
                    counter++;
                    Console.WriteLine("bytes: " + bytes);
                    if (type == 1)
                    {
                        if (torrentHash != TorrentFileInfo.TorrentHash)
                        {
                            Console.WriteLine("Niewłaściwy hash pliku");//wysłać info na serwer 
                        }
                        else
                        {
                            var result = TorrentFileInfo.CheckPieceHash(b, id);
                            if (!result)
                                Console.WriteLine("Odebrano błędny fragment");//wysłać info na serwer
                            else
                            {
                                Program.Incoming.Add(new PendingMessage
                                {
                                    PieceIndex = id,
                                    Stream = stream,
                                    EncodedMessage = b,
                                    Type = type
                                });
                                if (Settings.ReadPieces != null && id < Settings.ReadPieces.Length)
                                    Settings.ReadPieces[id] = true;
                            }
                        }
                    }
                    else if(type == 0)
                    {
                        Program.Incoming.Add(new PendingMessage
                        {
                            PieceIndex = id,
                            Stream = stream,
                            Type = type
                        });

                    }
                    else
                    {
                        Console.WriteLine("Niepoprawny typ wiadomosci");
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
                stream.BeginRead(buffer, 0, Settings.torrentFileInfo.PiecesLength + 26, new AsyncCallback(HandleRead), null);
            }
            catch (Exception e)
            {
                //Disconnect();
            }
        }


        public void SendPiece(Piece piece, byte type)
        {
            try
            {
                if (Settings.sendCorrectData)
                {
                    var bytes = EncodePiece(piece, type);
                    //formatter.Serialize(stream, piece);  
                    stream.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    Random rnd = new Random();
                    Byte[] bytes = new Byte[Settings.torrentFileInfo.PiecesLength + 26];
                    rnd.NextBytes(bytes);
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception e)
            {
                // Disconnect();
            }
        }

        public void SendMessage(int index)
        {
            int length = Settings.torrentFileInfo.PiecesLength + 26;
            byte[] message = new byte[length];
            Buffer.BlockCopy(BitConverter.GetBytes(index), 0, message, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(0), 0, message, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(TorrentFileInfo.TorrentHash), 0, message, 8, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(0),0,message,9,1); // 1 - przyslana czesc, 0 - prosba o wyslanie czesci
            Buffer.BlockCopy(Settings.ID.ToByteArray(), 0, message, 10, 16);
            Console.WriteLine(message.Length + "bytes sent");
            stream.Write(message,0,message.Length);
        }

        public static byte[] EncodePiece(Piece piece, byte type)
        {
            try
            {
                int length = piece.data.Length + 26;
                byte[] message = new byte[length];
                Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(piece.index), 0, message, 0, 4);
                Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(piece.length), 0, message, 4, 4);
                Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(TorrentFileInfo.TorrentHash), 0, message, 8, 1);
                message[9] = type; // 1 - przyslana czesc, 0 - prosba o wyslanie czesci
                Buffer.BlockCopy(Settings.ID.ToByteArray(), 0, message, 10, 16);

                //var lengthByte = BitConverter.GetBytes(piece.length);
                //var indexByte = BitConverter.GetBytes(piece.index);
                // Buffer.BlockCopy(indexByte, 0, message, 0, 4);
                //message[4] = type;  // 1 - przyslana czesc, 0 - prosba o wyslanie czesci
                //Buffer.BlockCopy(lengthByte, 0, message, 5, 4);
                Buffer.BlockCopy(piece.data, 0, message, 26, piece.data.Length);
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
