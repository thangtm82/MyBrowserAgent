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
        private readonly AmazonAdsCampaignService _campaigns = new AmazonAdsCampaignService();

        [HttpPost, Route("campaigns/filter")]
        public IHttpActionResult FilterCampaigns(CampaignFilterRequest request)
        {
            try
            {
                var rows = _campaigns.Filter(BrowserAgentRuntime.Browser, request);
                return Ok(ApiResult.Ok(rows));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail("Amazon Ads returned invalid report JSON."));
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail(ex.Message));
            }
            catch (WebDriverException ex)
            {
                return Content(HttpStatusCode.InternalServerError, ApiResult.Fail(ex.Message));
            }
        }

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
