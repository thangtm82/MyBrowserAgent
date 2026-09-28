# Filter Amazon Ads targets

`POST /api/amazon-ads/targets/filter` runs the `TargetsReport` request from `requestTarget.txt` inside the Agent's signed-in Selenium Chrome session. It opens `https://advertising.amazon.com/cb` in a temporary tab, reads the account identifiers from the page, and calls `/a9g-api-gateway/cm/dds/retrieveReport` with browser cookies. The original tab is restored afterward.

The request accepts inclusive ACoS bounds, inclusive calendar dates and a required page offset:

```json
{
  "MinAcos": 10,
  "MaxAcos": 40,
  "StartDate": "2026-09-01",
  "EndDate": "2026-09-28",
  "Offset": 0
}
```

Each call sends exactly one Amazon Ads request with `offsetPagination.size = 50` and the supplied `Offset`. The Agent returns only that page's `report.data` rows in the usual `ApiResult.Data` list; no matches returns `[]`. `report.numberOfRecords` is the total for the filter, not the number of rows returned by this call. The client chooses the next offset (for example, `0`, `50`, `100`) and makes another API call when it wants the next page. A mismatched offset or malformed row raises an error.

Desktop example (.NET Framework 4.7.2):

```csharp
var targets = await agent.FilterTargetsAsync(
    10, 40,
    new DateTime(2026, 9, 1), new DateTime(2026, 9, 28),
    offset: 0);

foreach (var target in targets)
    Console.WriteLine($"{target.Value<string>("targetId")}: {target.Value<decimal?>("acos")}");
```

The Agent writes the raw response for each API call under `logs\amazon-ads-targets-<UTC timestamp>-<run ID>-page-1.log` beside its executable. The log includes campaign and account data; keep it private. The API requires the usual `X-Api-Key`; it does not accept a Cookie header from the Desktop client.
