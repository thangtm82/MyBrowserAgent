using System.Collections.Generic;

namespace MyBrowserAgent.Models
{
    public sealed class TargetBidBulkUpdateRequest
    {
        public AmazonAdsAccountInfo AccountInfo { get; set; }
        public IList<TargetBidUpdateItem> Targets { get; set; }
    }
}
