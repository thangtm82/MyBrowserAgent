using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using MyBrowserAgent.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;

namespace MyBrowserAgent.Services
{
    public sealed class AmazonAdsTargetBidService
    {
        private const string CampaignUrl = "https://advertising.amazon.com/cb";

        // Chrome sends the session cookies and browser-owned Origin/Fetch headers.
        private const string UpdateScript = @"
            var done = arguments[arguments.length - 1];
            var headers = arguments[0];
            var body = arguments[1];
            var controller = new AbortController();
            var timer = setTimeout(function () { controller.abort(); }, 45000);
            fetch('/a9g-api-gateway/cm/adsApi/targets/update', {
                method: 'PUT', credentials: 'same-origin', headers: headers,
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

        public JObject Update(BrowserService browser, TargetBidUpdateRequest request)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));
            Validate(request);
            var targetId = request.TargetId.Trim();
            var body = new JArray(new JObject
            {
                ["targetId"] = targetId,
                ["countryCodes"] = new JArray(request.CountryCode.Trim().ToUpperInvariant()),
                ["bid"] = request.Bid.Value.ToString(CultureInfo.InvariantCulture)
            });
            return browser.RunInTemporaryTab(CampaignUrl,
                driver => UpdateInBrowser(driver, body, targetId));
        }

        public JObject UpdateMany(BrowserService browser, IList<TargetBidUpdateItem> requests)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));
            var body = BuildManyPayload(requests);
            return browser.RunInTemporaryTab(CampaignUrl,
                driver => UpdateInBrowser(driver, body, null));
        }

        private static JObject UpdateInBrowser(IWebDriver driver, JArray body, string singleTargetId)
        {
            var info = WaitForAccountInfo(driver);
            var headers = new Dictionary<string, string>
            {
                ["accept"] = "application/json",
                ["accept-language"] = "en-US,en;q=0.9",
                ["content-type"] = "application/json",
                ["amazon-ads-account-id"] = info.GlobalAccountId,
                ["amazon-advertising-api-advertiserid"] = info.EntityId,
                ["amazon-advertising-api-clientid"] = info.ClientId,
                ["amazon-advertising-api-csrf-data"] = info.ClientId,
                ["amazon-advertising-api-csrf-token"] = info.CsrfToken,
                ["amazon-advertising-api-isimpersonator"] = "false",
                ["amazon-advertising-api-marketplaceid"] = info.MarketplaceId,
                ["prefer"] = "return=representation"
            };
            if (!string.IsNullOrEmpty(info.TraceId) && !string.IsNullOrEmpty(info.SegmentId))
                headers["x-amzn-trace-id"] = "Root=" + info.TraceId +
                    ";Parent=" + info.SegmentId + ";Sampled=1";

            var timeouts = driver.Manage().Timeouts();
            var originalTimeout = timeouts.AsynchronousJavaScript;
            try
            {
                timeouts.AsynchronousJavaScript = TimeSpan.FromSeconds(60);
                var raw = ((IJavaScriptExecutor)driver).ExecuteAsyncScript(
                    UpdateScript, headers, body.ToString(Formatting.None)) as string;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("Amazon Ads returned no target bid update result.");

                var result = JObject.Parse(raw);
                if (result.Value<bool?>("ok") != true)
                    throw new InvalidOperationException("Amazon Ads target bid update failed (HTTP " +
                        (result.Value<int?>("status")?.ToString() ?? "unavailable") +
                        "). Check login, target access, and bid.");

                var response = JObject.Parse(result.Value<string>("body") ?? "");
                ConfirmResponseShape(response);
                if (singleTargetId != null)
                    ConfirmUpdate(response, singleTargetId);
                return response;
            }
            finally
            {
                timeouts.AsynchronousJavaScript = originalTimeout;
            }
        }

        private static void ConfirmResponseShape(JObject response)
        {
            if (!(response["updatedTargets"] is JArray) ||
                !(response["failedTargetIds"] is JArray) ||
                !(response["bulkUpdateSummary"] is JObject))
                throw new InvalidOperationException("Amazon Ads target bid response is missing update results.");
        }

        private static void ConfirmUpdate(JObject response, string targetId)
        {
            var updated = (JArray)response["updatedTargets"];
            var failed = (JArray)response["failedTargetIds"];
            var summary = (JObject)response["bulkUpdateSummary"];
            var confirmed = false;
            foreach (var item in updated)
            {
                if (!(item is JObject target))
                    throw new InvalidOperationException("Amazon Ads returned an invalid updated target.");
                if (string.Equals(target.Value<string>("targetId"), targetId, StringComparison.Ordinal) &&
                    target.Value<decimal?>("bid").HasValue)
                    confirmed = true;
            }
            if (!confirmed || failed.Count != 0 ||
                summary.Value<int?>("successfulCount") != 1 ||
                summary.Value<int?>("invalidCount") != 0 ||
                summary.Value<int?>("failedCount") != 0 ||
                summary.Value<int?>("partiallyUpdatedCount") != 0)
                throw new InvalidOperationException(
                    "Amazon Ads did not confirm a successful bid update for target " + targetId + ".");
        }

        private static AmazonAdsAccountInfo WaitForAccountInfo(IWebDriver driver)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            do
            {
                var info = AmazonAdsAccountInfoService.Parse(driver.PageSource);
                if (!string.IsNullOrEmpty(info.EntityId) && !string.IsNullOrEmpty(info.ClientId) &&
                    !string.IsNullOrEmpty(info.CsrfToken) && !string.IsNullOrEmpty(info.GlobalAccountId) &&
                    !string.IsNullOrEmpty(info.MarketplaceId))
                    return info;
                if (DateTime.UtcNow >= deadline) break;
                Thread.Sleep(250);
            } while (true);
            throw new InvalidOperationException("Amazon Ads account fields are missing; check the Chrome login or page format.");
        }

        private static JArray BuildManyPayload(IList<TargetBidUpdateItem> requests)
        {
            if (requests == null || requests.Count == 0)
                throw new ArgumentException("Request body must contain at least one target.");

            var body = new JArray();
            var targetIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in requests)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.TargetId))
                    throw new ArgumentException("Each targetId is required.");
                var targetId = item.TargetId.Trim();
                if (!targetIds.Add(targetId))
                    throw new ArgumentException("Duplicate targetId: " + targetId + ".");
                if (item.CountryCodes == null || item.CountryCodes.Count == 0)
                    throw new ArgumentException("Each countryCodes array must contain at least one code.");

                var countries = new JArray();
                foreach (var country in item.CountryCodes)
                {
                    if (string.IsNullOrWhiteSpace(country))
                        throw new ArgumentException("countryCodes cannot contain an empty code.");
                    countries.Add(country.Trim().ToUpperInvariant());
                }

                if (!decimal.TryParse(item.Bid, NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var bid) || bid <= 0)
                    throw new ArgumentException("Each bid must be a positive decimal string.");

                body.Add(new JObject
                {
                    ["targetId"] = targetId,
                    ["countryCodes"] = countries,
                    ["bid"] = bid.ToString(CultureInfo.InvariantCulture)
                });
            }
            return body;
        }

        private static void Validate(TargetBidUpdateRequest request)
        {
            if (request == null) throw new ArgumentException("Request body is required.");
            if (string.IsNullOrWhiteSpace(request.TargetId))
                throw new ArgumentException("TargetId is required.");
            if (string.IsNullOrWhiteSpace(request.CountryCode))
                throw new ArgumentException("CountryCode is required.");
            if (!request.Bid.HasValue || request.Bid.Value <= 0)
                throw new ArgumentException("Bid must be greater than zero.");
        }
    }
}
