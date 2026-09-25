using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using MyBrowserAgent.Models;

namespace MyBrowserAgent.Services
{
    // This service uses the supplied session cookies and does not start or change Selenium's browser.
    public sealed class AmazonAdsAccountInfoService
    {
        private static readonly HttpClient Http = CreateClient();
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        }

        public static Uri BuildUrl(string endpointUrl, string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentException("EntityId is required.", nameof(entityId));

            if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                (uri.Host != "advertising.amazon.com" && uri.Host != "advertising.amazon.ca") ||
                !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Fragment))
                throw new ArgumentException("EndpointUrl must be an HTTPS URL on advertising.amazon.com or advertising.amazon.ca.", nameof(endpointUrl));

            var builder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["entityId"] = entityId;
            builder.Query = query.ToString();
            return builder.Uri;
        }

        public async Task<AmazonAdsAccountInfo> FetchAsync(Uri url, string cookies)
        {
            if (string.IsNullOrWhiteSpace(cookies) || cookies.Contains("\r") || cookies.Contains("\n"))
                throw new ArgumentException("Cookies must contain a valid Cookie header value.", nameof(cookies));

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
                request.Headers.Add("Cookie", cookies);

                using (var response = await Http.SendAsync(request).ConfigureAwait(false))
                {
                    // Never follow a redirect to a sign-in page (or another host) with session cookies.
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                        throw new HttpRequestException("Amazon Ads redirected the request; check the endpoint and session cookies.");
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException("Amazon Ads returned HTTP " + (int)response.StatusCode + ".");

                    var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var info = Parse(html);
                    if (string.IsNullOrEmpty(info.Token) && string.IsNullOrEmpty(info.ClientId) &&
                        string.IsNullOrEmpty(info.CsrfToken) && string.IsNullOrEmpty(info.AdvertiserId) &&
                        string.IsNullOrEmpty(info.EntityId))
                        throw new InvalidOperationException("Account fields were not found; check login state or the Amazon Ads page format.");
                    return info;
                }
            }
        }

        public static AmazonAdsAccountInfo Parse(string html)
        {
            if (html == null) throw new ArgumentNullException(nameof(html));
            return new AmazonAdsAccountInfo
            {
                Token = Read(html, "token"),
                TraceId = Read(html, "xRayTraceId"),
                SegmentId = Read(html, "xRaySegmentId"),
                ClientId = Read(html, "clientId"),
                CsrfToken = Read(html, "csrfToken"),
                SessionId = Read(html, "sessionId"),
                PageHitRequestId = Read(html, "pageHitRequestId"),
                AdvertiserId = Read(html, "advertiserId"),
                EntityId = Read(html, "entityId")
            };
        }

        private static string Read(string html, string name)
        {
            // Both 'value' and "value" occur in Amazon Ads' embedded page configuration.
            var pattern = @"\b" + Regex.Escape(name) + @"\s*[:=]\s*(['""])(?<value>[^'""\r\n]+)\1";
            var match = Regex.Match(html, pattern, RegexOptions.None, RegexTimeout);
            return match.Success ? match.Groups["value"].Value : null;
        }
    }
}
