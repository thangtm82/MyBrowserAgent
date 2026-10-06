using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyBrowserAgent.Models
{
    public sealed class ManualProductCampaignCreateRequest
    {
        public AmazonAdsAccountInfo AccountInfo { get; set; }
        public int FormVersion { get; set; } = 185;
        public AdsManualProductTargetingFormData FormData { get; set; }
        public JObject FeatureFlags { get; set; } = new JObject();
    }

    public sealed class AdsManualProductTargetingFormRequest
    {
        public string FormId { get; set; } = "sp";
        public int FormVersion { get; set; } = 185;
        public string Experience { get; set; } = "campaign";
        public AdsManualProductTargetingFormData FormData { get; set; }
        public JObject FeatureFlags { get; set; } = new JObject();
    }

    public sealed class AdsManualProductTargetingFormData
    {
        public string AdGroupName { get; set; }
        public string Atv3CardType { get; set; } = "SHOW";
        public string ShopperCohortBidding { get; set; }
        public int SiteAmazonBusiness { get; set; }
        public bool SiteAmazonBusinessEnabled { get; set; }
        public int ProductDetail { get; set; }
        public int RestOfSearch { get; set; }
        public int TopOfSearch { get; set; }
        public string BiddingStrategy { get; set; } = "legacy";
        public decimal Budget { get; set; }
        public decimal BudgetRecommendation { get; set; }
        public string CampaignId { get; set; }
        public string PublicCampaignId { get; set; }
        public string CampaignName { get; set; }
        public decimal DefaultBid { get; set; }
        public string DraftId { get; set; }
        public long? EndDate { get; set; }
        public bool IsExpress { get; set; }
        public string ManualTargetingType { get; set; } = "PRODUCT";
        public List<string> NegativeProductTargets { get; set; } = new List<string>();
        public Portfolio Portfolio { get; set; }
        public List<ProductTarget> ProductTargets { get; set; }
        public long StartDate { get; set; }
        public List<Product> Products { get; set; }
        public string TargetingType { get; set; } = "MANUAL";
        public string Sites { get; set; }
    }

    public sealed class ProductTarget
    {
        public string Asin { get; set; }
        public ProductTargetBid Bid { get; set; }
        public string ProductMatchType { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ProductTargetPrice Price { get; set; }
    }

    public sealed class ProductTargetBid
    {
        public ProductTargetBid() { }
        public ProductTargetBid(decimal value) { Value = value; }
        public decimal Value { get; set; }
    }

    public sealed class ProductTargetPrice
    {
        public ProductTargetPrice() { }
        public ProductTargetPrice(decimal min, decimal max) { Min = min; Max = max; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
    }

    public sealed class ManualProductCampaignCreateResult
    {
        public int StatusCode { get; set; }
        public bool Succeeded { get; set; }
        public string Content { get; set; }
    }
}
