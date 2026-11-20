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
        private readonly AmazonAdsManualKeywordCampaignService _manualKeywordCampaigns = new AmazonAdsManualKeywordCampaignService();
        private readonly AmazonAdsKeywordRecommendationService _keywordRecommendations = new AmazonAdsKeywordRecommendationService();

        [HttpPost, Route("campaigns/filter")]
        public IHttpActionResult FilterCampaigns(CampaignFilterRequest request, [FromUri] string market = null)
        {
            try
            {
                var rows = _campaigns.Filter(BrowserAgentRuntime.Browser, request, ResolveMarket(market));
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
        public IHttpActionResult FilterTargets(TargetFilterRequest request, [FromUri] string market = null)
        {
            try
            {
                var rows = _targets.Filter(BrowserAgentRuntime.Browser, request, ResolveMarket(market, request?.Market));
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
        public IHttpActionResult UpdateTargetBid(TargetBidUpdateRequest request, [FromUri] string market = null)
        {
            try
            {
                var result = _targetBids.Update(BrowserAgentRuntime.Browser, request, ResolveMarket(market));
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
        public IHttpActionResult UpdateTargetBids(TargetBidBulkUpdateRequest request, [FromUri] string market = null)
        {
            var traceId = Guid.NewGuid().ToString("N");
            try
            {
                var result = _targetBids.UpdateMany(BrowserAgentRuntime.Browser, request, traceId, ResolveMarket(market));
                return TraceResponse(HttpStatusCode.OK, ApiResult.Ok(result), traceId);
            }
            catch (ArgumentException ex)
            {
                return TraceResponse(HttpStatusCode.BadRequest, ApiResult.Fail(ex.Message), traceId);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return TraceResponse(HttpStatusCode.BadGateway,
                    ApiResult.Fail("Amazon Ads returned invalid target bid update JSON."), traceId);
            }
            catch (InvalidOperationException ex)
            {
                return TraceResponse(HttpStatusCode.BadGateway, ApiResult.Fail(ex.Message), traceId);
            }
            catch (RegexMatchTimeoutException)
            {
                return TraceResponse(HttpStatusCode.BadGateway,
                    ApiResult.Fail("Could not parse the Amazon Ads page."), traceId);
            }
            catch (WebDriverException ex)
            {
                return TraceResponse(HttpStatusCode.InternalServerError, ApiResult.Fail(ex.Message), traceId);
            }
            catch (System.IO.IOException ex)
            {
                return TraceResponse(HttpStatusCode.InternalServerError,
                    ApiResult.Fail("Could not write the bulk bid trace log: " + ex.Message), traceId);
            }
            catch (UnauthorizedAccessException ex)
            {
                return TraceResponse(HttpStatusCode.InternalServerError,
                    ApiResult.Fail("Could not write the bulk bid trace log: " + ex.Message), traceId);
            }
        }

        private IHttpActionResult TraceResponse<T>(HttpStatusCode status, T body, string traceId)
        {
            var response = Request.CreateResponse(status, body);
            response.Headers.Add("X-Agent-Trace-Id", traceId);
            return ResponseMessage(response);
        }

        [HttpPost, Route("campaigns/auto")]
        public IHttpActionResult CreateAutoCampaign(AutoCampaignCreateRequest request, [FromUri] string market = null)
        {
            return SubmitCampaign("campaigns/auto", ResolveMarket(market, request?.Market), request?.AccountInfo, selectedMarket =>
            {
                var result = _autoCampaigns.Create(BrowserAgentRuntime.Browser, request, selectedMarket);
                return (result, result.Succeeded, result.StatusCode, result.Content);
            });
        }

        [HttpPost, Route("campaigns/manual-product")]
        public IHttpActionResult CreateManualProductCampaign(ManualProductCampaignCreateRequest request, [FromUri] string market = null)
        {
            return SubmitCampaign("campaigns/manual-product", ResolveMarket(market), request?.AccountInfo, selectedMarket =>
            {
                var result = _manualProductCampaigns.Create(BrowserAgentRuntime.Browser, request, selectedMarket);
                return (result, result.Succeeded, result.StatusCode, result.Content);
            });
        }

        [HttpPost, Route("targets/keywords/recommendations")]
        public IHttpActionResult GetKeywordRecommendations(KeywordRecommendationRequest request, [FromUri] string market = null)
        {
            try
            {
                var results = _keywordRecommendations.GetRecommendations(BrowserAgentRuntime.Browser, request, ResolveMarket(market));
                return Ok(ApiResult.Ok(results));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return Content(HttpStatusCode.BadGateway, ApiResult.Fail(
                    "Amazon Ads returned invalid keyword recommendation JSON."));
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

        [HttpPost, Route("campaigns/manual-keyword")]
        public IHttpActionResult CreateManualKeywordCampaign(ManualKeywordCampaignCreateRequest request, [FromUri] string market = null)
        {
            return SubmitCampaign("campaigns/manual-keyword", ResolveMarket(market), request?.AccountInfo, selectedMarket =>
            {
                var result = _manualKeywordCampaigns.Create(BrowserAgentRuntime.Browser, request, selectedMarket);
                return (result, result.Succeeded, result.StatusCode, result.Content);
            });
        }

        [HttpPost, Route("portfolios")]
        public IHttpActionResult CreatePortfolio(PortfolioCreateRequest request, [FromUri] string market = null)
        {
            try
            {
                var id = _portfolios.Create(BrowserAgentRuntime.Browser, request, ResolveMarket(market));
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
        public IHttpActionResult UpdatePortfolioAndGetExternalId(PortfolioExternalIdRequest request, [FromUri] string market = null)
        {
            try
            {
                var externalId = _portfolios.UpdateAndGetExternalId(BrowserAgentRuntime.Browser, request, ResolveMarket(market));
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
        public IHttpActionResult StartSession([FromUri] string market = "US")
        {
            try
            {
                var info = _service.StartSession(BrowserAgentRuntime.Browser, market);
                return Ok(ApiResult.Ok(info));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
        public IHttpActionResult AccountInfo([FromUri] string market = "US")
        {
            try
            {
                var info = _service.GetAccountInfo(BrowserAgentRuntime.Browser, market);
                return Ok(ApiResult.Ok(info));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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

        private IHttpActionResult SubmitCampaign(string endpoint, string market, AmazonAdsAccountInfo account,
            Func<string, (object Result, bool Succeeded, int StatusCode, string Content)> submit)
        {
            var traceId = Guid.NewGuid().ToString("N");
            try
            {
                var result = submit(market);
                if (!result.Succeeded)
                    CampaignTrace.Write(traceId, endpoint, market, account, result.StatusCode, result.Content);
                return TraceResponse(HttpStatusCode.OK, ApiResult.Ok(result.Result), traceId);
            }
            catch (ArgumentException ex)
            {
                CampaignTrace.Write(traceId, endpoint, market, account, exception: ex);
                return TraceResponse(HttpStatusCode.BadRequest, new HttpError(ex.Message), traceId);
            }
            catch (Exception ex) when (ex is Newtonsoft.Json.JsonException ||
                ex is InvalidOperationException || ex is RegexMatchTimeoutException || ex is WebDriverException)
            {
                CampaignTrace.Write(traceId, endpoint, market, account, exception: ex);
                var status = ex is WebDriverException ? HttpStatusCode.InternalServerError : HttpStatusCode.BadGateway;
                var message = ex.Message;
                if (ex is Newtonsoft.Json.JsonException)
                    message = "Could not read the Amazon Ads campaign result. Check for a created campaign before retrying.";
                else if (ex is RegexMatchTimeoutException)
                    message = "Could not parse the Amazon Ads page.";
                return TraceResponse(status, ApiResult.Fail(message), traceId);
            }
            catch (Exception ex)
            {
                CampaignTrace.Write(traceId, endpoint, market, account, exception: ex);
                var response = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                response.Headers.Add("X-Agent-Trace-Id", traceId);
                return ResponseMessage(response);
            }
        }

        private static string ResolveMarket(string queryMarket, string bodyMarket = null)
        {
            return queryMarket ?? bodyMarket ?? "US";
        }

    }
}
