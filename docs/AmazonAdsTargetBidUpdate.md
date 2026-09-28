# Update an Amazon Ads target bid

`PUT /api/amazon-ads/targets/bid` changes one target's bid using the Agent's signed-in Selenium Chrome session. The Agent uses client-supplied `AccountInfo` for Amazon Ads headers and runs a same-origin `PUT /a9g-api-gateway/cm/adsApi/targets/update`. Chrome sends session cookies. The current `campaign-manager` tab is reused; otherwise the Agent uses a temporary `/cb` tab.

The request requires the Agent's usual `X-Api-Key` and a JSON body:

```json
{
  "AccountInfo": {
    "EntityId": "ENTITY_ID",
    "GlobalAccountId": "GLOBAL_ACCOUNT_ID",
    "MarketplaceId": "ATVPDKIKX0DER",
    "ClientId": "CLIENT_ID",
    "CsrfToken": "CSRF_TOKEN"
  },
  "TargetId": "389597041525084",
  "CountryCode": "US",
  "Bid": 0.34
}
```

The Agent sends the captured Amazon Ads body shape: `[{"targetId":"389597041525084","countryCodes":["US"],"bid":"0.34"}]`. IDs remain strings and the bid is formatted with an invariant decimal separator. The endpoint rejects an empty target ID or country code, or a bid at or below zero.

`ApiResult.Data` contains the full Amazon response: `updatedTargets`, `failedTargetIds`, and `bulkUpdateSummary`. The Agent reports an error unless the requested target appears in `updatedTargets` with a bid, `successfulCount` is 1, and all failure or partial counts are zero.

Desktop example (.NET Framework 4.7.2):

```csharp
AmazonAdsAccountInfo info = await agent.StartAmazonAdsSessionAsync();
AmazonAdsTargetBidUpdateResult result =
    await agent.UpdateTargetBidAsync(info, "389597041525084", "US", 0.34m);

Console.WriteLine($"Updated target {result.UpdatedTargets[0].TargetId}: " +
    $"bid {result.UpdatedTargets[0].Bid}");
```

The client passes `AccountInfo` from the current Chrome login. It sends no Cookie header; the Agent uses the Chrome profile's session cookies.

## Update many targets

`PUT /api/amazon-ads/targets/bids` accepts a JSON object containing `AccountInfo` and `Targets`:

```json
{
  "AccountInfo": {
    "EntityId": "ENTITY_ID",
    "GlobalAccountId": "GLOBAL_ACCOUNT_ID",
    "MarketplaceId": "ATVPDKIKX0DER",
    "ClientId": "CLIENT_ID",
    "CsrfToken": "CSRF_TOKEN"
  },
  "Targets": [
    {"targetId":"389597041525084","countryCodes":["US"],"bid":"0.33"},
    {"targetId":"364231510296738","countryCodes":["US"],"bid":"1.09"}
  ]
}
```

Each entry needs a unique `targetId`, a nonempty `countryCodes` array, and a positive decimal `bid` string using a dot as the separator. The Agent sends only the `Targets` array in **one** Amazon Ads request through the signed-in Chrome session. `AccountInfo` supplies the headers; it must belong to the active Chrome login. `ApiResult.Data` contains the complete Amazon response. Inspect `failedTargetIds` and `bulkUpdateSummary` after every call: some targets may fail while others succeed. An HTTP error or malformed response is reported as an Agent error.

Desktop client example:

```csharp
var updates = new List<AmazonAdsTargetBidUpdateItem>
{
    new AmazonAdsTargetBidUpdateItem
    {
        TargetId = "389597041525084",
        CountryCodes = new List<string> { "US" },
        Bid = "0.33"
    },
    new AmazonAdsTargetBidUpdateItem
    {
        TargetId = "364231510296738",
        CountryCodes = new List<string> { "US" },
        Bid = "1.09"
    }
};
AmazonAdsAccountInfo info = await agent.StartAmazonAdsSessionAsync();
AmazonAdsTargetBidUpdateResult result = await agent.UpdateTargetBidsAsync(info, updates);
Console.WriteLine($"Updated: {result.BulkUpdateSummary.SuccessfulCount}; " +
    $"failed: {result.BulkUpdateSummary.FailedCount}");
```

`AccountInfo` must include `EntityId`, `GlobalAccountId`, `MarketplaceId`, `ClientId`, and `CsrfToken`. Missing fields return HTTP 400. The account info includes session credentials: keep it private, avoid logging the request body, and refresh it with `start-session` if the session changes.
