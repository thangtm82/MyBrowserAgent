using System;

namespace MyBrowserAgent.Services
{
    internal static class AmazonAdsMarket
    {
        private const string UsCampaignUrl = "https://advertising.amazon.com/cb";
        private const string CaCampaignUrl = "https://advertising.amazon.ca/cb";
        private const string UsMarketplaceId = "ATVPDKIKX0DER";
        private const string CaMarketplaceId = "A2EUQ1WTGCTBG2";

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

        public static void ValidateMarketplace(string market, string marketplaceId)
        {
            var normalized = Normalize(market);
            var expected = normalized == "CA" ? CaMarketplaceId : UsMarketplaceId;
            if (!string.Equals(marketplaceId, expected, StringComparison.Ordinal))
                throw new ArgumentException("AccountInfo.MarketplaceId does not match Market (" + normalized + ").");
        }

        private static string Normalize(string market)
        {
            var value = market?.Trim().ToUpperInvariant();
            if (value == "US" || value == "CA") return value;
            throw new ArgumentException("market must be US or CA.", nameof(market));
        }
    }
}
