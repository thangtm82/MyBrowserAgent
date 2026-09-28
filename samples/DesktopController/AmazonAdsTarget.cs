using System.Collections.Generic;
using Newtonsoft.Json;

namespace DesktopController
{
    // One item in ApiResult.Data from POST /api/amazon-ads/targets/filter.
    public sealed class AmazonAdsTarget
    {
        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        [JsonProperty("targetState")]
        public string TargetState { get; set; }

        [JsonProperty("calculatedStatusName")]
        public string CalculatedStatusName { get; set; }

        [JsonProperty("calculatedStatusReasons")]
        public IList<string> CalculatedStatusReasons { get; set; }

        [JsonProperty("programType")]
        public string ProgramType { get; set; }

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("currencyCode")]
        public string CurrencyCode { get; set; }

        [JsonProperty("matchType")]
        public string MatchType { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("targetSecondary")]
        public string TargetSecondary { get; set; }

        [JsonProperty("asin")]
        public string Asin { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("mediaUrl")]
        public string MediaUrl { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("campaignId")]
        public string CampaignId { get; set; }

        [JsonProperty("campaignName")]
        public string CampaignName { get; set; }

        [JsonProperty("marketplaceId")]
        public string MarketplaceId { get; set; }

        [JsonProperty("isGlobalCampaign")]
        public bool? IsGlobalCampaign { get; set; }

        [JsonProperty("isDspCampaign")]
        public bool? IsDspCampaign { get; set; }

        [JsonProperty("campaignExternalId")]
        public string CampaignExternalId { get; set; }

        [JsonProperty("campaignState")]
        public string CampaignState { get; set; }

        [JsonProperty("adGroupId")]
        public string AdGroupId { get; set; }

        [JsonProperty("adGroupName")]
        public string AdGroupName { get; set; }

        [JsonProperty("adGroupExternalId")]
        public string AdGroupExternalId { get; set; }

        [JsonProperty("roas")]
        public decimal? Roas { get; set; }

        [JsonProperty("conversionRate")]
        public decimal? ConversionRate { get; set; }

        [JsonProperty("targetBid")]
        public decimal? TargetBid { get; set; }

        [JsonProperty("adGroupDefaultBid")]
        public decimal? AdGroupDefaultBid { get; set; }

        [JsonProperty("campaignBudget")]
        public decimal? CampaignBudget { get; set; }

        [JsonProperty("impressions")]
        public long? Impressions { get; set; }

        [JsonProperty("topOfSearchImpressionShare")]
        public decimal? TopOfSearchImpressionShare { get; set; }

        [JsonProperty("clicks")]
        public long? Clicks { get; set; }

        [JsonProperty("ctr")]
        public decimal? Ctr { get; set; }

        [JsonProperty("spend")]
        public decimal? Spend { get; set; }

        [JsonProperty("spendCoV")]
        public decimal? SpendCoV { get; set; }

        [JsonProperty("cpc")]
        public decimal? Cpc { get; set; }

        [JsonProperty("cpcCoV")]
        public decimal? CpcCoV { get; set; }

        [JsonProperty("orders")]
        public long? Orders { get; set; }

        [JsonProperty("sales")]
        public decimal? Sales { get; set; }

        [JsonProperty("salesCoV")]
        public decimal? SalesCoV { get; set; }

        [JsonProperty("acos")]
        public decimal? Acos { get; set; }
    }
}
