using MiscUtil.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using TorrentClient.Messages;
using TorrentTracker.Tracker;
using TorrentTracker.Tracker.Data;

namespace TorrentClient
{
    public class Peer
    {
        public Guid GUID { get; set; } = Guid.Empty;
        private TcpClient client { get; set; }
        public IPEndPoint EndPoint { get; private set; }
        public NetworkStream stream { get; set; }
        public bool IsConnected;
        public int Port { get { return EndPoint.Port; } }
        /*private*/
        public byte[] buffer;
        private int counter = 0;
        private int counterRead = 0;
        private int errorCounter = 0;  
        
        public List<int> PieceRequestSent { get; set; } = new List<int>(); 
        /*private*/
        public Peer(TcpClient client, Guid guid)
        {
            GUID = guid;
            this.client = client;
        }
        public Peer(Guid guid)
        {
            GUID = guid;
        }

        public void ResetErrorCounter()
        {
            errorCounter = 0;
        }
        public void IncreaseErrorCounter(Guid GUID)
        {
            errorCounter++;
            if(errorCounter >= Settings.maxErrorCount)
            {
                Console.WriteLine("Banning peer: " + GUID);
                Program.bannedPeers.Add(GUID);
                var transportObject = new TransportObject(new BanPeer { BanID = GUID });
                Tools.Send(Program.clientTracker.GetStream(), transportObject);
            }
            else
            {
                Console.WriteLine("Received invalid message from " + GUID + " (" + errorCounter + "/" + Settings.maxErrorCount + ")");
            }
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
            buffer = new byte[Settings.torrentFileInfo.PiecesLength + Program.messageMetadataSize];
            stream.BeginRead(buffer, 0, Settings.torrentFileInfo.PiecesLength + Program.messageMetadataSize, new AsyncCallback(HandleRead), null); //tutaj oczekuje asynchronicznie na jakieś kawałki pliku
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

                    int id = EndianBitConverter.Big.ToInt32(buffer, 0);
                    int length = EndianBitConverter.Big.ToInt32(buffer, 4);
                    int torrentHash = EndianBitConverter.Big.ToInt32(buffer, 8);
                    int type = buffer[12];
                    var idS = buffer.Skip(13).Take(16).ToArray();
                    Guid guid = new Guid(idS);
                    Console.WriteLine(Settings.ID);
                    byte[] b = buffer.Skip(29).ToArray();

                    buffer = new byte[Settings.torrentFileInfo.PiecesLength + Program.messageMetadataSize];
                    Console.WriteLine("odczytano typ: " + type + " id: " + id + " l " + length);
                    counter++;
                    Console.WriteLine("bytes: " + bytes);
                    if (type == 1)
                    {
                        if (torrentHash != TorrentFileInfo.TorrentHash)
                        {
                            Console.WriteLine("Otrzymano hash: " + torrentHash + " ----- " + "Oczekiwany hash: " + TorrentFileInfo.TorrentHash);
                            Console.WriteLine("Niewłaściwy hash pliku.");
                            
                            var peers = Program.Peers.ToList();
                            foreach (KeyValuePair<int, Peer> keyValuePair in peers)
                            {
                                if (keyValuePair.Value.stream == stream)
                                    keyValuePair.Value.IncreaseErrorCounter(keyValuePair.Value.GUID);
                            }
                        }
                        else
                        {
                            Console.WriteLine("PieceIndex = " + id);
                            var result = TorrentFileInfo.CheckPieceHash(b, id);
                            if (!result)
                            {

                                Console.WriteLine("Odebrano błędny fragment.");
                                
                                var peers = Program.Peers.ToList();
                                foreach (KeyValuePair<int, Peer> keyValuePair in peers)
                                {
                                    if (keyValuePair.Value.stream == stream)
                                        keyValuePair.Value.IncreaseErrorCounter(keyValuePair.Value.GUID); 
                                }
                            }
                                
                            else
                            {
                                Program.Incoming.Add(new PendingMessage
                                {
                                    PieceIndex = id,
                                    Stream = stream,
                                    EncodedMessage = b,
                                    Type = type,
                                    Guid = guid
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
                            Type = type,
                            Guid = guid
                        });

                    }
                    else if(type == 2)
                    {
                        Program.Incoming.Add(new PendingMessage
                        {
                            PieceIndex = id,
                            Stream = stream,
                            Type = type,
                            Guid = guid,
                            IndexPeer = torrentHash
                        });
                    }
                    else if(type == 3)
                    {
                        Program.Incoming.Add(new PendingMessage
                        {
                            PieceIndex = id,
                            Stream = stream,
                            Type = type,
                            Guid = guid,
                            IndexPeer = torrentHash
                        });
                    }
                    else
                    {
                        Console.WriteLine("Niepoprawny typ wiadomosci");

                        var peers = Program.Peers.ToList();
                        foreach (KeyValuePair<int, Peer> keyValuePair in peers)
                        {
                            if (keyValuePair.Value.stream == stream)
                                keyValuePair.Value.IncreaseErrorCounter(keyValuePair.Value.GUID);
                        }
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
                stream.BeginRead(buffer, 0, Settings.torrentFileInfo.PiecesLength + Program.messageMetadataSize, new AsyncCallback(HandleRead), null);
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
                    Byte[] bytes = new Byte[Settings.torrentFileInfo.PiecesLength + Program.messageMetadataSize];
                    rnd.NextBytes(bytes);
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception e)
            {
                // Disconnect();
            }
        } 
        public void SendGUIDRequest(int count)
        {
            int length = Settings.torrentFileInfo.PiecesLength + Program.messageMetadataSize;
            byte[] message = new byte[length];
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(0), 0, message, 0, 4);
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(0), 0, message, 4, 4);
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(count), 0, message, 8, 4);
            message[12] = 2; // 1 - przyslana czesc, 0 - prosba o wyslanie czesci, 2 - prosba o wyslanie GUIDu, 3 - przyslanie GUIDu
            Buffer.BlockCopy(Settings.ID.ToByteArray(), 0, message, 13, 16);
            Console.WriteLine(message.Length + "bytes sent");
            try
            {
                stream.Write(message, 0, message.Length);
            }
            catch (Exception e)
            {
                //Disconnect
            }
        }

