using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            Tracker t = new Tracker("127.0.0.1", 60000);
            Console.WriteLine("Press enter to finish");
            Console.ReadKey();
            t.Close();
        }
    }
}
