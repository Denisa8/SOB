using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TorrentTracker.Tracker;
using TorrentTracker.Tracker.Data;

namespace TrackerTest
{
    class Program
    {
        private static Guid ID = Guid.NewGuid();
        private static TcpClient client;
        private static Timer timerTracker;
        private static bool available;
        private static string trackerIp = "127.0.0.1";
        private static int trackerPort = 60000;

        static void Main(string[] args)
        {
            client = new TcpClient();
            Run(client);
            Console.ReadLine();
            client.Close();
        }

        private static async Task Run(TcpClient client)
        {
            await client.ConnectAsync(trackerIp, trackerPort);
            Console.WriteLine("Connected to tracker");
            var f = new Dictionary<string, FileTorrent>();
            var p = new List<int>();
            f.Add("test.txt", new FileTorrent("test.txt", p, 10));
            Tools.Send(client.GetStream(), new TransportObject(new InitConnectToTracker(ID, "127.0.0.1", 1300, f, new List<Guid>())));
            available = true;
            timerTracker = new Timer(async (o)=> await ListenTracker(),null,Timeout.Infinite,Timeout.Infinite);
            timerTracker.Change(0, Timeout.Infinite);
        }

        private static async Task ListenTracker()
        {
            try
            {
                TransportObject ob;
                try
                {
                    ob = Tools.Receive(client.GetStream(), 5000);
                }
                catch (Exception)
                {
                    timerTracker.Change(0, Timeout.Infinite);
                    return;
                }
                Console.Write(ID + " - ");
                if (ob != null)
                {
                    if (ob.Type == typeof(ChangeSendDataPeer))
                    {
                        var csdp = ob.TryCast<ChangeSendDataPeer>();
                        Console.WriteLine("ChangeSendDataPeer: " + csdp.CorrectSendData);
                    }
                    else if (ob.Type == typeof(List<ConnectedPeer>))
                    {
                        var lcp = ob.TryCast<List<ConnectedPeer>>();
                        Console.WriteLine("ListPeer");
                        foreach (var l in lcp)
                        {
                            Console.WriteLine("peer: " + l.ID);
                        }
                    }
                    else if (ob.Type == typeof(ConnectedPeer))
                    {
                        Console.WriteLine("Connected new peer: " + ob.TryCast<ConnectedPeer>().ID);
                    }
                    else if (ob.Type == typeof(CheckAvailablePeer))
                    {
                        Console.WriteLine("Check available");
                        var cap = ob.TryCast<CheckAvailablePeer>();
                        cap.Available = available;
                        try
                        {
                            Tools.Send(client.GetStream(), new TransportObject(cap));
                        }
                        catch (IOException)
                        {
                            if (!client.Connected)
                            {
                                await client.ConnectAsync(trackerIp, trackerPort);
                            }
                        }
                    }
                    else if (ob.Type == typeof(ChangeAvailablePeer))
                    {
                        Console.WriteLine("Change available peer: " + ob.TryCast<ChangeAvailablePeer>().Available);
                    }
                    else if (ob.Type == typeof(ChangeSendDataPeer))
                    {
                        Console.WriteLine("Change send data peer: " + ob.TryCast<ChangeSendDataPeer>().CorrectSendData);
                    }
                }
                timerTracker.Change(0, Timeout.Infinite);
            }
            catch (Exception) { }
        }
    }
}
