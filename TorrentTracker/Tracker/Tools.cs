using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Web;
using TorrentTracker.Tracker.Data;

namespace TorrentTracker.Tracker
{
    public class Tools
    {
        private Tools() { }

        public static TransportObject Receive(Stream stream, int timeout)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            var tmp = stream.ReadTimeout;
            stream.ReadTimeout = timeout;
            TransportObject ob = null;
            Exception exception = null;
            try { ob = (TransportObject)formatter.Deserialize(stream); } catch (Exception e) { exception = e; }
            stream.ReadTimeout = tmp;
            if (exception != null) throw exception;
            return ob;
        }

        public static TransportObject Receive(Stream stream)
        {
            return Receive(stream, Timeout.Infinite);
        }

        public static void Send(NetworkStream stream, TransportObject ob, int timeout)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            var tmp = stream.WriteTimeout;
            stream.WriteTimeout = timeout;
            Exception exception = null;
            try { formatter.Serialize(stream, ob); } catch (Exception e) { exception = e; }
            stream.WriteTimeout = tmp;
            if (exception != null) throw exception;
        }

        public static void Send(NetworkStream stream, TransportObject ob)
        {
            Send(stream, ob, Timeout.Infinite);
        }
    }
}