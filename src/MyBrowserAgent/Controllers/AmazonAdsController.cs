using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Http;
using MyBrowserAgent.Models;
using MyBrowserAgent.Runtime;
using MyBrowserAgent.Services;
using OpenQA.Selenium;

namespace MyBrowserAgent.Controllers
{
    [RoutePrefix("api/amazon-ads")]
    public sealed class AmazonAdsController : ApiController
    {
        private readonly AmazonAdsAccountInfoService _service = new AmazonAdsAccountInfoService();

        [HttpPost, Route("account-info")]
        public IHttpActionResult AccountInfo()
        {
            try
            {
                var info = _service.GetAccountInfo(BrowserAgentRuntime.Browser);
                return Ok(ApiResult.Ok(info));
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail(ex.Message));
            }
            catch (RegexMatchTimeoutException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail("Could not parse the Amazon Ads page."));
            }
            catch (WebDriverException ex)
            {
                return Content(HttpStatusCode.InternalServerError, ApiResult.Fail(ex.Message));
            }
        }
    }
}
