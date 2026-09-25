using System;
using System.Text.RegularExpressions;
using MyBrowserAgent.Models;

namespace MyBrowserAgent.Services
{
    public sealed class AmazonAdsAccountInfoService
    {
        private const string CampaignUrl = "https://advertising.amazon.com/cb";
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

        public AmazonAdsAccountInfo GetAccountInfo(BrowserService browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            // PageSource includes the page's inline configuration scripts. The Chrome profile
            // provides the logged-in session; no Cookie header or separate HTTP request is used.
            var html = browser.ReadPageSourceInTemporaryTab(
                CampaignUrl,
                source => !string.IsNullOrEmpty(Read(source, "entityId")),
                15);
            var info = Parse(html);
            if (string.IsNullOrEmpty(info.EntityId))
                throw new InvalidOperationException(
                    "EntityId was not found on the Amazon Ads page; check the Chrome login or page format.");
            return info;
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
                EntityId = Read(html, "entityId"),
                GlobalAccountId = Read(html, "globalAccountId")
            };
        }

        private static string Read(string html, string name)
        {
            var pattern = @"\b" + Regex.Escape(name) + @"\s*[:=]\s*(['""])(?<value>[^'""\r\n]+)\1";
            var match = Regex.Match(html, pattern, RegexOptions.None, RegexTimeout);
            return match.Success ? match.Groups["value"].Value : null;
        }
    }
}
