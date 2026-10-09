# Create a Sponsored Products manual product targeting campaign

Pass `?market=CA` to use `https://advertising.amazon.ca/cb`; omit it for US (`https://advertising.amazon.com/cb`). Use `AccountInfo` from the active Chrome session. Unsupported `market` values return HTTP 400; the market-to-`MarketplaceId` check is temporarily disabled.

`POST /api/amazon-ads/campaigns/manual-product` sends the supplied form to `/a9g-api-gateway/atlas/submit` in the signed-in Selenium Chrome session. Supply `X-Api-Key` and `AccountInfo` from `POST /api/amazon-ads/start-session`.

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
  "FeatureFlags": {},
  "FormData": {
    "CampaignName": "My product targeting campaign",
    "AdGroupName": "My ad group",
    "Budget": 20.00,
    "DefaultBid": 0.50,
    "StartDate": 1798761600000,
    "Portfolio": {
      "PortfolioId": "PORTFOLIO_ID",
      "Name": "My portfolio"
    },
    "Products": [
      { "Asin": "B012345678", "Merchant": { "Sku": "MY-SKU" } }
    ],
    "ProductTargets": [
      { "Asin": "B098765432", "Bid": { "Value": 0.50 }, "ProductMatchType": "ASIN_SAME_AS" }
    ]
  }
}
```

The Agent sends `formId: "sp"`, `experience: "campaign"`, `targetingType: "MANUAL"`, and `manualTargetingType: "PRODUCT"`. Form version defaults to 185. `FeatureFlags` is an arbitrary JSON object because the supplied `FeatureFlag` type was not defined; pass your real flags if necessary. Other form data fields, including placement adjustments and `NegativeProductTargets`, are available in `AdsManualProductTargetingFormData`. An optional `ProductTarget.Price` is also supported. The Agent fills `Portfolio.Budget.CurrencyCode` from the selected market (USD for US, CAD for CA), matching the supplied function.

The Agent verifies that the selected Chrome account matches `AccountInfo`, uses the current tab on the selected market's `/cb` URL, and sends the request with Chrome's cookies. `ApiResult.Data` has `StatusCode`, `Succeeded`, and raw `Content`. Check `Data.Succeeded` and the Amazon response content; an Agent HTTP 200 only means Amazon returned a response. The request is not retried automatically. If a timeout occurs, check Amazon Ads before submitting again.

Desktop example (.NET Framework 4.7.2):

```csharp
AmazonAdsAccountInfo info = await agent.StartAmazonAdsSessionAsync();
var form = new AdsManualProductTargetingFormData
{
    CampaignName = "My product targeting campaign",
    AdGroupName = "My ad group",
    Budget = 20m,
    DefaultBid = 0.50m,
    StartDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    Portfolio = new Portfolio { PortfolioId = "PORTFOLIO_ID", Name = "My portfolio" },
    Products = new List<Product> { new Product("B012345678", "MY-SKU") },
    ProductTargets = new List<ProductTarget>
    {
        new ProductTarget
        {
            Asin = "B098765432",
            Bid = new ProductTargetBid(0.50m),
            ProductMatchType = "ASIN_SAME_AS"
        }
    }
};
ManualProductCampaignCreateResult result =
    await agent.CreateManualProductCampaignAsync(info, form);
Console.WriteLine($"Amazon HTTP {result.StatusCode}: {result.Content}");
if (!result.Succeeded) throw new InvalidOperationException("Amazon rejected the campaign.");
```
