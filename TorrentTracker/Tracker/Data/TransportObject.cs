using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class TransportObject
    {
        public Type Type { get; private set; }
        public Object Object { get; private set; }

        public TransportObject(Object ob)
        {
            Object = ob;
            Type = ob.GetType();
        }

        public T TryCast<T>()
        {
            if (!Type.Equals(typeof(T)))
                throw new InvalidCastException("Nie udało się przekonwertować obiektu, ponieważ typ obiektu jest różny od oczekiwanego typu.");
            return (T)Object;
        }

        public void SendObject(NetworkStream stream)
        {
            BinaryFormatter binary = new BinaryFormatter();
            try
            {
                binary.Serialize(stream, this);
            }
            catch (Exception e)
            {
                Console.WriteLine("Nie udało się wysłać obiektu." + Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace);
            }
        }
    }
}