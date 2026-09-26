using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using MyBrowserAgent.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;

namespace MyBrowserAgent.Services
{
    public sealed class AmazonAdsCampaignService
    {
        private const string CampaignUrl = "https://advertising.amazon.com/cb";
        private const int PageSize = 50;
        private static readonly string[] Fields = {
            "campaignExternalId", "campaignName", "state", "statusName", "marketplaceId",
            "programType", "isDspCampaign", "advertiserId", "advertiserCfId", "globalAccountId",
            "campaignCostType", "dateValue", "budgetCurrency", "currencyOfView", "campaignFlightId",
            "saStatusName", "dspCampaignDeliveryStatus", "globalCampaignId", "internalProgramType",
            "campaignId", "bidAdjustmentPredicates", "bidAdjustmentPercentages", "statusReasons",
            "isGlobalCampaign", "campaignType", "campaignTargetingType", "entityId",
            "campaign.autoManaged", "siteRestrictions", "campaign.country", "countryCodes",
            "brandName", "sourceName", "campaignRetailer", "campaignBiddingStrategy",
            "portfolioId", "portfolioExternalId", "portfolioName", "currencyCode", "advertiserName",
            "startDate", "endDate", "timezone", "campaignBudgetAmount", "campaignBudgetAmountCoV",
            "campaignBudgetType", "campaignEffectiveBudget", "campaignEffectiveBudgetCoV",
            "campaignRuleBasedBudgetAmount", "campaignRuleBasedBudgetAmountCoV",
            "campaignApplicableBudgetRuleId", "campaignApplicableBudgetRuleName",
            "isAnyRuleAssociatedWithCampaign", "bidType", "clicks", "ctr", "cpc", "cpcCoV",
            "spend", "spendCoV", "sales", "salesCoV", "acos", "topOfSearchImpressionShare",
            "orders", "roas", "longTermSales", "longTermSalesCoV"
        };

        // The last argument is Selenium's ExecuteAsyncScript callback. Browser cookies are sent
        // by fetch itself; the caller never passes or logs a Cookie header.
        private const string FetchScript = @"
            var done = arguments[arguments.length - 1];
            var headers = arguments[0];
            var body = arguments[1];
            var controller = new AbortController();
            var timer = setTimeout(function () { controller.abort(); }, 45000);
            fetch('/a9g-api-gateway/campaign-manager/retrieveReport', {
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

        public IList<JObject> Filter(BrowserService browser, CampaignFilterRequest filter)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));
            Validate(filter);
            return browser.RunInTemporaryTab(CampaignUrl, driver => FetchAll(driver, filter));
        }

