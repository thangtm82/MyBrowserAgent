# Amazon Ads account information

`POST /api/amazon-ads/account-info` fetches the Amazon Ads campaign page and reads the account fields embedded in its HTML. It runs independently of Selenium and does not change the browser's current page.

The endpoint requires the same `X-Api-Key` as the browser API. Provide the campaign page URL and entity ID used by your US or CA account; the previous function stored these in external variables, so they are supplied per call. The service accepts only HTTPS URLs on `advertising.amazon.com` or `advertising.amazon.ca`.

```json
{
  "EndpointUrl": "https://advertising.amazon.com/YOUR-CAMPAIGN-PAGE-PATH",
  "EntityId": "YOUR_ENTITY_ID",
  "Cookies": "cookie1=value1; cookie2=value2"
}
```

Example from C# (.NET Framework 4.7.2):

```csharp
using (var agent = new BrowserAgentClient("VPS01", "http://10.0.0.11:5050/", "YOUR_API_KEY"))
{
    var info = await agent.GetAmazonAdsAccountInfoAsync(
        "https://advertising.amazon.com/YOUR-CAMPAIGN-PAGE-PATH",
        "YOUR_ENTITY_ID",
        "cookie1=value1; cookie2=value2");
    Console.WriteLine(info.AdvertiserId);
    Console.WriteLine(info.EntityId);
}
```

The response uses the usual `ApiResult` envelope. `Data` contains `Token`, `TraceId`, `SegmentId`, `ClientId`, `CsrfToken`, `SessionId`, `PageHitRequestId`, `AdvertiserId`, and `EntityId`. A field missing from the returned HTML is omitted from the JSON response (and is `null` in the C# DTO). If the page has none of the identifying fields, the endpoint returns an error so an expired login does not look like a successful extraction.

The `Cookies` field is a Cookie header string, not the JSON result of `/api/browser/cookies`. Use the cookies of the logged-in Amazon Ads session for the target domain. Keep the API on a private network and avoid logging request bodies or responses: both contain session credentials. The request is subject to a 30-second timeout and does not follow redirects.
