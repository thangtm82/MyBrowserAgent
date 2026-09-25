using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using MyBrowserAgent.Models;
using MyBrowserAgent.Services;

namespace MyBrowserAgent.Controllers
{
    [RoutePrefix("api/amazon-ads")]
    public sealed class AmazonAdsController : ApiController
    {
        private readonly AmazonAdsAccountInfoService _service = new AmazonAdsAccountInfoService();

        [HttpPost, Route("account-info")]
        public async Task<IHttpActionResult> AccountInfo(AmazonAdsAccountInfoRequest request)
        {
            if (request == null) return BadRequest("Request body is required.");
            if (string.IsNullOrWhiteSpace(request.Cookies)) return BadRequest("Cookies are required.");

            Uri url;
            try { url = AmazonAdsAccountInfoService.BuildUrl(request.EndpointUrl, request.EntityId); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }

            try
            {
                var info = await _service.FetchAsync(url, request.Cookies);
                return Ok(ApiResult.Ok(info));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (TaskCanceledException) { return Content(HttpStatusCode.GatewayTimeout, ApiResult.Fail("Amazon Ads request timed out.")); }
            catch (HttpRequestException ex) { return Content(HttpStatusCode.BadGateway, ApiResult.Fail(ex.Message)); }
            catch (InvalidOperationException ex) { return Content(HttpStatusCode.BadGateway, ApiResult.Fail(ex.Message)); }
            catch (RegexMatchTimeoutException) { return Content(HttpStatusCode.BadGateway, ApiResult.Fail("Could not parse the Amazon Ads page.")); }
        }
    }
}
