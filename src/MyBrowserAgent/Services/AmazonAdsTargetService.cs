using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MyBrowserAgent.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;

namespace MyBrowserAgent.Services
{
    public sealed class AmazonAdsTargetService
    {
        private const int PageSize = 50;
        private static readonly string[] Fields = {
            "targetId", "targetState", "calculatedStatusName", "calculatedStatusReasons",
            "programType", "countryCode", "currencyCode", "matchType", "target",
            "targetSecondary", "asin", "url", "mediaUrl", "title", "campaignId",
            "campaignName", "marketplaceId", "isGlobalCampaign", "isDspCampaign",
            "campaignExternalId", "campaignState", "adGroupId", "adGroupName",
            "adGroupExternalId", "roas", "conversionRate", "targetBid",
            "adGroupDefaultBid", "campaignBudget", "impressions",
            "topOfSearchImpressionShare", "clicks", "ctr", "spend", "spendCoV",
            "cpc", "cpcCoV", "orders", "sales", "salesCoV", "acos"
        };

        // The browser supplies the signed-in session cookies; no Cookie header is passed in.
        private const string FetchScript = @"
            var done = arguments[arguments.length - 1];
            var headers = arguments[0];
            var body = arguments[1];
            var controller = new AbortController();
            var timer = setTimeout(function () { controller.abort(); }, 45000);
            fetch('/a9g-api-gateway/cm/dds/retrieveReport', {
                method: 'POST', credentials: 'same-origin', headers: headers,
                body: body, signal: controller.signal
            }).then(function (response) {
                return response.text().then(function (text) {
                    clearTimeout(timer);
                    done(JSON.stringify({ ok: response.ok, status: response.status, body: text }));
                });
            }).catch(function (error) {
                clearTimeout(timer);
                done(JSON.stringify({ ok: false, error: String(error) }));
            });";

