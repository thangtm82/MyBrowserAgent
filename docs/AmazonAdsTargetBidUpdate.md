# Update an Amazon Ads target bid

`PUT /api/amazon-ads/targets/bid` changes one target's bid using the Agent's signed-in Selenium Chrome session. The Agent opens `https://advertising.amazon.com/cb` in a temporary tab, obtains the account identifiers from the page, and runs a same-origin `PUT /a9g-api-gateway/cm/adsApi/targets/update`. Chrome sends its session cookies. The original tab is restored afterward.

The request requires the Agent's usual `X-Api-Key` and a JSON body:

```json
{
  "TargetId": "389597041525084",
  "CountryCode": "US",
  "Bid": 0.34
}
```

The Agent sends the captured Amazon Ads body shape: `[{"targetId":"389597041525084","countryCodes":["US"],"bid":"0.34"}]`. IDs remain strings and the bid is formatted with an invariant decimal separator. The endpoint rejects an empty target ID or country code, or a bid at or below zero.

`ApiResult.Data` contains the full Amazon response: `updatedTargets`, `failedTargetIds`, and `bulkUpdateSummary`. The Agent reports an error unless the requested target appears in `updatedTargets` with a bid, `successfulCount` is 1, and all failure or partial counts are zero.

Desktop example (.NET Framework 4.7.2):

```csharp
AmazonAdsTargetBidUpdateResult result =
    await agent.UpdateTargetBidAsync("389597041525084", "US", 0.34m);

Console.WriteLine($"Updated target {result.UpdatedTargets[0].TargetId}: " +
    $"bid {result.UpdatedTargets[0].Bid}");
```

The client sends no Cookie header. The Agent uses the Chrome profile's existing Amazon Ads login.
