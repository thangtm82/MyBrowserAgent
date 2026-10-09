using System;
using System.Collections.Generic;
using System.Threading;
using MyBrowserAgent.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;

namespace MyBrowserAgent.Services
{
    public sealed class AmazonAdsPortfolioService
    {
        private const string UsMarketplaceId = "ATVPDKIKX0DER";
        private const string CaMarketplaceId = "A2EUQ1WTGCTBG2";

        // Chrome supplies its session cookies and the browser-owned request headers.
        private const string CreateScript = @"
            var done = arguments[arguments.length - 1];
            var headers = arguments[0];
            var body = arguments[1];
            var entityId = arguments[2];
            var controller = new AbortController();
            var timer = setTimeout(function () { controller.abort(); }, 45000);
            fetch('/a9g-api-gateway/cm/api/portfolios', {
                method: 'POST',
                credentials: 'same-origin',
                headers: headers,
                body: body,
                referrer: location.origin + '/cm/portfolios?entityId=' + encodeURIComponent(entityId),
                signal: controller.signal
            }).then(function (response) {
                return response.text().then(function (text) {
                    clearTimeout(timer);
                    done(JSON.stringify({ ok: response.ok, status: response.status, body: text }));
                });
            }).catch(function (error) {
                clearTimeout(timer);
                done(JSON.stringify({ ok: false, error: String(error) }));
            });";

