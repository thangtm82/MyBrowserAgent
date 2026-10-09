using System;

namespace MyBrowserAgent.Services
{
    internal static class AmazonAdsMarket
    {
        private const string UsCampaignUrl = "https://advertising.amazon.com/cb";
        private const string CaCampaignUrl = "https://advertising.amazon.ca/cb";

        public static string GetCampaignUrl(string market)
        {
            switch (Normalize(market))
            {
                case "US": return UsCampaignUrl;
                case "CA": return CaCampaignUrl;
                default: throw new ArgumentException("market must be US or CA.", nameof(market));
            }
        }

        public static string GetCurrencyCode(string market)
        {
            return Normalize(market) == "CA" ? "CAD" : "USD";
        }

        private static string Normalize(string market)
        {
            var value = market?.Trim().ToUpperInvariant();
            if (value == "US" || value == "CA") return value;
            throw new ArgumentException("market must be US or CA.", nameof(market));
        }
    }
}
