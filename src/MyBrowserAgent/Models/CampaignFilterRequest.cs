using System;

namespace MyBrowserAgent.Models
{
    public sealed class CampaignFilterRequest
    {
        public string TargetType { get; set; }
        public decimal? MinAcos { get; set; }
        public decimal? MaxAcos { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
