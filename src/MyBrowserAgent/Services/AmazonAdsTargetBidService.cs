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
            return browser.RunInTemporaryTab(CampaignUrl, driver => UpdateInBrowser(driver, request));
        }

        private static JObject UpdateInBrowser(IWebDriver driver, TargetBidUpdateRequest request)
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

            var targetId = request.TargetId.Trim();
            var body = new JArray(new JObject
            {
                ["targetId"] = targetId,
                ["countryCodes"] = new JArray(request.CountryCode.Trim().ToUpperInvariant()),
                // The captured request sends bid as a JSON string.
                ["bid"] = request.Bid.Value.ToString(CultureInfo.InvariantCulture)
            });

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
                ConfirmUpdate(response, targetId);
                return response;
            }
            finally
            {
                timeouts.AsynchronousJavaScript = originalTimeout;
            }
        }

        private static void ConfirmUpdate(JObject response, string targetId)
        {
            var updated = response["updatedTargets"] as JArray;
            var failed = response["failedTargetIds"] as JArray;
            var summary = response["bulkUpdateSummary"] as JObject;
            if (updated == null || failed == null || summary == null)
                throw new InvalidOperationException("Amazon Ads target bid response is missing update results.");

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
