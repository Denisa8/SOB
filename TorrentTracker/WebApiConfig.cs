using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Cors;

namespace TorrentTracker
{
  public static class WebApiConfig
  {
    public static void Register(HttpConfiguration config)
    {
      var cors = new EnableCorsAttribute("http://localhost:4200/", "*", "*");
      config.EnableCors(cors); 

      // Konfiguracja i usługi składnika Web API
      // ustawienie odpowiedzi JSON
      config.Formatters.Clear();
      config.Formatters.Add(new JsonMediaTypeFormatter());
      config.Formatters.JsonFormatter.SerializerSettings =
      new JsonSerializerSettings
      {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
      };
      config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());

      // Trasy składnika Web API
      config.MapHttpAttributeRoutes();

      config.Routes.MapHttpRoute(
          name: "DefaultApi",
          routeTemplate: "{controller}/{action}/{id}",
          defaults: new { id = RouteParameter.Optional }
      );
    }
  }
}
