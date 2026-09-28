# Filter Amazon Ads targets

`POST /api/amazon-ads/targets/filter` runs the `TargetsReport` request from `requestTarget.txt` inside the Agent's signed-in Selenium Chrome session. It opens `https://advertising.amazon.com/cb` in a temporary tab, reads the account identifiers from the page, and calls `/a9g-api-gateway/cm/dds/retrieveReport` with browser cookies. The original tab is restored afterward.

The request accepts inclusive ACoS bounds, inclusive calendar dates and an optional starting offset:

```json
{
  "MinAcos": 10,
  "MaxAcos": 40,
  "StartDate": "2026-09-01",
  "EndDate": "2026-09-28",
  "Offset": 0
}
```

`Offset` defaults to 0. The Agent requests 50 targets per page and advances `reportConfig.offsetPagination.offset` until it has retrieved the remaining rows indicated by `report.numberOfRecords`. The response uses the usual `ApiResult` envelope; `Data` is a list of the full objects from `report.data`, including `targetId`, `target`, `campaignId`, `acos`, `spend`, and `sales`. No matches returns an empty list. A missing or inconsistent page raises an error instead of returning an incomplete list.

Desktop example (.NET Framework 4.7.2):

```csharp
var targets = await agent.FilterTargetsAsync(
    10, 40,
    new DateTime(2026, 9, 1), new DateTime(2026, 9, 28));

foreach (var target in targets)
    Console.WriteLine($"{target.Value<string>("targetId")}: {target.Value<decimal?>("acos")}");
```

The Agent writes each raw response page under `logs\amazon-ads-targets-<UTC timestamp>-<run ID>-page-1.log` beside its executable. The log includes campaign and account data; keep it private. The API requires the usual `X-Api-Key`; it does not accept a Cookie header from the Desktop client.
