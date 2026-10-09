using System;

namespace MyBrowserAgent.Models
{
    public sealed class TargetFilterRequest
    {
        public AmazonAdsAccountInfo AccountInfo { get; set; }
        public string Market { get; set; } = "US";
        public decimal? MinAcos { get; set; }
        public decimal? MaxAcos { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Offset { get; set; }
        public string MatchType { get; set; }
    }
}
