using System.Collections.Generic;

namespace MyBrowserAgent.Models
{
    public sealed class KeywordRecommendationRequest
    {
        public AmazonAdsAccountInfo AccountInfo { get; set; }
        public string Asin { get; set; }
        public List<string> Keywords { get; set; }
        public List<string> MatchTypes { get; set; }
    }

    public sealed class RecommendationRequest
    {
        public List<string> Asins { get; set; }
        public string RecommendationType { get; set; } = "KEYWORDS_FOR_ASINS";
        public List<Target> targets { get; set; }
        public int maxRecommendations { get; set; } = 0;
        public string biddingStrategy { get; set; } = "LEGACY_FOR_SALES";
    }

    public sealed class Target
    {
        public string Keyword { get; set; }
        public string MatchType { get; set; }
    }
}
