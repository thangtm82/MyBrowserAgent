namespace MyBrowserAgent.Models
{
    public sealed class PortfolioExternalIdRequest
    {
        public AmazonAdsAccountInfo AccountInfo { get; set; }
        public string PortfolioId { get; set; }
        public string Name { get; set; }
    }
}
