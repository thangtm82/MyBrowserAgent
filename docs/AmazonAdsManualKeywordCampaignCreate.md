# Create a Sponsored Products manual keyword campaign

`POST /api/amazon-ads/campaigns/manual-keyword` submits the manual keyword form to `/a9g-api-gateway/atlas/submit` using the signed-in Selenium Chrome session. Send `X-Api-Key` and `AccountInfo` returned by `POST /api/amazon-ads/start-session`.

```json
{
  "AccountInfo": {
    "EntityId": "ENTITY_ID",
    "GlobalAccountId": "GLOBAL_ACCOUNT_ID",
    "MarketplaceId": "ATVPDKIKX0DER",
    "ClientId": "CLIENT_ID",
    "CsrfToken": "CSRF_TOKEN",
    "AdvertiserId": "ADVERTISER_ID",
    "PageHitRequestId": "PAGE_HIT_REQUEST_ID",
    "SessionId": "SESSION_ID"
  },
  "FormVersion": 185,
  "FormData": {
    "CampaignName": "My manual keyword campaign",
    "AdGroupName": "My ad group",
    "Budget": 20.00,
    "DefaultBid": 0.50,
    "StartDate": 1798761600000,
    "Portfolio": { "PortfolioId": "PORTFOLIO_ID", "Name": "My portfolio" },
    "Products": [{ "Asin": "B012345678", "Merchant": { "Sku": "MY-SKU" } }],
    "KeywordTargets": [{ "Keyword": "example keyword", "MatchType": "EXACT", "Bid": 0.50 }]
  }
}
```

`FormData.KeywordTargets` is a list of `KeywordTarget` objects with `Keyword`, `MatchType`, and numeric `Bid`, as specified in the supplied class. The rest of the payload is modeled by `AdsManualFormData`, `ShopperCohortBidding`, and `FeatureFlag`. `FeatureFlags` defaults to all 35 flags and values from your supplied class, and you can override them. The upstream request has `formId: "sp"`, `experience: "campaign"`, `targetingType: "MANUAL"`, `manualTargetingType: "KEYWORD"`, and `formVersion: 185` by default.

The Agent fills `Portfolio.Budget.CurrencyCode` from the selected marketplace (USD for US, CAD for CA), confirms that the Chrome account matches `AccountInfo`, and uses Chrome's cookies. It uses the current tab, navigating it to `https://advertising.amazon.com/cb` if needed, and leaves it open. `ApiResult.Data` contains `StatusCode`, `Succeeded`, and Amazon's raw `Content`; check `Succeeded` and the response content. The Agent does not retry submissions automatically, so check Amazon Ads before retrying after a timeout.

Desktop example (.NET Framework 4.7.2):

```csharp
AmazonAdsAccountInfo info = await agent.StartAmazonAdsSessionAsync();
var form = new AdsManualFormData
{
    CampaignName = "My manual keyword campaign",
    AdGroupName = "My ad group",
    Budget = 20m,
    DefaultBid = 0.50,
    StartDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    Portfolio = new Portfolio { PortfolioId = "PORTFOLIO_ID", Name = "My portfolio" },
    Products = new List<Product> { new Product("B012345678", "MY-SKU") },
    KeywordTargets = new List<KeywordTarget>
    {
        new KeywordTarget { Keyword = "example keyword", MatchType = "EXACT", Bid = 0.50m }
    }
};
ManualKeywordCampaignCreateResult result =
    await agent.CreateManualKeywordCampaignAsync(info, form);
Console.WriteLine($"Amazon HTTP {result.StatusCode}: {result.Content}");
if (!result.Succeeded) throw new InvalidOperationException("Amazon rejected the campaign.");
```
