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

The Agent creates one target for each keyword and match type pair. For this example it sends four targets. It serializes the supplied `RecommendationRequest` defaults and the keyword/match-type pairs with the original property casing. The upstream body is:

```json
{
  "Asins": ["B012345678"],
  "RecommendationType": "KEYWORDS_FOR_ASINS",
  "targets": [
    { "Keyword": "running shoes", "MatchType": "EXACT" },
    { "Keyword": "running shoes", "MatchType": "PHRASE" },
    { "Keyword": "walking shoes", "MatchType": "EXACT" },
    { "Keyword": "walking shoes", "MatchType": "PHRASE" }
  ],
  "maxRecommendations": 0,
  "biddingStrategy": "LEGACY_FOR_SALES"
}
```

The attachment still omits the `Target` class; the Agent models the two members used by the original method, `Keyword` and `MatchType`. It sends `application/vnd.spkeywordsrecommendation.v5+json`. Chrome supplies its signed-in cookies. The Agent checks the active account against `AccountInfo` and reuses a `/cb` tab when available.

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
