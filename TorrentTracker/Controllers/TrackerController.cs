using System;
using System.Collections.Generic;
using System.IO; 
using System.Web.Http;
using System.Web.Http.Cors;
using TorrentTracker.Models.DTO;

namespace TorrentTracker.Controllers
{
  [EnableCors(origins: "*", headers: "*", methods: "*")]
  public class TrackerController : ApiController
    {
        [HttpGet]
        [ActionName("peer-list")]
        public IEnumerable<PeerDTO> GetPeerList()
        {
            return Tracker.Tracker.GetPeerList();
        }

        [HttpGet]
        [Route("tracker/change-available/{id}/{available}")]
        public IHttpActionResult SetAvailable([FromUri]string id, [FromUri] bool available)
        {
            try
            {
                Guid g =new Guid(id);
                Tracker.Tracker.GetInstance().ChangeAvailablePeer(g, available);
                return Ok();
            }
            catch (IOException e) { return InternalServerError(e); }
            catch (Exception e) { return BadRequest(e.Message); }
        }

        [HttpGet]
        [Route("tracker/change-send-data/{id}/{correctdata}")]
        public IHttpActionResult SetCorrectSendData([FromUri] string id, [FromUri] bool correctdata)
        {
            try
            {
                Guid g = new Guid(id); 
                Tracker.Tracker.GetInstance().ChangeSendDataPeer(g, correctdata);
                return Ok();
            }
            catch (IOException e) { return InternalServerError(e); }
            catch (Exception e) { return BadRequest(e.Message); }
        }
    }
}
