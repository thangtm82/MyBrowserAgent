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
        private readonly AmazonAdsTargetService _targets = new AmazonAdsTargetService();
        private readonly AmazonAdsTargetBidService _targetBids = new AmazonAdsTargetBidService();

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

        [HttpPost, Route("targets/filter")]
        public IHttpActionResult FilterTargets(TargetFilterRequest request)
        {
            try
            {
                var rows = _targets.Filter(BrowserAgentRuntime.Browser, request);
                return Ok(ApiResult.Ok(rows));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail("Amazon Ads returned invalid target report JSON."));
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

        [HttpPut, Route("targets/bid")]
        public IHttpActionResult UpdateTargetBid(TargetBidUpdateRequest request)
        {
            try
            {
                var result = _targetBids.Update(BrowserAgentRuntime.Browser, request);
                return Ok(ApiResult.Ok(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail("Amazon Ads returned invalid target bid update JSON."));
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

        [HttpPut, Route("targets/bids")]
        public IHttpActionResult UpdateTargetBids(TargetBidBulkUpdateRequest request)
        {
            try
            {
                var result = _targetBids.UpdateMany(BrowserAgentRuntime.Browser, request);
                return Ok(ApiResult.Ok(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail("Amazon Ads returned invalid target bid update JSON."));
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

        [HttpPost, Route("start-session")]
        public IHttpActionResult StartSession()
        {
            try
            {
                var info = _service.StartSession(BrowserAgentRuntime.Browser);
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
            catch (System.IO.IOException ex)
            {
                return Content(HttpStatusCode.InternalServerError, ApiResult.Fail(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
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
