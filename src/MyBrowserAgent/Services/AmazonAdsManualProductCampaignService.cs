using System;
using System.Collections.Generic;
using System.Threading;
using MyBrowserAgent.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;

namespace MyBrowserAgent.Services
{
    public sealed class AmazonAdsManualProductCampaignService
    {
        private const string CampaignUrl = "https://advertising.amazon.com/cb";
        private const string UsMarketplaceId = "ATVPDKIKX0DER";
        private const string CaMarketplaceId = "A2EUQ1WTGCTBG2";
        private static readonly JsonSerializerSettings BodySettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private const string SubmitScript = @"
            var done = arguments[arguments.length - 1];
            var headers = arguments[0];
            var body = arguments[1];
            var entityId = arguments[2];
            var controller = new AbortController();
            var timer = setTimeout(function () { controller.abort(); }, 45000);
            fetch('/a9g-api-gateway/atlas/submit', {
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

        public ManualProductCampaignCreateResult Create(
            BrowserService browser, ManualProductCampaignCreateRequest request)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));
            Validate(request);

            var payload = new AdsManualProductTargetingFormRequest
            {
                FormVersion = request.FormVersion,
                FormData = request.FormData,
                FeatureFlags = request.FeatureFlags ?? new JObject()
            };
            var body = JsonConvert.SerializeObject(payload, Formatting.None, BodySettings);
            return browser.RunInTemporaryTab(CampaignUrl,
                driver => SubmitInBrowser(driver, request.AccountInfo, body));
        }

        private static ManualProductCampaignCreateResult SubmitInBrowser(
            IWebDriver driver, AmazonAdsAccountInfo info, string body)
        {
            ConfirmActiveAccount(driver, info);
            var headers = new Dictionary<string, string>
            {
                ["accept"] = "application/json",
                ["accept-language"] = "en-US,en;q=0.9,vi;q=0.8",
                ["content-type"] = "application/json",
                ["amazon-advertising-api-advertiserid"] = info.EntityId,
                ["amazon-advertising-api-clientid"] = info.ClientId,
                ["amazon-advertising-api-csrf-data"] = info.ClientId,
                ["amazon-advertising-api-csrf-token"] = info.CsrfToken,
                ["amazon-advertising-api-isimpersonator"] = "false",
                ["amazon-advertising-api-marketplaceid"] = info.MarketplaceId,
                ["amazon-advertising-api-scope"] = "INTERNAL_SCOPE",
                ["prefer"] = "return=representation",
                ["x-amzn-access-type"] = "customer",
                ["x-amzn-advertiser-id"] = info.AdvertiserId,
                ["x-amzn-advertiser-type"] = "seller",
                ["x-amzn-customer-id"] = info.AdvertiserId,
                ["x-amzn-entity-id"] = info.EntityId,
                ["x-amzn-form-id"] = "sp",
                ["x-amzn-marketplace-id"] = info.MarketplaceId,
                ["x-amzn-page-hit-request-id"] = info.PageHitRequestId,
                ["x-amzn-session-id"] = info.SessionId
            };

            var timeouts = driver.Manage().Timeouts();
            var originalTimeout = timeouts.AsynchronousJavaScript;
            try
            {
                timeouts.AsynchronousJavaScript = TimeSpan.FromSeconds(60);
                var raw = ((IJavaScriptExecutor)driver).ExecuteAsyncScript(
                    SubmitScript, headers, body, info.EntityId) as string;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException(
                        "Amazon Ads returned no campaign result. Check for a created campaign before retrying.");

                var response = JObject.Parse(raw);
                var status = response.Value<int?>("status");
                if (!status.HasValue)
                    throw new InvalidOperationException(
                        "The browser could not complete the Amazon Ads campaign request. Check for a created campaign before retrying.");

                return new ManualProductCampaignCreateResult
                {
                    StatusCode = status.Value,
                    Succeeded = response.Value<bool?>("ok") == true,
                    Content = response.Value<string>("body") ?? string.Empty
                };
            }
            finally
            {
                timeouts.AsynchronousJavaScript = originalTimeout;
            }
        }

        private static void Validate(ManualProductCampaignCreateRequest request)
        {
            if (request == null) throw new ArgumentException("Request body is required.");
            AmazonAdsAccountInfoValidator.Validate(request.AccountInfo);
            if (string.IsNullOrWhiteSpace(request.AccountInfo.AdvertiserId) ||
                string.IsNullOrWhiteSpace(request.AccountInfo.PageHitRequestId) ||
                string.IsNullOrWhiteSpace(request.AccountInfo.SessionId))
                throw new ArgumentException(
                    "AccountInfo also requires AdvertiserId, PageHitRequestId and SessionId for campaign creation.");
            if (request.FormVersion <= 0)
                throw new ArgumentException("FormVersion must be positive.");

            var form = request.FormData;
            if (form == null) throw new ArgumentException("FormData is required.");
            if (string.IsNullOrWhiteSpace(form.CampaignName) ||
                string.IsNullOrWhiteSpace(form.AdGroupName))
                throw new ArgumentException("CampaignName and AdGroupName are required.");
            if (!string.Equals(form.TargetingType, "MANUAL", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(form.ManualTargetingType, "PRODUCT", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("TargetingType must be MANUAL and ManualTargetingType must be PRODUCT.");
            if (form.Budget <= 0 || form.DefaultBid <= 0)
                throw new ArgumentException("Budget and DefaultBid must be positive.");
            if (form.StartDate <= 0)
                throw new ArgumentException("StartDate is required.");
            if (form.Portfolio == null)
                throw new ArgumentException("Portfolio is required.");
            if (form.Products == null || form.Products.Count == 0)
                throw new ArgumentException("Products must contain at least one item.");
            foreach (var product in form.Products)
                if (product == null || string.IsNullOrWhiteSpace(product.Asin) ||
                    product.Merchant == null || string.IsNullOrWhiteSpace(product.Merchant.Sku))
                    throw new ArgumentException("Each product requires Asin and Merchant.Sku.");
            if (form.ProductTargets == null || form.ProductTargets.Count == 0)
                throw new ArgumentException("ProductTargets must contain at least one item.");
            foreach (var target in form.ProductTargets)
                if (target == null)
                    throw new ArgumentException("ProductTargets cannot contain null items.");

            var currency = GetCurrencyCode(request.AccountInfo.MarketplaceId);
            if (form.Portfolio.Budget == null)
                form.Portfolio.Budget = new Budget();
            // The source function always takes portfolio currency from the selected marketplace.
            form.Portfolio.Budget.CurrencyCode = currency;
        }

        private static string GetCurrencyCode(string marketplaceId)
        {
            if (string.Equals(marketplaceId, UsMarketplaceId, StringComparison.Ordinal))
                return "USD";
            if (string.Equals(marketplaceId, CaMarketplaceId, StringComparison.Ordinal))
                return "CAD";
            throw new ArgumentException("Manual product campaign creation supports US and CA marketplaces only.");
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
                !string.Equals(actual.MarketplaceId, expected.MarketplaceId, StringComparison.Ordinal) ||
                !string.Equals(actual.AdvertiserId, expected.AdvertiserId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "AccountInfo does not match the active Amazon Ads account in Chrome. Refresh account info before creating a campaign.");
        }
    }
}
