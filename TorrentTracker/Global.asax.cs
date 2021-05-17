using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace TorrentTracker
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            //uruchomienie trackera
            Tracker.Tracker.Init("127.0.0.1",60000);
        }

        public override void Dispose()
        {
            //wy³¹czenie trackera
            Tracker.Tracker.GetInstance().Close();
            base.Dispose();
        }
    }
}