        public static byte[] EncodeGUIDResponse(int indexPeer)
        {
            int length = Settings.torrentFileInfo.PiecesLength + Program.messageMetadataSize;
            byte[] message = new byte[length];
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(0), 0, message, 0, 4);
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(0), 0, message, 4, 4);
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(indexPeer), 0, message, 8, 4);
            message[12] = 3; // 1 - przyslana czesc, 0 - prosba o wyslanie czesci, 2 - prosba o wyslanie GUIDu, 3 - przyslanie GUIDu
            Buffer.BlockCopy(Settings.ID.ToByteArray(), 0, message, 13, 16);

            return message;
        }

        public void SendMessage(int index)
        {
            int length = Settings.torrentFileInfo.PiecesLength + Program.messageMetadataSize;
            byte[] message = new byte[length];
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(index), 0, message, 0, 4);
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(0), 0, message, 4, 4);
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(TorrentFileInfo.TorrentHash), 0, message, 8, 4);
            Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(0),0,message,12,1); // 1 - przyslana czesc, 0 - prosba o wyslanie czesci, 2 - prosba o wyslanie GUIDu, 3 - przyslanie GUIDu
            Buffer.BlockCopy(Settings.ID.ToByteArray(), 0, message, 13, 16);
            Console.WriteLine(message.Length + "bytes sent");
            try
            {
                stream.Write(message, 0, message.Length);
            }
            catch(Exception e)
            {
                //Disconnect
            }
        }

        public static byte[] EncodePiece(Piece piece, byte type)
        {
            try
            {
                int length = piece.data.Length + Program.messageMetadataSize;
                byte[] message = new byte[length];
                Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(piece.index), 0, message, 0, 4);
                Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(piece.length), 0, message, 4, 4);
                Buffer.BlockCopy(EndianBitConverter.Big.GetBytes(TorrentFileInfo.TorrentHash), 0, message, 8, 4);
                message[12] = type; // 1 - przyslana czesc, 0 - prosba o wyslanie czesci, 2 - prosba o wyslanie GUIDu, 3 - przyslanie GUIDu
                Buffer.BlockCopy(Settings.ID.ToByteArray(), 0, message, 13, 16);
                Guid guid = new Guid(Settings.ID.ToByteArray()); 
                Buffer.BlockCopy(piece.data, 0, message, Program.messageMetadataSize, piece.data.Length);
                return message;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
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

    }
}
