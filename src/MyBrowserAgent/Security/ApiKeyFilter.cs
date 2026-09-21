using System.Linq;
using System.Net;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using MyBrowserAgent.Models;
using MyBrowserAgent.Runtime;

namespace MyBrowserAgent.Security
{
    public sealed class ApiKeyFilter : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var config = BrowserAgentRuntime.Config;
            if (config == null || string.IsNullOrWhiteSpace(config.ApiKey))
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.ServiceUnavailable,
                    ApiResult.Fail("BrowserAgent API key is not configured."));
                return;
            }

            if (!actionContext.Request.Headers.TryGetValues("X-Api-Key", out var values))
            {
                Reject(actionContext);
                return;
            }

            var supplied = values.FirstOrDefault();
            if (string.IsNullOrEmpty(supplied) ||
                !string.Equals(supplied, config.ApiKey, System.StringComparison.Ordinal))
                Reject(actionContext);
        }

        private static void Reject(HttpActionContext context)
        {
            context.Response = context.Request.CreateResponse(
                HttpStatusCode.Unauthorized,
                ApiResult.Fail("Unauthorized"));
        }
    }
}
