using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DesktopController
{
    public sealed class BrowserAgentClient : IDisposable
    {
        private readonly HttpClient _http;
        public string Name { get; }

        public BrowserAgentClient(string name, string baseUrl, string apiKey)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = Timeout.InfiniteTimeSpan
            };
            _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        public async Task<AgentStatus> GetStatusAsync()
        {
            var response = await SendAsync<AgentStatus>(HttpMethod.Get, "api/browser/status", null);
            return response.Data;
        }

        public Task StartAsync() => SendNoResultAsync(HttpMethod.Post, "api/browser/start", null);
        public Task StopAsync() => SendNoResultAsync(HttpMethod.Post, "api/browser/stop", null);
        public Task OpenAsync(string url) => SendNoResultAsync(HttpMethod.Post, "api/browser/open", new { Url = url });
        public Task ClickAsync(string selector, string selectorType = "css") => SendNoResultAsync(HttpMethod.Post, "api/browser/click", new { Selector = selector, SelectorType = selectorType });
        public Task FillAsync(string selector, string value, string selectorType = "css") => SendNoResultAsync(HttpMethod.Post, "api/browser/fill", new { Selector = selector, SelectorType = selectorType, Value = value });
        public Task SendKeysAsync(string selector, string value, string selectorType = "css") => SendNoResultAsync(HttpMethod.Post, "api/browser/sendkeys", new { Selector = selector, SelectorType = selectorType, Value = value });

        public async Task<AmazonAdsAccountInfo> StartAmazonAdsSessionAsync(string market = "US")
        {
            var response = await SendAsync<AmazonAdsAccountInfo>(
                HttpMethod.Post, "api/amazon-ads/start-session?market=" + Uri.EscapeDataString(market ?? "US"),
                null, TimeSpan.FromMinutes(5));
            return response.Data;
        }

        public async Task<AmazonAdsAccountInfo> GetAmazonAdsAccountInfoAsync()
        {
            var response = await SendAsync<AmazonAdsAccountInfo>(HttpMethod.Post, "api/amazon-ads/account-info", null);
            return response.Data;
        }

        public async Task<IList<JObject>> FilterCampaignsAsync(AmazonAdsAccountInfo accountInfo,
            string targetType, decimal minAcos, decimal maxAcos,
            DateTime startDate, DateTime endDate)
        {
            var response = await SendAsync<List<JObject>>(HttpMethod.Post, "api/amazon-ads/campaigns/filter",
                new
                {
                    AccountInfo = accountInfo,
                    TargetType = targetType,
                    MinAcos = minAcos,
                    MaxAcos = maxAcos,
                    StartDate = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    EndDate = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                }, TimeSpan.FromMinutes(10));
            return response.Data;
        }

        public Task<IList<JObject>> FilterTargetsAsync(AmazonAdsAccountInfo accountInfo,
            decimal minAcos, decimal maxAcos, DateTime startDate, DateTime endDate, int offset,
            string matchType = null, string market = "US")
        {
            return FilterTargetsCoreAsync<JObject>(
                accountInfo, minAcos, maxAcos, startDate, endDate, offset, matchType, market);
        }

        public Task<IList<AmazonAdsTarget>> FilterTargetsTypedAsync(AmazonAdsAccountInfo accountInfo,
            decimal minAcos, decimal maxAcos, DateTime startDate, DateTime endDate, int offset,
            string matchType = null, string market = "US")
        {
            return FilterTargetsCoreAsync<AmazonAdsTarget>(
                accountInfo, minAcos, maxAcos, startDate, endDate, offset, matchType, market);
        }

        private async Task<IList<T>> FilterTargetsCoreAsync<T>(AmazonAdsAccountInfo accountInfo,
            decimal minAcos, decimal maxAcos, DateTime startDate, DateTime endDate, int offset,
            string matchType, string market)
        {
            var response = await SendAsync<List<T>>(HttpMethod.Post, "api/amazon-ads/targets/filter",
                new
                {
                    AccountInfo = accountInfo,
                    Market = market,
                    MinAcos = minAcos,
                    MaxAcos = maxAcos,
                    StartDate = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    EndDate = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Offset = offset,
                    MatchType = matchType
                }, TimeSpan.FromMinutes(3));
            return response.Data;
        }

        public async Task<IList<KeywordTarget>> GetKeywordRecommendationsAsync(
            AmazonAdsAccountInfo accountInfo, string asin,
            IList<string> keywords, IList<string> matchTypes)
        {
            var response = await SendAsync<List<KeywordTarget>>(
                HttpMethod.Post, "api/amazon-ads/targets/keywords/recommendations",
                new { AccountInfo = accountInfo, Asin = asin, Keywords = keywords, MatchTypes = matchTypes },
                TimeSpan.FromMinutes(2));
            return response.Data;
        }

        public async Task<AmazonAdsTargetBidUpdateResult> UpdateTargetBidAsync(
            AmazonAdsAccountInfo accountInfo, string targetId, string countryCode, decimal bid)
        {
            var response = await SendAsync<AmazonAdsTargetBidUpdateResult>(
                HttpMethod.Put, "api/amazon-ads/targets/bid",
                new { AccountInfo = accountInfo, TargetId = targetId, CountryCode = countryCode, Bid = bid },
                TimeSpan.FromMinutes(3));
            return response.Data;
        }

        public async Task<AmazonAdsTargetBidUpdateResult> UpdateTargetBidsAsync(
            AmazonAdsAccountInfo accountInfo, IList<AmazonAdsTargetBidUpdateItem> targets)
        {
            var response = await SendAsync<AmazonAdsTargetBidUpdateResult>(
                HttpMethod.Put, "api/amazon-ads/targets/bids",
                new { AccountInfo = accountInfo, Targets = targets },
                TimeSpan.FromMinutes(3));
            if (response.Data != null) response.Data.TraceId = response.TraceId;
            return response.Data;
        }

        public async Task<AutoCampaignCreateResult> CreateAutoCampaignAsync(
            AmazonAdsAccountInfo accountInfo, AdsAutoFormData formData,
            int formVersion = 186, JObject featureFlags = null, string market = "US")
        {
            var response = await SendAsync<AutoCampaignCreateResult>(
                HttpMethod.Post, "api/amazon-ads/campaigns/auto",
                new
                {
                    AccountInfo = accountInfo,
                    Market = market,
                    FormVersion = formVersion,
                    FormData = formData,
                    FeatureFlags = featureFlags ?? new JObject()
                }, TimeSpan.FromMinutes(2));
            return response.Data;
        }

        public async Task<ManualProductCampaignCreateResult> CreateManualProductCampaignAsync(
            AmazonAdsAccountInfo accountInfo, AdsManualProductTargetingFormData formData,
            int formVersion = 185, JObject featureFlags = null)
        {
            var response = await SendAsync<ManualProductCampaignCreateResult>(
                HttpMethod.Post, "api/amazon-ads/campaigns/manual-product",
                new
                {
                    AccountInfo = accountInfo,
                    FormVersion = formVersion,
                    FormData = formData,
                    FeatureFlags = featureFlags ?? new JObject()
                }, TimeSpan.FromMinutes(2));
            return response.Data;
        }

        public async Task<ManualKeywordCampaignCreateResult> CreateManualKeywordCampaignAsync(
            AmazonAdsAccountInfo accountInfo, AdsManualFormData formData,
            int formVersion = 185, FeatureFlag featureFlags = null)
        {
            var response = await SendAsync<ManualKeywordCampaignCreateResult>(
                HttpMethod.Post, "api/amazon-ads/campaigns/manual-keyword",
                new
                {
                    AccountInfo = accountInfo,
                    FormVersion = formVersion,
                    FormData = formData,
                    FeatureFlags = featureFlags ?? new FeatureFlag()
                }, TimeSpan.FromMinutes(2));
            return response.Data;
        }

        public async Task<string> CreatePortfolioAsync(
            AmazonAdsAccountInfo accountInfo, string name)
        {
            var response = await SendAsync<string>(
                HttpMethod.Post, "api/amazon-ads/portfolios",
                new { AccountInfo = accountInfo, Name = name }, TimeSpan.FromMinutes(2));
            return response.Data;
        }

        public async Task<string> UpdatePortfolioAndGetExternalIdAsync(
            AmazonAdsAccountInfo accountInfo, string portfolioId, string name)
        {
            var response = await SendAsync<string>(
                HttpMethod.Put, "api/amazon-ads/portfolios/external-id",
                new { AccountInfo = accountInfo, PortfolioId = portfolioId, Name = name },
                TimeSpan.FromMinutes(2));
            return response.Data;
        }

        public async Task<string> GetHtmlAsync()
        {
            var response = await SendAsync<string>(HttpMethod.Get, "api/browser/html", null);
            return response.Data;
        }

        public async Task<object> ExecuteJavaScriptAsync(string script)
        {
            var response = await SendAsync<object>(HttpMethod.Post, "api/browser/javascript", new { Script = script });
            return response.Data;
        }

        public async Task<string> GetCookiesJsonAsync(bool includeValues = false)
        {
            using (var response = await _http.GetAsync("api/browser/cookies?includeValues=" + includeValues.ToString().ToLowerInvariant()))
            {
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException(Name + ": " + body);
                return body;
            }
        }

        public async Task<string> GetCookiesTextAsync()
        {
            using (var response = await _http.GetAsync("api/browser/cookies/text"))
            {
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException(Name + ": HTTP " + (int)response.StatusCode + " - " + body);
                return body;
            }
        }

        public async Task ScreenshotAsync(string filePath)
        {
            using (var response = await _http.GetAsync("api/browser/screenshot"))
            {
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException(Name + ": " + await response.Content.ReadAsStringAsync());

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(filePath, bytes);
            }
        }

        private async Task SendNoResultAsync(HttpMethod method, string path, object body)
        {
            await SendAsync<object>(method, path, body);
        }

        private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string path, object body, TimeSpan? requestTimeout = null)
        {
            using (var timeout = new CancellationTokenSource(requestTimeout ?? TimeSpan.FromMinutes(2)))
            using (var request = new HttpRequestMessage(method, path))
            {
                if (body != null)
                {
                    var json = JsonConvert.SerializeObject(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                using (var response = await _http.SendAsync(request, timeout.Token))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var traceId = response.Headers.TryGetValues("X-Agent-Trace-Id", out var traceValues)
                        ? traceValues.FirstOrDefault() : null;
                    var traceText = traceId == null ? "" : " (TraceId: " + traceId + ")";
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException(Name + ": HTTP " + (int)response.StatusCode +
                            traceText + " - " + json);

                    var result = JsonConvert.DeserializeObject<ApiResponse<T>>(json);
                    if (result == null) throw new InvalidOperationException(Name + ": invalid JSON response." + traceText);
                    if (!result.Success) throw new InvalidOperationException(Name + ": " + result.Error + traceText);
                    result.TraceId = traceId;
                    return result;
                }
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
