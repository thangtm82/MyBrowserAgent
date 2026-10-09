# Create an Amazon Ads automatic campaign

`POST /api/amazon-ads/campaigns/auto` submits the Sponsored Products form to `/a9g-api-gateway/atlas/submit` from the signed-in Selenium Chrome session. The browser supplies session cookies and Origin; the Agent sends the Amazon Ads headers from `AccountInfo`. The Agent uses the current tab, navigating it to `/cb` on `advertising.amazon.com` (US) or `advertising.amazon.ca` (CA) if needed, and leaves it open.

Request `AmazonAdsAccountInfo` with `POST /api/amazon-ads/start-session` first. Send the Agent's `X-Api-Key` header and this JSON body:

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
  "Market": "US",
  "FormVersion": 186,
  "FeatureFlags": {},
  "FormData": {
    "CampaignName": "My auto campaign",
    "AdGroupName": "My ad group",
    "Budget": 20.00,
    "DefaultBid": 0.50,
    "StartDate": 1798761600000,
    "Products": [
      { "Asin": "B012345678", "Merchant": { "Sku": "MY-SKU" } }
    ],
    "AutomaticTargetingGroups": {
      "close-match": { "IsActive": true, "Bid": 0.50 },
      "loose-match": { "IsActive": true, "Bid": 0.50 },
      "substitutes": { "IsActive": true, "Bid": 0.50 },
      "complements": { "IsActive": true, "Bid": 0.50 }
    }
  }
}
```

Use the actual ASIN, SKU, and start-date value expected by your existing form. The `ProductAds` class defines `Asin`, and `Product` adds `Merchant.Sku`. The Agent preserves the supplied form fields, supplies the defaults from `classes.txt`, serializes the upstream body with camel-case names, and keeps the hyphenated automatic targeting group keys. The upstream body has `formId: "sp"` and `experience: "campaign"`. `FormVersion` defaults to 186 and can be updated by the client if Amazon's form version changes. Because `FeatureFlag` was not defined in the supplied file, `FeatureFlags` accepts arbitrary JSON; pass the flags used by your original form if an empty object is insufficient.

Set `Market` to `CA` and start a CA session for Canadian campaigns. `Market` defaults to `US`; the Agent requires the matching US (`ATVPDKIKX0DER`) or CA (`A2EUQ1WTGCTBG2`) `AccountInfo.MarketplaceId`. If `FormData.Portfolio` is supplied and its budget has no currency, the Agent fills USD or CAD from the marketplace; a conflicting currency is rejected. The active Chrome page must show the same entity, global account, marketplace, and advertiser ID as `AccountInfo`.

`ApiResult.Data` contains `StatusCode`, `Succeeded`, and raw `Content` from Amazon. `ApiResult.Success` means that the Agent received an HTTP response; check `Data.Succeeded` for Amazon's result. Browser/network failures return an Agent error. The request is **not retried automatically**: after a timeout, check for a created campaign before sending it again.

.NET Framework desktop example:

```csharp
AmazonAdsAccountInfo info = await agent.StartAmazonAdsSessionAsync();
var form = new AdsAutoFormData
{
    CampaignName = "My auto campaign",
    AdGroupName = "My ad group",
    Budget = 20m,
    DefaultBid = 0.50m,
    StartDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    Products = new List<Product> { new Product("B012345678", "MY-SKU") },
    AutomaticTargetingGroups = new AutomaticTargetingGroups
    {
        CloseMatch = new AutomaticTargeting { IsActive = true, Bid = 0.50m },
        LooseMatch = new AutomaticTargeting { IsActive = true, Bid = 0.50m },
        Substitutes = new AutomaticTargeting { IsActive = true, Bid = 0.50m },
        Complements = new AutomaticTargeting { IsActive = true, Bid = 0.50m }
    }
};
AutoCampaignCreateResult result = await agent.CreateAutoCampaignAsync(info, form);
// For CA: start with StartAmazonAdsSessionAsync("CA") and pass market: "CA".
Console.WriteLine($"Amazon HTTP {result.StatusCode}: {result.Content}");
if (!result.Succeeded) throw new InvalidOperationException("Amazon rejected the campaign.");
```