        private static IList<JObject> FetchAll(IWebDriver driver, CampaignFilterRequest filter)
        {
            var info = WaitForAccountInfo(driver);
            var headers = new Dictionary<string, string>
            {
                ["accept"] = "application/json",
                ["advertiserid"] = info.GlobalAccountId,
                ["advertisertype"] = "undefined",
                ["amazon-ads-account-id"] = info.GlobalAccountId,
                ["amazon-advertising-api-advertiserid"] = info.EntityId,
                ["amazon-advertising-api-clientid"] = info.ClientId,
                ["amazon-advertising-api-csrf-data"] = info.ClientId,
                ["amazon-advertising-api-csrf-token"] = info.CsrfToken,
                ["amazon-advertising-api-isimpersonator"] = "false",
                ["amazon-advertising-api-isportalserver"] = "false",
                ["amazon-advertising-api-marketplaceid"] = info.MarketplaceId,
                ["content-type"] = "application/json",
                ["prefer"] = "return=representation",
                ["x-ads-report-source-id"] = "campaignOverviewAllCampaigns",
                ["x-amz-isglobalcampaignenabled"] = "true"
            };

            var payload = BuildPayload(filter, info.GlobalAccountId);
            var campaigns = new List<JObject>();
            var seenTokens = new HashSet<string>(StringComparer.Ordinal);
            var logRunId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture) +
                "-" + Guid.NewGuid().ToString("N");
            var deadline = DateTime.UtcNow.AddMinutes(8);
            var timeout = driver.Manage().Timeouts();
            var originalTimeout = timeout.AsynchronousJavaScript;
            try
            {
                timeout.AsynchronousJavaScript = TimeSpan.FromSeconds(60);
                for (var page = 0; page < 100; page++)
                {
                    if (DateTime.UtcNow >= deadline)
                        throw new InvalidOperationException("Amazon Ads campaign pagination exceeded eight minutes.");
                    var report = FetchPage(driver, headers, payload, logRunId, page + 1);
                    var rows = report["data"] as JArray;
                    if (rows == null)
                        throw new InvalidOperationException("Amazon Ads report is missing report.data.");
                    foreach (var row in rows)
                    {
                        if (!(row is JObject campaign))
                            throw new InvalidOperationException("Amazon Ads returned an invalid campaign row.");
                        campaigns.Add(campaign);
                    }

                    var pagination = report["tokenPagination"] as JObject;
                    var nextToken = pagination?.Value<string>("nextPageToken") ??
                        pagination?.Value<string>("nextToken") ?? pagination?.Value<string>("token");
                    if (string.IsNullOrEmpty(nextToken))
                        return campaigns;
                    if (!seenTokens.Add(nextToken))
                        throw new InvalidOperationException("Amazon Ads repeated a campaign pagination token.");

                    var requestPagination = (JObject)payload["reportConfig"]["tokenPagination"];
                    // Match the browser's next-page request: both fields carry the response token.
                    requestPagination["nextToken"] = nextToken;
                    requestPagination["nextPageToken"] = nextToken;
                }
                throw new InvalidOperationException("Amazon Ads campaign pagination exceeded 100 pages.");
            }
            finally
            {
                timeout.AsynchronousJavaScript = originalTimeout;
            }
        }

        private static AmazonAdsAccountInfo WaitForAccountInfo(IWebDriver driver)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            AmazonAdsAccountInfo info;
            do
            {
                info = AmazonAdsAccountInfoService.Parse(driver.PageSource);
                if (!string.IsNullOrEmpty(info.EntityId) && !string.IsNullOrEmpty(info.ClientId) &&
                    !string.IsNullOrEmpty(info.CsrfToken) && !string.IsNullOrEmpty(info.GlobalAccountId) &&
                    !string.IsNullOrEmpty(info.MarketplaceId))
                    return info;
                if (DateTime.UtcNow >= deadline) break;
                Thread.Sleep(250);
            } while (true);
            throw new InvalidOperationException("Amazon Ads account fields are missing; check the Chrome login or page format.");
        }

        private static JObject FetchPage(IWebDriver driver, IDictionary<string, string> headers,
            JObject payload, string logRunId, int page)
        {
            var raw = ((IJavaScriptExecutor)driver).ExecuteAsyncScript(
                FetchScript, headers, payload.ToString(Formatting.None)) as string;
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Amazon Ads returned no JavaScript result.");

            var result = JObject.Parse(raw);
            LogResponse(result, logRunId, page);
            if (result.Value<bool?>("ok") != true)
                throw new InvalidOperationException("Amazon Ads request failed (HTTP " +
                    (result.Value<int?>("status")?.ToString() ?? "unavailable") + "). Check login and account access.");
            var response = JObject.Parse(result.Value<string>("body") ?? "");
            return response["report"] as JObject ??
                throw new InvalidOperationException("Amazon Ads response is missing report.");
        }

        private static void LogResponse(JObject result, string logRunId, int page)
        {
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(directory);
            var filename = "amazon-ads-campaigns-" + logRunId + "-page-" +
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

        private static JObject BuildPayload(CampaignFilterRequest filter, string accountId)
        {
            var conditions = new JArray(
                new JObject { ["field"] = "state", ["values"] = new JArray("ENABLED", "PAUSED"),
                    ["comparisonOperator"] = "IN", ["not"] = false },
                new JObject { ["field"] = "campaignTargetingType", ["values"] = new JArray(filter.TargetType),
                    ["comparisonOperator"] = "IN", ["not"] = false },
                new JObject { ["field"] = "acos", ["comparisonOperator"] = "GREATER_THAN_OR_EQUALS",
                    ["value"] = filter.MinAcos.Value, ["not"] = false },
                new JObject { ["field"] = "acos", ["comparisonOperator"] = "LESS_THAN_OR_EQUALS",
                    ["value"] = filter.MaxAcos.Value, ["not"] = false },
                new JObject { ["field"] = "programType", ["values"] = new JArray("SP", "SPONSORED_ADS_RETAILERS"),
                    ["comparisonOperator"] = "IN", ["not"] = false });

            return new JObject
            {
                ["reportConfig"] = new JObject
                {
                    ["reportId"] = "UnifiedCampaignReportSyncAPI",
                    ["fields"] = new JArray(Fields),
                    ["filter"] = new JObject { ["and"] = conditions },
                    ["startDate"] = filter.StartDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["endDate"] = filter.EndDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["timeUnits"] = new JArray("SUMMARY", "DAILY"),
                    ["tokenPagination"] = new JObject { ["size"] = PageSize },
                    ["currencyOfView"] = "USD",
                    ["sort"] = new JObject { ["sortField"] = "startDate", ["sortOrder"] = "DESC" },
                    ["columnsByTimeUnit"] = new JObject
                    {
                        ["SUMMARY"] = new JArray("clicks", "ctr", "cpcCoV", "spendCoV", "salesCoV",
                            "acos", "orders", "roas", "longTermSalesCoV"),
                        ["DAILY"] = new JArray("spendCoV", "salesCoV", "orders", "roas", "longTermSalesCoV")
                    }
                },
                ["accessRequestedAccounts"] = new JArray(new JObject { ["accountId"] = accountId })
            };
        }

        private static void Validate(CampaignFilterRequest filter)
        {
            if (filter == null) throw new ArgumentException("Request body is required.");
            if (string.IsNullOrWhiteSpace(filter.TargetType)) throw new ArgumentException("TargetType is required.");
            if (!filter.MinAcos.HasValue || !filter.MaxAcos.HasValue ||
                filter.MinAcos < 0 || filter.MinAcos > filter.MaxAcos)
                throw new ArgumentException("MinAcos and MaxAcos must be nonnegative and MinAcos <= MaxAcos.");
            if (!filter.StartDate.HasValue || !filter.EndDate.HasValue || filter.StartDate > filter.EndDate)
                throw new ArgumentException("StartDate and EndDate are required and StartDate must be <= EndDate.");
        }
    }
}
