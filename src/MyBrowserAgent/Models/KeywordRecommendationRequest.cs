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
