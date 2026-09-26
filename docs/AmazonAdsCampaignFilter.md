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

The response follows the usual `ApiResult` envelope. `Data` is the array from `report.data`; each campaign retains all fields provided by Amazon Ads, including `campaignId`, `campaignName`, `acos`, `spend`, and `sales`. A successful filter with no matches returns `[]`. The Agent reads `report.tokenPagination.nextPageToken` and places its value in both `reportConfig.tokenPagination.nextToken` and `reportConfig.tokenPagination.nextPageToken` for the following request, matching the supplied `page2request.txt`. It also accepts `nextToken` or `token` in the response for compatibility. If Amazon Ads does not return a continuation token, the Agent returns the rows in `report.data`; `numberOfRecords` alone is not used to infer that another page exists.

Each call writes the complete, unmodified response body from every Amazon Ads report page to a separate log file on the Agent VPS. For example, if the Agent runs at `C:\\BrowserAgent\\MyBrowserAgent.exe`, look in `C:\\BrowserAgent\\logs\\amazon-ads-campaigns-<UTC timestamp>-<run ID>-page-1.log`. The file contains a UTC timestamp, page number, HTTP status, and `ResponseBody:` with the raw Amazon response (including `numberOfRecords`, `data`, and pagination fields). HTTP error responses are logged before the request raises an error. Request headers and cookies are not written. These files contain campaign and account data, so keep them private and delete them when no longer needed.

Desktop example (.NET Framework 4.7.2):

```csharp
var campaigns = await agent.FilterCampaignsAsync(
    "AUTOMATIC", 10, 40,
    new DateTime(2026, 9, 1), new DateTime(2026, 9, 25));

foreach (var campaign in campaigns)
    Console.WriteLine($"{campaign.Value<string>("campaignName")}: {campaign.Value<decimal?>("acos")}");
```

An Amazon Ads session must already be signed in within the Agent's Chrome profile. The endpoint uses the same `X-Api-Key` as the rest of the Agent. It does not accept cookies or authentication fields from the Desktop client. The live response with 51 matching campaigns has 50 rows in the first page and a `nextPageToken`. The next request uses the token with the pagination fields shown above; each response page contributes its `report.data` rows to the returned list. If Amazon Ads omits a continuation token, the Agent returns only the rows received.
