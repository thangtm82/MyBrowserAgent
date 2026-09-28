namespace MyBrowserAgent.Models
{
    public sealed class TargetBidUpdateRequest
    {
        public string TargetId { get; set; }
        public string CountryCode { get; set; }
        public decimal? Bid { get; set; }
    }
}
