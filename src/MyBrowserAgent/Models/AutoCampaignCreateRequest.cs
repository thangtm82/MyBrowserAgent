using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyBrowserAgent.Models
{
    public sealed class AutoCampaignCreateRequest
    {
        public AmazonAdsAccountInfo AccountInfo { get; set; }
        public string Market { get; set; } = "US";
        public int FormVersion { get; set; } = 186;
        public AdsAutoFormData FormData { get; set; }
        public JObject FeatureFlags { get; set; } = new JObject();
    }

    public sealed class AdsAutoFormRequest
    {
        public string FormId { get; set; } = "sp";
        public int FormVersion { get; set; } = 186;
        public string Experience { get; set; } = "campaign";
        public AdsAutoFormData FormData { get; set; }
        public JObject FeatureFlags { get; set; } = new JObject();
    }

    public sealed class AdsAutoFormData
    {
        public string AdGroupName { get; set; }
        public AutomaticTargeting AutomaticTargetingCloseMatch { get; set; }
        public AutomaticTargeting AutomaticTargetingComplements { get; set; }
        public AutomaticTargeting AutomaticTargetingLooseMatch { get; set; }
        public AutomaticTargeting AutomaticTargetingSubstitutes { get; set; }
        public AutomaticTargetingGroups AutomaticTargetingGroups { get; set; }
        public string AutomaticTargetingType { get; set; } = "TARGETING_GROUP";
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
        public DateTime? EndDate { get; set; }
        public bool IsExpress { get; set; } = true;
        public int MultiMarketplaceCampaignCount { get; set; }
        public int MultiMarketplaceTotalCampaignCount { get; set; }
        public List<string> NegativeKeywordTargets { get; set; }
        public List<string> NegativeProductTargets { get; set; }
        public Portfolio Portfolio { get; set; }
        public List<Product> Products { get; set; }
        public long StartDate { get; set; }
        public string TargetingType { get; set; } = "AUTO";
        public string Sites { get; set; } = "AMAZON_BUSINESS";
        public bool HasMultipleMarketplaces { get; set; } = true;
    }

    public sealed class AutomaticTargetingGroups
    {
        [JsonProperty("close-match")]
        public AutomaticTargeting CloseMatch { get; set; }
        [JsonProperty("loose-match")]
        public AutomaticTargeting LooseMatch { get; set; }
        [JsonProperty("substitutes")]
        public AutomaticTargeting Substitutes { get; set; }
        [JsonProperty("complements")]
        public AutomaticTargeting Complements { get; set; }
    }

    public sealed class AutomaticTargeting
    {
        public bool IsActive { get; set; }
        public decimal Bid { get; set; }
    }

    public sealed class Portfolio
    {
        public Budget Budget { get; set; } = new Budget();
        public bool InBudget { get; set; } = true;
        public string Name { get; set; }
        public string PortfolioId { get; set; }
        public string State { get; set; } = "ENABLED";
    }

    public sealed class Budget
    {
        public string CurrencyCode { get; set; }
        public string Policy { get; set; } = "NO_CAP";
    }

    public class ProductAds
    {
        public string Asin { get; set; }
    }

    public sealed class Product : ProductAds
    {
        public Product() { }

        public Product(string asin, string sku)
        {
            Asin = asin;
            Merchant = new Merchant { Sku = sku };
        }

        public Merchant Merchant { get; set; }
    }

    public sealed class Merchant
    {
        public string Sku { get; set; }
    }

    public sealed class AutoCampaignCreateResult
    {
        public int StatusCode { get; set; }
        public bool Succeeded { get; set; }
        public string Content { get; set; }
    }
}
