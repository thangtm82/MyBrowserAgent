using System;
using System.Net;
using System.Net.Http;
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
        private readonly AmazonAdsPortfolioService _portfolios = new AmazonAdsPortfolioService();
        private readonly AmazonAdsAutoCampaignService _autoCampaigns = new AmazonAdsAutoCampaignService();
        private readonly AmazonAdsManualProductCampaignService _manualProductCampaigns = new AmazonAdsManualProductCampaignService();

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
            var traceId = Guid.NewGuid().ToString("N");
            try
            {
                var result = _targetBids.UpdateMany(BrowserAgentRuntime.Browser, request, traceId);
                return BulkBidResponse(HttpStatusCode.OK, ApiResult.Ok(result), traceId);
            }
            catch (ArgumentException ex)
            {
                return BulkBidResponse(HttpStatusCode.BadRequest, ApiResult.Fail(ex.Message), traceId);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return BulkBidResponse(HttpStatusCode.BadGateway,
                    ApiResult.Fail("Amazon Ads returned invalid target bid update JSON."), traceId);
            }
            catch (InvalidOperationException ex)
            {
                return BulkBidResponse(HttpStatusCode.BadGateway, ApiResult.Fail(ex.Message), traceId);
            }
            catch (RegexMatchTimeoutException)
            {
                return BulkBidResponse(HttpStatusCode.BadGateway,
                    ApiResult.Fail("Could not parse the Amazon Ads page."), traceId);
            }
            catch (WebDriverException ex)
            {
                return BulkBidResponse(HttpStatusCode.InternalServerError, ApiResult.Fail(ex.Message), traceId);
            }
            catch (System.IO.IOException ex)
            {
                return BulkBidResponse(HttpStatusCode.InternalServerError,
                    ApiResult.Fail("Could not write the bulk bid trace log: " + ex.Message), traceId);
            }
            catch (UnauthorizedAccessException ex)
            {
                return BulkBidResponse(HttpStatusCode.InternalServerError,
                    ApiResult.Fail("Could not write the bulk bid trace log: " + ex.Message), traceId);
            }
        }

        private IHttpActionResult BulkBidResponse(HttpStatusCode status, ApiResult body, string traceId)
        {
            var response = Request.CreateResponse(status, body);
            response.Headers.Add("X-Agent-Trace-Id", traceId);
            return ResponseMessage(response);
        }

        [HttpPost, Route("campaigns/auto")]
        public IHttpActionResult CreateAutoCampaign(AutoCampaignCreateRequest request)
        {
            try
            {
                var result = _autoCampaigns.Create(BrowserAgentRuntime.Browser, request);
                return Ok(ApiResult.Ok(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail(
                    "Could not read the Amazon Ads campaign result. Check for a created campaign before retrying."));
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

        [HttpPost, Route("campaigns/manual-product")]
        public IHttpActionResult CreateManualProductCampaign(ManualProductCampaignCreateRequest request)
        {
            try
            {
                var result = _manualProductCampaigns.Create(BrowserAgentRuntime.Browser, request);
                return Ok(ApiResult.Ok(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail(
                    "Could not read the Amazon Ads campaign result. Check for a created campaign before retrying."));
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

        [HttpPost, Route("portfolios")]
        public IHttpActionResult CreatePortfolio(PortfolioCreateRequest request)
        {
            try
            {
                var id = _portfolios.Create(BrowserAgentRuntime.Browser, request);
                return Ok(ApiResult.Ok(id));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail(
                    "Amazon Ads returned invalid portfolio JSON. Check the account before retrying."));
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

        [HttpPut, Route("portfolios/external-id")]
        public IHttpActionResult UpdatePortfolioAndGetExternalId(PortfolioExternalIdRequest request)
        {
            try
            {
                var externalId = _portfolios.UpdateAndGetExternalId(BrowserAgentRuntime.Browser, request);
                return Ok(ApiResult.Ok(externalId));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail(
                    "Amazon Ads returned invalid portfolio JSON. Check the portfolio before retrying."));
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