        public IList<JObject> Filter(BrowserService browser, TargetFilterRequest filter)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));
            Validate(filter);
            return browser.RunInTemporaryTab(AmazonAdsMarket.GetCampaignUrl(filter.Market),
                driver => FetchOnePage(driver, filter));
        }

        private static IList<JObject> FetchOnePage(IWebDriver driver, TargetFilterRequest filter)
        {
            var info = filter.AccountInfo;
            var headers = new Dictionary<string, string>
            {
                ["accept"] = "application/json",
                ["advertisertype"] = "SELLER",
                ["amazon-ads-account-id"] = info.GlobalAccountId,
                ["amazon-advertising-api-advertiserid"] = info.EntityId,
                ["amazon-advertising-api-clientid"] = info.ClientId,
                ["amazon-advertising-api-csrf-data"] = info.ClientId,
                ["amazon-advertising-api-csrf-token"] = info.CsrfToken,
                ["amazon-advertising-api-isimpersonator"] = "false",
                ["amazon-advertising-api-isportalserver"] = "false",
                ["amazon-advertising-api-marketplaceid"] = info.MarketplaceId,
                ["content-type"] = "application/json",
                ["locale"] = "en_US",
                ["prefer"] = "return=representation"
            };
            if (!string.IsNullOrEmpty(info.TraceId) && !string.IsNullOrEmpty(info.SegmentId))
                headers["x-amzn-trace-id"] = "Root=" + info.TraceId +
                    ";Parent=" + info.SegmentId + ";Sampled=1";

            var payload = BuildPayload(filter);
            var logRunId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture) +
                "-" + Guid.NewGuid().ToString("N");
            var timeouts = driver.Manage().Timeouts();
            var originalTimeout = timeouts.AsynchronousJavaScript;
            try
            {
                timeouts.AsynchronousJavaScript = TimeSpan.FromSeconds(60);
                var report = FetchPage(driver, headers, payload, logRunId, 1);
                var rows = report["data"] as JArray;
                if (rows == null)
                    throw new InvalidOperationException("Amazon Ads target report is missing report.data.");

                var returnedOffset = (report["offsetPagination"] as JObject)?.Value<int?>("offset");
                if (returnedOffset.HasValue && returnedOffset.Value != filter.Offset.Value)
                    throw new InvalidOperationException("Amazon Ads returned an unexpected target page offset.");

                var targets = new List<JObject>(rows.Count);
                foreach (var row in rows)
                {
                    if (!(row is JObject target))
                        throw new InvalidOperationException("Amazon Ads returned an invalid target row.");
                    targets.Add(target);
                }
                return targets;
            }
            finally
            {
                timeouts.AsynchronousJavaScript = originalTimeout;
            }
        }

        private static JObject FetchPage(IWebDriver driver, IDictionary<string, string> headers,
            JObject payload, string logRunId, int page)
        {
            var raw = ((IJavaScriptExecutor)driver).ExecuteAsyncScript(
                FetchScript, headers, payload.ToString(Formatting.None)) as string;
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Amazon Ads returned no target report JavaScript result.");

            var result = JObject.Parse(raw);
            LogResponse(result, logRunId, page);
            if (result.Value<bool?>("ok") != true)
                throw new InvalidOperationException("Amazon Ads target request failed (HTTP " +
                    (result.Value<int?>("status")?.ToString() ?? "unavailable") +
                    "). Check login and account access.");
            var response = JObject.Parse(result.Value<string>("body") ?? "");
            return response["report"] as JObject ??
                throw new InvalidOperationException("Amazon Ads target response is missing report.");
        }

        private static void LogResponse(JObject result, string logRunId, int page)
        {
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(directory);
            var filename = "amazon-ads-targets-" + logRunId + "-page-" +
                page.ToString(CultureInfo.InvariantCulture) + ".log";
            var content = "TimestampUtc: " + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) +
                Environment.NewLine + "Page: " + page.ToString(CultureInfo.InvariantCulture) +
                Environment.NewLine + "HttpStatus: " +
                (result.Value<int?>("status")?.ToString(CultureInfo.InvariantCulture) ?? "unavailable") +
                Environment.NewLine + "ResponseBody:" + Environment.NewLine +
                (result.Value<string>("body") ?? "") + Environment.NewLine;
            var error = result.Value<string>("error");
            if (!string.IsNullOrEmpty(error))
                content += "JavaScriptError: " + error + Environment.NewLine;
            File.WriteAllText(Path.Combine(directory, filename), content, new UTF8Encoding(false));
        }

        private static JObject BuildPayload(TargetFilterRequest filter)
        {
            var conditions = new JArray(
                new JObject { ["field"] = "acos", ["comparisonOperator"] = "LESS_THAN_OR_EQUALS",
                    ["value"] = filter.MaxAcos.Value, ["not"] = false },
                new JObject { ["field"] = "acos", ["comparisonOperator"] = "GREATER_THAN_OR_EQUALS",
                    ["value"] = filter.MinAcos.Value, ["not"] = false });

            if (!string.IsNullOrWhiteSpace(filter.MatchType))
            {
                var values = new JArray();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var part in filter.MatchType.Split(','))
                {
                    var value = part.Trim();
                    if (value.Length > 0 && seen.Add(value))
                        values.Add(value);
                }

                if (values.Count > 0)
                {
                    conditions.Add(new JObject
                    {
                        ["field"] = "matchType",
                        ["values"] = values,
                        ["comparisonOperator"] = "IN",
                        ["not"] = false
                    });
                }
            }

            return new JObject
            {
                ["reportConfig"] = new JObject
                {
                    ["reportId"] = "TargetsReport",
                    ["fields"] = new JArray(Fields),
                    ["filter"] = new JObject { ["and"] = conditions },
                    ["startDate"] = filter.StartDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["endDate"] = filter.EndDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["timeUnits"] = new JArray("SUMMARY", "DAILY"),
                    ["offsetPagination"] = new JObject { ["size"] = PageSize, ["offset"] = filter.Offset.Value },
                    ["currencyOfView"] = "USD",
                    ["sort"] = new JObject { ["sortField"] = "spendCoV", ["sortOrder"] = "DESC" }
                }
            };
        }

        private static void Validate(TargetFilterRequest filter)
        {
            if (filter == null) throw new ArgumentException("Request body is required.");
            AmazonAdsAccountInfoValidator.Validate(filter.AccountInfo);
            AmazonAdsMarket.ValidateMarketplace(filter.Market, filter.AccountInfo.MarketplaceId);
            if (!filter.MinAcos.HasValue || !filter.MaxAcos.HasValue ||
                filter.MinAcos < 0 || filter.MinAcos > filter.MaxAcos)
                throw new ArgumentException("MinAcos and MaxAcos must be nonnegative and MinAcos <= MaxAcos.");
            if (!filter.StartDate.HasValue || !filter.EndDate.HasValue || filter.StartDate > filter.EndDate)
                throw new ArgumentException("StartDate and EndDate are required and StartDate must be <= EndDate.");
            if (!filter.Offset.HasValue || filter.Offset.Value < 0)
                throw new ArgumentException("Offset is required and must be nonnegative.");
        }
    }
}
