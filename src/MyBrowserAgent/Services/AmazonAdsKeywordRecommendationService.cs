using System;
using System.Collections.Generic;
using System.Threading;
using MyBrowserAgent.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;

namespace MyBrowserAgent.Services
{
    public sealed class AmazonAdsKeywordRecommendationService
    {

        private const string FetchScript = @"
            var done = arguments[arguments.length - 1];
            var headers = arguments[0];
            var body = arguments[1];
            var entityId = arguments[2];
            var controller = new AbortController();
            var timer = setTimeout(function () { controller.abort(); }, 45000);
            fetch('/a9g-api-gateway/sp/targets/keywords/recommendations', {
                method: 'POST',
                credentials: 'same-origin',
                headers: headers,
                body: body,
                referrer: location.origin + '/cb/sp?entityId=' + encodeURIComponent(entityId),
                signal: controller.signal
            }).then(function (response) {
                return response.text().then(function (text) {
                    clearTimeout(timer);
                    done(JSON.stringify({ status: response.status, ok: response.ok, body: text }));
                });
            }).catch(function (error) {
                clearTimeout(timer);
                done(JSON.stringify({ error: String(error) }));
            });";

        public IList<KeywordTarget> GetRecommendations(
            BrowserService browser, KeywordRecommendationRequest request, string market = "US")
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));
            Validate(request);
            var targets = new List<Target>();
            foreach (var keyword in request.Keywords)
                foreach (var matchType in request.MatchTypes)
                    targets.Add(new Target { Keyword = keyword, MatchType = matchType });

            var payload = new RecommendationRequest
            {
                Asins = new List<string> { request.Asin },
                targets = targets
            };
            // Preserve the property casing used by the supplied model.
            var body = JsonConvert.SerializeObject(payload, Formatting.None);
            return browser.RunInTemporaryTab(AmazonAdsMarket.GetCampaignUrl(market),
                driver => FetchInBrowser(driver, request.AccountInfo, body));
        }

        private static IList<KeywordTarget> FetchInBrowser(
            IWebDriver driver, AmazonAdsAccountInfo expected, string body)
        {
            ConfirmActiveAccount(driver, expected);
            var headers = new Dictionary<string, string>
            {
                ["accept"] = "application/json",
                ["accept-language"] = "en-US,en;q=0.9,vi;q=0.8",
                ["amazon-advertising-api-advertiserid"] = expected.EntityId,
                ["amazon-advertising-api-clientid"] = expected.ClientId,
                ["amazon-advertising-api-csrf-data"] = expected.ClientId,
                ["amazon-advertising-api-csrf-token"] = expected.CsrfToken,
                ["amazon-advertising-api-isimpersonator"] = "false",
                ["amazon-advertising-api-marketplaceid"] = expected.MarketplaceId,
                ["content-type"] = "application/vnd.spkeywordsrecommendation.v5+json",
                ["prefer"] = "return=representation"
            };
            if (!string.IsNullOrEmpty(expected.TraceId) && !string.IsNullOrEmpty(expected.SegmentId))
                headers["x-amzn-trace-id"] = "Root=" + expected.TraceId +
                    ";Parent=" + expected.SegmentId;

            var timeouts = driver.Manage().Timeouts();
            var originalTimeout = timeouts.AsynchronousJavaScript;
            try
            {
                timeouts.AsynchronousJavaScript = TimeSpan.FromSeconds(60);
                var raw = ((IJavaScriptExecutor)driver).ExecuteAsyncScript(
                    FetchScript, headers, body, expected.EntityId) as string;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("Amazon Ads returned no keyword recommendation result.");

                var result = JObject.Parse(raw);
                var status = result.Value<int?>("status");
                if (result.Value<bool?>("ok") != true || status != 200)
                    throw new InvalidOperationException("Amazon Ads keyword recommendation request failed (HTTP " +
                        (status?.ToString() ?? "unavailable") + "). Check login and account access.");

                var response = JObject.Parse(result.Value<string>("body") ?? "");
                var rows = response["keywordTargetList"] as JArray ??
                    throw new InvalidOperationException(
                        "Amazon Ads keyword recommendation response is missing keywordTargetList.");
                var recommendations = new List<KeywordTarget>();
                foreach (var row in rows)
                {
                    var keyword = row as JObject ??
                        throw new InvalidOperationException("Amazon Ads returned an invalid keyword recommendation.");
                    var text = keyword.Value<string>("keyword");
                    var bids = keyword["bidInfo"] as JArray;
                    if (string.IsNullOrWhiteSpace(text) || bids == null)
                        throw new InvalidOperationException("Amazon Ads keyword recommendation is missing keyword or bidInfo.");
                    foreach (var item in bids)
                    {
                        var bid = item as JObject ??
                            throw new InvalidOperationException("Amazon Ads returned an invalid recommendation bid.");
                        var matchType = bid.Value<string>("matchType");
                        var cents = bid.Value<double?>("bid");
                        if (string.IsNullOrWhiteSpace(matchType) || !cents.HasValue ||
                            double.IsNaN(cents.Value) || double.IsInfinity(cents.Value))
                            throw new InvalidOperationException("Amazon Ads recommendation is missing matchType or bid.");
                        recommendations.Add(new KeywordTarget
                        {
                            Keyword = text,
                            MatchType = matchType,
                            Bid = Math.Round(cents.Value / 100.0, 2)
                        });
                    }
                }
                return recommendations;
            }
            finally
            {
                timeouts.AsynchronousJavaScript = originalTimeout;
            }
        }

        private static void Validate(KeywordRecommendationRequest request)
        {
            if (request == null) throw new ArgumentException("Request body is required.");
            AmazonAdsAccountInfoValidator.Validate(request.AccountInfo);
            if (string.IsNullOrWhiteSpace(request.Asin))
                throw new ArgumentException("Asin is required.");
            if (request.Keywords == null || request.Keywords.Count == 0)
                throw new ArgumentException("Keywords must contain at least one item.");
            foreach (var keyword in request.Keywords)
                if (string.IsNullOrWhiteSpace(keyword))
                    throw new ArgumentException("Keywords cannot contain empty items.");
            if (request.MatchTypes == null || request.MatchTypes.Count == 0)
                throw new ArgumentException("MatchTypes must contain at least one item.");
            foreach (var matchType in request.MatchTypes)
                if (string.IsNullOrWhiteSpace(matchType))
                    throw new ArgumentException("MatchTypes cannot contain empty items.");
        }

        private static void ConfirmActiveAccount(IWebDriver driver, AmazonAdsAccountInfo expected)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            AmazonAdsAccountInfo actual;
            do
            {
                actual = AmazonAdsAccountInfoService.Parse(driver.PageSource);
                if (!string.IsNullOrEmpty(actual.EntityId) &&
                    !string.IsNullOrEmpty(actual.GlobalAccountId) &&
                    !string.IsNullOrEmpty(actual.MarketplaceId))
                    break;
                if (DateTime.UtcNow >= deadline)
                    throw new InvalidOperationException(
                        "Amazon Ads account information is missing in Chrome; sign in and select the account.");
                Thread.Sleep(250);
            } while (true);

            if (!string.Equals(actual.EntityId, expected.EntityId, StringComparison.Ordinal) ||
                !string.Equals(actual.GlobalAccountId, expected.GlobalAccountId, StringComparison.Ordinal) ||
                !string.Equals(actual.MarketplaceId, expected.MarketplaceId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "AccountInfo does not match the active Amazon Ads account in Chrome. Refresh account info before requesting recommendations.");
        }
    }
}
