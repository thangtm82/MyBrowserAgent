# Amazon Ads account information

`POST /api/amazon-ads/account-info` uses the Agent's Selenium Chrome profile to open the `/cb` URL for `?market=US` (default) or `?market=CA`, read its page source, and extract the account fields. The request has no body. It requires the usual `X-Api-Key` header and an Amazon Ads session already signed in within the Agent's Chrome profile.

The operation uses the current tab and navigates it to the selected market's `/cb` URL when needed. It waits up to 15 seconds for `EntityId` and leaves the tab open, including when the operation fails. It serializes access to Chrome with the existing browser commands. It does not need a Cookie header, campaign URL, or entity ID from Desktop.

Example (.NET Framework 4.7.2):

```csharp
using (var agent = new BrowserAgentClient("VPS01", "http://10.0.0.11:5050/", "YOUR_API_KEY"))
{
    var info = await agent.GetAmazonAdsAccountInfoAsync();
    Console.WriteLine(info.AdvertiserId);
    Console.WriteLine(info.EntityId);
    Console.WriteLine(info.GlobalAccountId);
    Console.WriteLine(info.MarketplaceId);
    Console.WriteLine(info.Currency); // USD or CAD
}
```

`account-info` also accepts `?market=CA`; the desktop client can call `GetAmazonAdsAccountInfoAsync("CA")`. The request has no body.

`Data` contains `Token`, `TraceId`, `SegmentId`, `ClientId`, `CsrfToken`, `SessionId`, `PageHitRequestId`, `AdvertiserId`, `EntityId`, `GlobalAccountId`, `MarketplaceId`, and `Currency`. `Currency` is derived from the `market` parameter (US → USD, CA → CAD), not from page HTML. Any other field absent from the HTML is omitted from the JSON (and `null` in the C# DTO). If `EntityId` is missing, the API returns an error indicating that the Chrome login or Amazon Ads page format should be checked.

These fields include session credentials. Keep the API on a private network and avoid logging the response.

## Start a new Amazon Ads session

`POST /api/amazon-ads/start-session` has no body and requires the usual `X-Api-Key`. Its optional query parameter `market` accepts `US` (default) or `CA`. The Agent stops its Chrome/WebDriver session, launches a new one with the **same persistent Chrome profile**, and navigates to `https://advertising.amazon.com/cb` for US or `https://advertising.amazon.ca/cb` for CA. Unsupported values return HTTP 400 without restarting Chrome. Existing tabs in the Agent's Chrome session are closed. Cookies and the saved profile remain on disk.

The Agent waits up to 15 seconds after navigation for `EntityId`, `GlobalAccountId`, `MarketplaceId`, `ClientId`, and `CsrfToken`. On success it returns `ApiResult.Data` as the same `AmazonAdsAccountInfo` model shown above and leaves the `/cb` tab open. If the account fields cannot be found, the API reports an error and leaves Chrome open so you can sign in or inspect the page.

Desktop client:

```csharp
AmazonAdsAccountInfo info = await agent.StartAmazonAdsSessionAsync(); // US
AmazonAdsAccountInfo canadaInfo = await agent.StartAmazonAdsSessionAsync("CA");
Console.WriteLine(canadaInfo.EntityId);
Console.WriteLine(canadaInfo.GlobalAccountId);
Console.WriteLine(canadaInfo.Currency); // CAD
```

This creates a fresh Selenium browser session, while using the stored Amazon Ads login from the same Chrome profile. It does not create a new Amazon user account.

Use the `AmazonAdsAccountInfo` returned by `start-session` (or `account-info`) as the first argument to the desktop client's campaign filter, target filter, and bid update methods. The Agent uses its required fields as Amazon Ads headers and Chrome supplies cookies. If the Chrome session or selected account changes, request fresh account info.
