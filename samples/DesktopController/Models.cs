namespace DesktopController
{
    public sealed class VpsConfig
    {
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
    }

    public sealed class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
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

    public sealed class AgentStatus
    {
        public bool Running { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Error { get; set; }
    }
}