        public string Create(BrowserService browser, PortfolioCreateRequest request, string market = "US")
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));
            if (request == null) throw new ArgumentException("Request body is required.");
            AmazonAdsAccountInfoValidator.Validate(request.AccountInfo);
            AmazonAdsMarket.ValidateMarketplace(market, request.AccountInfo.MarketplaceId);
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name is required.");

            var currencyCode = GetCurrencyCode(request.AccountInfo.MarketplaceId);
            var body = new JObject
            {
                ["createPortfoliosInputList"] = new JArray(new JObject
                {
                    ["name"] = request.Name.Trim(),
                    ["budget"] = new JObject
                    {
                        ["currencyCode"] = currencyCode,
                        ["budgetType"] = "NO_CAP"
                    },
                    ["associateCampaignWithPortfolioInputList"] = new JArray()
                })
            };

            return browser.RunInTemporaryTab(AmazonAdsMarket.GetCampaignUrl(market),
                driver => CreateInBrowser(driver, request.AccountInfo, body));
        }

        // Updating the portfolio is the operation that returns portfolioExternalId.
        private const string UpdateScript = @"
            var done = arguments[arguments.length - 1];
            var headers = arguments[0];
            var body = arguments[1];
            var entityId = arguments[2];
            var portfolioId = arguments[3];
            var controller = new AbortController();
            var timer = setTimeout(function () { controller.abort(); }, 45000);
            fetch('/a9g-api-gateway/cm/api/portfolios', {
                method: 'PUT',
                credentials: 'same-origin',
                headers: headers,
                body: body,
                referrer: location.origin + '/cm/portfolios/' + encodeURIComponent(portfolioId) +
                    '/settings?entityId=' + encodeURIComponent(entityId) + '&ref=ALL_PORTFOLIOS',
                signal: controller.signal
            }).then(function (response) {
                return response.text().then(function (text) {
                    clearTimeout(timer);
                    done(JSON.stringify({ ok: response.ok, status: response.status, body: text }));
                });
            }).catch(function (error) {
                clearTimeout(timer);
                done(JSON.stringify({ ok: false, error: String(error) }));
            });";

        public string UpdateAndGetExternalId(BrowserService browser, PortfolioExternalIdRequest request, string market = "US")
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));
            if (request == null) throw new ArgumentException("Request body is required.");
            AmazonAdsAccountInfoValidator.Validate(request.AccountInfo);
            AmazonAdsMarket.ValidateMarketplace(market, request.AccountInfo.MarketplaceId);
            if (string.IsNullOrWhiteSpace(request.PortfolioId))
                throw new ArgumentException("PortfolioId is required.");
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name is required.");

            var portfolioId = request.PortfolioId.Trim();
            var body = new JObject
            {
                ["updatePortfoliosInputList"] = new JArray(new JObject
                {
                    ["name"] = request.Name.Trim(),
                    ["budget"] = new JObject
                    {
                        ["budgetType"] = "NO_CAP",
                        ["currencyCode"] = GetCurrencyCode(request.AccountInfo.MarketplaceId)
                    },
                    ["portfolioId"] = portfolioId
                })
            };

            return browser.RunInTemporaryTab(AmazonAdsMarket.GetCampaignUrl(market),
                driver => UpdateInBrowser(driver, request.AccountInfo, portfolioId, body));
        }

        private static string UpdateInBrowser(IWebDriver driver, AmazonAdsAccountInfo info,
            string portfolioId, JObject body)
        {
            ConfirmActiveAccount(driver, info);
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

            var timeouts = driver.Manage().Timeouts();
            var originalTimeout = timeouts.AsynchronousJavaScript;
            try
            {
                timeouts.AsynchronousJavaScript = TimeSpan.FromSeconds(60);
                var raw = ((IJavaScriptExecutor)driver).ExecuteAsyncScript(
                    UpdateScript, headers, body.ToString(Formatting.None), info.EntityId, portfolioId) as string;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException(
                        "Amazon Ads returned no portfolio update result. Check the portfolio before retrying.");

                var result = JObject.Parse(raw);
                if (result.Value<bool?>("ok") != true)
                    throw new InvalidOperationException(
                        "Amazon Ads portfolio update failed (HTTP " +
                        (result.Value<int?>("status")?.ToString() ?? "unavailable") +
                        "). Check the portfolio before retrying.");

                var response = JObject.Parse(result.Value<string>("body") ?? "");
                var outputs = response["updatePortfoliosOutputList"] as JArray;
                var externalId = (outputs?.First as JObject)?["portfolio"]?["portfolioExternalId"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(externalId))
                    throw new InvalidOperationException(
                        "Amazon Ads did not return portfolioExternalId. Check the portfolio before retrying.");
                return externalId;
            }
            finally
            {
                timeouts.AsynchronousJavaScript = originalTimeout;
            }
        }

        private static string CreateInBrowser(IWebDriver driver, AmazonAdsAccountInfo info, JObject body)
        {
            ConfirmActiveAccount(driver, info);

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

            var timeouts = driver.Manage().Timeouts();
            var originalTimeout = timeouts.AsynchronousJavaScript;
            try
            {
                timeouts.AsynchronousJavaScript = TimeSpan.FromSeconds(60);
                var raw = ((IJavaScriptExecutor)driver).ExecuteAsyncScript(
                    CreateScript, headers, body.ToString(Formatting.None), info.EntityId) as string;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException(
                        "Amazon Ads returned no portfolio result. Check whether it was created before retrying.");

                var result = JObject.Parse(raw);
                if (result.Value<bool?>("ok") != true)
                    throw new InvalidOperationException(
                        "Amazon Ads portfolio creation failed (HTTP " +
                        (result.Value<int?>("status")?.ToString() ?? "unavailable") +
                        "). Check the account before retrying.");

                var response = JObject.Parse(result.Value<string>("body") ?? "");
                var outputs = response["createPortfoliosOutputList"] as JArray;
                var id = (outputs?.First as JObject)?["id"]?.ToString();
                if (string.IsNullOrWhiteSpace(id))
                    throw new InvalidOperationException(
                        "Amazon Ads did not return a portfolio ID. Check whether it was created before retrying.");
                return id;
            }
            finally
            {
                timeouts.AsynchronousJavaScript = originalTimeout;
            }
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
                    "AccountInfo does not match the active Amazon Ads account in Chrome. Refresh account info before creating a portfolio.");
        }

        private static string GetCurrencyCode(string marketplaceId)
        {
            if (string.Equals(marketplaceId, UsMarketplaceId, StringComparison.Ordinal))
                return "USD";
            if (string.Equals(marketplaceId, CaMarketplaceId, StringComparison.Ordinal))
                return "CAD";
            throw new ArgumentException("Portfolio creation supports US and CA marketplaces only.");
        }
    }
}
