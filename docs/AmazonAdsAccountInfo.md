# Amazon Ads account information

`POST /api/amazon-ads/account-info` uses the Agent's Selenium Chrome profile to open `https://advertising.amazon.com/cb`, read its page source, and extract the account fields. The request has no body. It requires the usual `X-Api-Key` header and an Amazon Ads session already signed in within the Agent's Chrome profile.

The operation opens a temporary tab, waits up to 15 seconds for `EntityId` to appear in the HTML, then closes that tab and restores the original tab. It serializes access to Chrome with the existing browser commands. It does not need a Cookie header, campaign URL, or entity ID from Desktop.

Example (.NET Framework 4.7.2):

```csharp
using (var agent = new BrowserAgentClient("VPS01", "http://10.0.0.11:5050/", "YOUR_API_KEY"))
{
    var info = await agent.GetAmazonAdsAccountInfoAsync();
    Console.WriteLine(info.AdvertiserId);
    Console.WriteLine(info.EntityId);
    Console.WriteLine(info.GlobalAccountId);
    Console.WriteLine(info.MarketplaceId);
}
```

`Data` contains `Token`, `TraceId`, `SegmentId`, `ClientId`, `CsrfToken`, `SessionId`, `PageHitRequestId`, `AdvertiserId`, `EntityId`, `GlobalAccountId`, and `MarketplaceId`. Any field absent from the HTML is omitted from the JSON (and `null` in the C# DTO). If `EntityId` is missing, the API returns an error indicating that the Chrome login or Amazon Ads page format should be checked.

These fields include session credentials. Keep the API on a private network and avoid logging the response.
