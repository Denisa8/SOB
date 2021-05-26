using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Models.DTO
{
    public class PeerDTO
    {
        public string ID { get; set; }
        public bool Available { get; set; }
        public bool CorrectSendData { get; set; }
        public List<Guid> BannedPeers { get; set; }
        public List<FileDTO> Files { get; set; }
    }
}