using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using MyBrowserAgent.Models;
using MyBrowserAgent.Runtime;

namespace MyBrowserAgent.Controllers
{
    [RoutePrefix("api/browser")]
    public sealed class BrowserController : ApiController
    {
        [HttpGet, Route("status")]
        public IHttpActionResult Status() => Run(() => BrowserAgentRuntime.Browser.GetStatus());

        [HttpPost, Route("start")]
        public IHttpActionResult Start() => Run(() =>
        {
            BrowserAgentRuntime.Browser.Start();
            return BrowserAgentRuntime.Browser.GetStatus();
        });

        [HttpPost, Route("stop")]
        public IHttpActionResult Stop() => Run(() =>
        {
            BrowserAgentRuntime.Browser.Stop();
            return new { running = false };
        });

        [HttpPost, Route("open")]
        public IHttpActionResult Open(OpenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url)) return BadRequest("Url is required.");
            return Run(() =>
            {
                BrowserAgentRuntime.Browser.Open(request.Url);
                return BrowserAgentRuntime.Browser.GetStatus();
            });
        }

        [HttpPost, Route("click")]
        public IHttpActionResult Click(ElementRequest request)
        {
            if (!ValidElement(request, out var error)) return error;
            return Run(() => { BrowserAgentRuntime.Browser.Click(request.Selector, request.SelectorType); return null; });
        }

        [HttpPost, Route("fill")]
        public IHttpActionResult Fill(FillRequest request)
        {
            if (!ValidElement(request, out var error)) return error;
            return Run(() => { BrowserAgentRuntime.Browser.Fill(request.Selector, request.Value, request.SelectorType); return null; });
        }

        [HttpPost, Route("sendkeys")]
        public IHttpActionResult SendKeys(FillRequest request)
        {
            if (!ValidElement(request, out var error)) return error;
            return Run(() => { BrowserAgentRuntime.Browser.SendKeys(request.Selector, request.Value, request.SelectorType); return null; });
        }

        [HttpPost, Route("text")]
        public IHttpActionResult Text(ElementRequest request)
        {
            if (!ValidElement(request, out var error)) return error;
            return Run(() => BrowserAgentRuntime.Browser.GetText(request.Selector, request.SelectorType));
        }

        [HttpPost, Route("attribute")]
        public IHttpActionResult Attribute(AttributeRequest request)
        {
            if (!ValidElement(request, out var error)) return error;
            if (string.IsNullOrWhiteSpace(request.Attribute)) return BadRequest("Attribute is required.");
            return Run(() => BrowserAgentRuntime.Browser.GetAttribute(request.Selector, request.Attribute, request.SelectorType));
        }

        [HttpGet, Route("html")]
        public IHttpActionResult Html() => Run(() => BrowserAgentRuntime.Browser.GetHtml());

        [HttpPost, Route("javascript")]
        public IHttpActionResult JavaScript(JavaScriptRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Script)) return BadRequest("Script is required.");
            return Run(() => BrowserAgentRuntime.Browser.ExecuteJavaScript(request.Script));
        }

        [HttpGet, Route("cookies")]
        public IHttpActionResult Cookies(bool includeValues = false) => Run(() => BrowserAgentRuntime.Browser.GetCookies(includeValues));

        [HttpPost, Route("cookies/clear")]
        public IHttpActionResult ClearCookies() => Run(() => { BrowserAgentRuntime.Browser.DeleteAllCookies(); return null; });

        [HttpGet, Route("windows")]
        public IHttpActionResult Windows() => Run(() => BrowserAgentRuntime.Browser.GetWindows());

        [HttpPost, Route("window/switch")]
        public IHttpActionResult SwitchWindow(WindowRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Handle)) return BadRequest("Handle is required.");
            return Run(() => { BrowserAgentRuntime.Browser.SwitchWindow(request.Handle); return null; });
        }

        [HttpPost, Route("tab/new")]
        public IHttpActionResult NewTab(NewTabRequest request) => Run(() => { BrowserAgentRuntime.Browser.NewTab(request?.Url); return null; });

        [HttpPost, Route("tab/close")]
        public IHttpActionResult CloseTab() => Run(() => { BrowserAgentRuntime.Browser.CloseCurrentTab(); return null; });

        [HttpPost, Route("refresh")]
        public IHttpActionResult Refresh() => Run(() => { BrowserAgentRuntime.Browser.Refresh(); return null; });

        [HttpPost, Route("back")]
        public IHttpActionResult Back() => Run(() => { BrowserAgentRuntime.Browser.Back(); return null; });

        [HttpPost, Route("forward")]
        public IHttpActionResult Forward() => Run(() => { BrowserAgentRuntime.Browser.Forward(); return null; });

        [HttpPost, Route("wait")]
        public IHttpActionResult Wait(WaitRequest request)
        {
            if (!ValidElement(request, out var error)) return error;
            return Run(() => new { found = BrowserAgentRuntime.Browser.WaitForElement(request.Selector, request.TimeoutSeconds, request.SelectorType) });
        }

        [HttpGet, Route("screenshot")]
        public HttpResponseMessage Screenshot()
        {
            try
            {
                var bytes = BrowserAgentRuntime.Browser.Screenshot();
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(bytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline") { FileName = "screenshot.png" };
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ApiResult.Fail(ex.Message));
            }
        }

        private IHttpActionResult Run(Func<object> action)
        {
            try { return Ok(ApiResult.Ok(action())); }
            catch (Exception ex) { return Content(HttpStatusCode.InternalServerError, ApiResult.Fail(ex.Message)); }
        }

        private bool ValidElement(ElementRequest request, out IHttpActionResult error)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Selector))
            {
                error = BadRequest("Selector is required.");
                return false;
            }
            error = null;
            return true;
        }
    }
}
