using System.Collections.Generic;
using Newtonsoft.Json;

namespace DesktopController
{
    public sealed class AmazonAdsTargetBidUpdateItem
    {
        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        [JsonProperty("countryCodes")]
        public IList<string> CountryCodes { get; set; }

        [JsonProperty("bid")]
        public string Bid { get; set; }
    }

    public sealed class AmazonAdsTargetBidUpdateResult
    {
        [JsonIgnore]
        public string TraceId { get; set; }

        [JsonProperty("updatedTargets")]
        public IList<UpdatedAmazonAdsTargetBid> UpdatedTargets { get; set; }

        [JsonProperty("failedTargetIds")]
        public IList<string> FailedTargetIds { get; set; }

        [JsonProperty("bulkUpdateSummary")]
        public AmazonAdsBidUpdateSummary BulkUpdateSummary { get; set; }
    }

    public sealed class UpdatedAmazonAdsTargetBid
    {
        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("bid")]
        public decimal? Bid { get; set; }

        [JsonProperty("servingStatus")]
        public string ServingStatus { get; set; }
    }

    public sealed class AmazonAdsBidUpdateSummary
    {
        [JsonProperty("invalidCount")]
        public int InvalidCount { get; set; }

        [JsonProperty("failedCount")]
        public int FailedCount { get; set; }

        [JsonProperty("successfulCount")]
        public int SuccessfulCount { get; set; }

        [JsonProperty("partiallyUpdatedCount")]
        public int PartiallyUpdatedCount { get; set; }
    }
}
