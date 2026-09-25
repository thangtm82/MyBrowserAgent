# Filter Amazon Ads campaigns

`POST /api/amazon-ads/campaigns/filter` opens `https://advertising.amazon.com/cb` in a temporary Selenium tab, reads the account identifiers embedded in that page, and executes `fetch` from the page's own origin. Chrome supplies the logged-in cookies. The original tab is restored afterward.

The API applies the conditions from `filterCamp.txt`: state `ENABLED` or `PAUSED`, the requested `campaignTargetingType`, an inclusive ACoS range, and program type `SP` or `SPONSORED_ADS_RETAILERS`. Dates are inclusive calendar dates in `yyyy-MM-dd` form. The campaign report retains the fields and sort order of the supplied request.

```json
{
  "TargetType": "AUTOMATIC",
  "MinAcos": 10,
  "MaxAcos": 40,
  "StartDate": "2026-09-01",
  "EndDate": "2026-09-25"
}
```

The response follows the usual `ApiResult` envelope. `Data` is the array from `report.data`; each campaign retains all fields provided by Amazon Ads, including `campaignId`, `campaignName`, `acos`, `spend`, and `sales`. A successful filter with no matches returns `[]`. The Agent follows a `tokenPagination.nextToken` continuation when present. If Amazon Ads does not return a continuation token, the Agent returns the rows in `report.data`; `numberOfRecords` alone is not used to infer that another page exists.

Desktop example (.NET Framework 4.7.2):

```csharp
var campaigns = await agent.FilterCampaignsAsync(
    "AUTOMATIC", 10, 40,
    new DateTime(2026, 9, 1), new DateTime(2026, 9, 25));

foreach (var campaign in campaigns)
    Console.WriteLine($"{campaign.Value<string>("campaignName")}: {campaign.Value<decimal?>("acos")}");
```

An Amazon Ads session must already be signed in within the Agent's Chrome profile. The endpoint uses the same `X-Api-Key` as the rest of the Agent. It does not accept cookies or authentication fields from the Desktop client. The supplied response example contains one complete page. If Amazon Ads returns more matching campaigns than the 50 requested per page but omits a continuation token, the response may contain only the rows returned by Amazon; pagination beyond that case requires a live response to establish the supported mechanism.
