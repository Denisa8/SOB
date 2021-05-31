﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentClient
{
 public class Settings
  { 
    public static readonly string peerIp = "127.0.0.1";
    public static int port = 3002;
    public static readonly string trackerIp = "127.0.0.1";
    public static readonly int trackerPort = 60000;
    public static Guid ID = Guid.NewGuid();
    public static bool availablePeer = true;
    public static bool sendCorrectData = true;
    public static bool isStopping = false;
  }
}
