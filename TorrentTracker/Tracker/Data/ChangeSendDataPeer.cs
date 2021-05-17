using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TorrentTracker.Tracker.Data
{
    [Serializable]
    public class ChangeSendDataPeer
    {
        public bool CorrectSendData { get; set; }

        public ChangeSendDataPeer(bool correctSendData)
        {
            CorrectSendData = correctSendData;
        }
    }
}