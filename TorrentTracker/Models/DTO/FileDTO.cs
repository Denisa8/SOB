using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Models.DTO
{
    [Serializable]
    public class FileDTO
    {
        public string Filename { get; set; }
        public int Progress { get; set; }
    }
}