namespace MyBrowserAgent.Models
{
    public sealed class PortfolioCreateRequest
    {
        public AmazonAdsAccountInfo AccountInfo { get; set; }
        public string Name { get; set; }
    }
}
