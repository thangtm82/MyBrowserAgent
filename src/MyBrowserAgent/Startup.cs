using System.Web.Http;
using MyBrowserAgent.Security;
using Newtonsoft.Json;
using Owin;

namespace MyBrowserAgent
{
    public sealed class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.Filters.Add(new ApiKeyFilter());
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            app.UseWebApi(config);
        }
    }
}
