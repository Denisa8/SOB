using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StartMultipleClients
{
    class Program 
    {
        static int ClientNumber { get; set; } = 2; // ile klientow ma zostac uruchomionych
        static int StartingPort { get; set; } = 1301;  // pierwszy port ktory bedzie uzyty
        public static string StartingPath { get; set; } = "C:\\Users\\Admin\\Desktop\\test"; //tutaj wstawiacie sobie folder w ktorym maja byc stworzone osobne katalogi dla klientow. W tym katalogu musi tez byc torrent
        static List<string> Paths { get; set; }
        static string pathToProgram = "C:\\Users\\Admin\\Desktop\\SOB - projekt\\SOB\bin\\Debug\\TorrentClient.exe";

    static void Main(string[] args)
        {
            Run();
        }
        static void Init()
        {
            Paths = new List<string>();
            string tempPath;
            for (int i = 0; i < ClientNumber; i++)
            {
                tempPath = Path.Combine(StartingPath, "Klient " + i);
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);

                if(!Directory.Exists(Path.Combine(tempPath,"Downloads")))
                    Directory.CreateDirectory(Path.Combine(tempPath, "Downloads"));

                Paths.Add(tempPath);
            }
        }

        public static void Run()
        {
            Init();

            for (int i = 0; i < ClientNumber; i++)
            {               
                var proc = System.Diagnostics.Process.Start(pathToProgram, StartingPort + i + " " + StartingPath + " " + Paths[i]);
            }
        }
    }
}
