using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TorrentTracker.Models.DTO;

namespace TorrentTracker.Controllers
{
    public class TrackerController : ApiController
    {
        [HttpGet]
        [ActionName("peer-list")]
        public IEnumerable<PeerDTO> GetPeerList()
        {
            return Tracker.Tracker.GetPeerList();
        }

        [HttpGet]
        [Route("tracker/change-available/{id:guid}/{available:bool}")]
        public IHttpActionResult SetAvailable(Guid id, bool available)
        {
            try
            {
                Tracker.Tracker.GetInstance().ChangeAvailablePeer(id, available);
                return Ok();
            }
            catch (IOException e) { return InternalServerError(e); }
            catch (Exception e) { return BadRequest(e.Message); }
        }

        [HttpGet]
        [Route("tracker/change-send-data/{id:guid}/{correctdata:bool}")]
        public IHttpActionResult SetCorrectSendData(Guid id, bool correctdata)
        {
            try
            {
                Tracker.Tracker.GetInstance().ChangeSendDataPeer(id, correctdata);
                return Ok();
            }
            catch (IOException e) { return InternalServerError(e); }
            catch (Exception e) { return BadRequest(e.Message); }
        }
    }
}
