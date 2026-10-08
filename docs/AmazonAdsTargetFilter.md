# Filter Amazon Ads targets

`POST /api/amazon-ads/targets/filter` runs the `TargetsReport` request from `requestTarget.txt` inside the signed-in Selenium Chrome session. It uses client-supplied `AccountInfo` in the Amazon Ads headers and browser cookies for the request. The current `/cb` tab is reused; otherwise the Agent uses a temporary `/cb` tab.

The request accepts inclusive ACoS bounds, inclusive calendar dates, a required page offset, and optional `MatchType`:

```json
{
  "AccountInfo": {
    "EntityId": "ENTITY_ID",
    "GlobalAccountId": "GLOBAL_ACCOUNT_ID",
    "MarketplaceId": "ATVPDKIKX0DER",
    "ClientId": "CLIENT_ID",
    "CsrfToken": "CSRF_TOKEN"
  },
  "MinAcos": 10,
  "MaxAcos": 40,
  "StartDate": "2026-09-01",
  "EndDate": "2026-09-28",
  "Offset": 0,
  "MatchType": "TARGETING_EXPRESSION_PREDEFINED, ANOTHER_MATCH_TYPE"
}
```

`MatchType` accepts one value or comma-separated values. For example, `"TARGETING_EXPRESSION_PREDEFINED, ANOTHER_MATCH_TYPE"` adds `{"field":"matchType","values":["TARGETING_EXPRESSION_PREDEFINED","ANOTHER_MATCH_TYPE"],"comparisonOperator":"IN","not":false}` to `filter.and`. Replace `ANOTHER_MATCH_TYPE` with a valid value from your account. The Agent trims each value, skips empty entries and removes exact duplicates while preserving order. A missing value or a string containing only spaces and commas adds no match type condition.

Each call sends exactly one Amazon Ads request with `offsetPagination.size = 50` and the supplied `Offset`. The Agent returns only that page's `report.data` rows in the usual `ApiResult.Data` list; no matches returns `[]`. `report.numberOfRecords` is the total for the filter, not the number of rows returned by this call. The client chooses the next offset (for example, `0`, `50`, `100`) and makes another API call when it wants the next page. A mismatched offset or malformed row raises an error.

Desktop example (.NET Framework 4.7.2):

```csharp
AmazonAdsAccountInfo info = await agent.StartAmazonAdsSessionAsync();
IList<AmazonAdsTarget> targets = await agent.FilterTargetsTypedAsync(
    info, 10, 40,
    new DateTime(2026, 9, 1), new DateTime(2026, 9, 28),
    offset: 0,
    matchType: "TARGETING_EXPRESSION_PREDEFINED, ANOTHER_MATCH_TYPE");

foreach (var target in targets)
    Console.WriteLine($"{target.TargetId}: {target.Acos}");
```

The DesktopController sample declares `AmazonAdsTarget` with all 41 fields from the supplied `responseTarget.txt`. `FilterTargetsTypedAsync` deserializes `ApiResult.Data` directly into `IList<AmazonAdsTarget>`. `FilterTargetsAsync` remains available when the caller wants the original `JObject` rows.

The Agent writes the raw response for each API call under `logs\amazon-ads-targets-<UTC timestamp>-<run ID>-page-1.log` beside its executable. The log includes campaign and account data; keep it private. The API requires `AccountInfo` from the current Chrome login (for example, returned by `start-session`) and the usual `X-Api-Key`. Chrome sends cookies; the Desktop client does not. Treat `AccountInfo` as sensitive and avoid logging it.
