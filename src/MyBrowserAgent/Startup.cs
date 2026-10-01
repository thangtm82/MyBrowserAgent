using System.Net;
using System.Web.Http;
using MyBrowserAgent.Runtime;
using MyBrowserAgent.Security;
using Newtonsoft.Json;
using Owin;

namespace MyBrowserAgent
{
    public sealed class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            if (!BrowserAgentRuntime.Config.IsInternetMode)
            {
                // HTTP.sys URL prefixes match the Host header. Also verify the peer address.
                app.Use(async (context, next) =>
                {
                    IPAddress address;
                    var remoteIp = context.Request.RemoteIpAddress;
                    if (!IPAddress.TryParse(remoteIp, out address) ||
                        !(IPAddress.IsLoopback(address) ||
                          (address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4()))))
                    {
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("Local mode only accepts loopback connections.");
                        return;
                    }

                    await next();
                });
            }

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
