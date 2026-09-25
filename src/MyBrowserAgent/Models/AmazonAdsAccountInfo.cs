namespace MyBrowserAgent.Models
{
    public sealed class AmazonAdsAccountInfoRequest
    {
        public string EndpointUrl { get; set; }
        public string EntityId { get; set; }
        public string Cookies { get; set; }
    }

    public sealed class AmazonAdsAccountInfo
    {
        public string Token { get; set; }
        public string TraceId { get; set; }
        public string SegmentId { get; set; }
        public string ClientId { get; set; }
        public string CsrfToken { get; set; }
        public string SessionId { get; set; }
        public string PageHitRequestId { get; set; }
        public string AdvertiserId { get; set; }
        public string EntityId { get; set; }
    }
}
