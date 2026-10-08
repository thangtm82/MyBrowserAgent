# Get Amazon Ads keyword recommendations

`POST /api/amazon-ads/targets/keywords/recommendations` calls the signed-in Chrome session's `/a9g-api-gateway/sp/targets/keywords/recommendations` endpoint. Send `X-Api-Key` and `AccountInfo` returned by `POST /api/amazon-ads/start-session`.

```json
{
  "AccountInfo": {
    "EntityId": "ENTITY_ID",
    "GlobalAccountId": "GLOBAL_ACCOUNT_ID",
    "MarketplaceId": "ATVPDKIKX0DER",
    "ClientId": "CLIENT_ID",
    "CsrfToken": "CSRF_TOKEN"
  },
  "Asin": "B012345678",
  "Keywords": ["running shoes", "walking shoes"],
  "MatchTypes": ["EXACT", "PHRASE"]
}
```

The Agent creates one target for each keyword and match type pair. For this example it sends four targets. The attached method does not define the `RecommendationRequest` and `Target` classes. The Agent constructs `Asins`, `targets`, `Keyword` and `MatchType` from the member names referenced by that method, and uses `application/vnd.spkeywordsrecommendation.v5+json`. Confirm the exact body keys against your working request if Amazon rejects it. Chrome supplies its signed-in cookies. The Agent checks the active account against `AccountInfo` and reuses a `/cb` tab when available.

On HTTP 200, the Agent flattens `keywordTargetList[*].bidInfo[*]` into `ApiResult.Data`, a list of `KeywordTarget` objects with `Keyword`, `MatchType`, and `Bid`. The bid is divided by 100 and rounded to two decimal places, matching the supplied function. If Amazon returns invalid JSON or an unsuccessful status, the Agent returns an error rather than a null list. An empty recommendation list returns `[]`.

.NET Framework desktop example:

```csharp
AmazonAdsAccountInfo accountInfo = await agent.StartAmazonAdsSessionAsync();
IList<KeywordTarget> recommendations = await agent.GetKeywordRecommendationsAsync(
    accountInfo, "B012345678",
    new List<string> { "running shoes", "walking shoes" },
    new List<string> { "EXACT", "PHRASE" });
foreach (var item in recommendations)
    Console.WriteLine($"{item.Keyword} / {item.MatchType}: {item.Bid:F2}");
```

`KeywordTarget` is the same typed model used by the manual keyword campaign API. No live Amazon Ads request is made during the build check.
