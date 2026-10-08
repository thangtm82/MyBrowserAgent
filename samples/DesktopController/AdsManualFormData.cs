using System.Collections.Generic;

namespace DesktopController
{
    public sealed class AdsManualFormData
    {
        public string AdGroupName { get; set; }
        public string Atv3CardType { get; set; }
        public ShopperCohortBidding ShopperCohortBidding { get; set; } = new ShopperCohortBidding();
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
        public List<KeywordTarget> KeywordTargets { get; set; }
        public int KeywordSuggestionsBelow { get; set; } = 1;
        public int KeywordSuggestionsWithin { get; set; } = 2;
        public int KeywordSuggestionsAbove { get; set; }
        public string ManualTargetingType { get; set; } = "KEYWORD";
        public List<string> NegativeKeywordTargets { get; set; }
        public Portfolio Portfolio { get; set; }
        public long StartDate { get; set; }
        public List<Product> Products { get; set; }
        public string TargetingType { get; set; } = "MANUAL";
        public string Sites { get; set; } = "AMAZON_AND_BEYOND";
    }

    public sealed class KeywordTarget
    {
        public string Keyword { get; set; }
        public string MatchType { get; set; }
        public double Bid { get; set; }
    }

    public sealed class FeatureFlag
    {
        public string SPONSORED_PRODUCTS_FW_US_VENDOR_EXPANSION_PARENT_1000775 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_FW_US_VENDOR_EXPANSION_EXPERIMENT_1000776 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_FW_NONUS_EXPANSION_PARENT_1000773 { get; set; } = "";
        public string SPONSORED_PRODUCTS_FW_NONUS_EXPANSION_EXPERIMENT_1000774 { get; set; } = "";
        public string CC_ATLAS_SP_STORM_MIGRATION_1256966 { get; set; } = "";
        public string SPONSORED_PRODUCTS_TAC_GUIDED_EXPERIENCE_1043219 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_CC_CM_NEGATIVE_TARGETING_PARITY_1261445 { get; set; } = "";
        public string SPONSORED_PRODUCTS_AMAZON_BUSINESS_EXCLUSIVE_CAMPAIGN_1177538 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_AMAZON_HAUL_EXCLUSIVE_CAMPAIGN_1180883 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_CC_BID_RULES_LAUNCH_1153463 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_CC_BID_RULES_EXPERIMENT_1153460 { get; set; } = "C";
        public string SPONSORED_PRODUCTS_UCM_RULES_MANAGEMENT_LAUNCH_1037948 { get; set; } = "C";
        public string SPONSORED_PRODUCTS_UCM_RULES_MANAGEMENT_EXPERIMENT_1037949 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_CC_TPG_RULES_LAUNCH_1178981 { get; set; } = "";
        public string SPONSORED_PRODUCTS_CC_TPG_RULES_EXPERIMENT_1178994 { get; set; } = "";
        public string SPONSORED_PRODUCTS_CC_GLOBAL_ADS_PARENT_963590_GLOBALID { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_CC_GLOBAL_ADS_EXPERIMENT_963614_GLOBALID { get; set; } = "T1";
        public string UCM_GLOBAL_CAMPAIGNS_1144875_GLOBALID { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_SPECTRUM_1202524 { get; set; } = "C";
        public string SPONSORED_PRODUCTS_SPECTRUM_EXPERIMENT_1202529 { get; set; } = "C";
        public string SPONSORED_PRODUCTS_1194443 { get; set; } = "";
        public string SPONSORED_PRODUCTS_KG_CATEGORY_DEFAULT_SELECTION_1082734 { get; set; } = "T1";
        public string SSPA_SP_CUSTOM_TEXT_327148 { get; set; } = "";
        public string CC_SP_UNIFIED_MIGRATION_STAGE_ONE_1256918 { get; set; } = "";
        public string CC_SP_UNIFIED_MIGRATION_STAGE_TWO_1282000 { get; set; } = "";
        public string AX_SP_SITE_AMAZON_BUSINESS_893888 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_HIGH_VALUE_SHOPPER_BIDDING_975693 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_MM_LOCALIZE_KEYWORDS_ENABLE_PERFORMANCE_MODE_LAUNCH_1207652 { get; set; } = "T1";
        public string CC_MM_CREATE_TAKT_EVENTS_574214 { get; set; } = "";
        public string SPONSORED_PRODUCTS_FW_US_PAT_EXPANSION_PARENT_1000778 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_FW_US_PAT_EXPANSION_EXPERIMENT_1000779 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_964568 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_CC_GLOBAL_ADS_PARENT_963590 { get; set; } = "T1";
        public string SPONSORED_PRODUCTS_CC_GLOBAL_ADS_EXPERIMENT_963614 { get; set; } = "T1";
        public string UCM_GLOBAL_CAMPAIGNS_1144875 { get; set; } = "T1";
    }

    public sealed class ShopperCohortBidding
    {
        public string ShopperCohortType { get; set; } = "AUDIENCE_SEGMENT";
    }

    public sealed class ManualKeywordCampaignCreateResult
    {
        public int StatusCode { get; set; }
        public bool Succeeded { get; set; }
        public string Content { get; set; }
    }
}
