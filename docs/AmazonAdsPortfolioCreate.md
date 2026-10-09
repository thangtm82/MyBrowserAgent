# Create an Amazon Ads portfolio

`POST /api/amazon-ads/portfolios` creates one portfolio in the signed-in Selenium Chrome session. It uses the client-supplied account information and sends a same-origin request to `/a9g-api-gateway/cm/api/portfolios`. Chrome supplies its own cookies. The Agent navigates the current tab to `https://advertising.amazon.com/cb` if needed and leaves it open afterward.

Use `POST /api/amazon-ads/start-session` to get `AccountInfo`, then send:

```json
{
  "AccountInfo": {
    "EntityId": "ENTITY_ID",
    "GlobalAccountId": "GLOBAL_ACCOUNT_ID",
    "MarketplaceId": "ATVPDKIKX0DER",
    "ClientId": "CLIENT_ID",
    "CsrfToken": "CSRF_TOKEN"
  },
  "Name": "My Portfolio"
}
```

Set the usual `X-Api-Key` header. The Agent verifies that the account and marketplace in `AccountInfo` match the Chrome page before creating the portfolio. US (`ATVPDKIKX0DER`) uses USD; CA (`A2EUQ1WTGCTBG2`) uses CAD. Other marketplaces are rejected. The body uses a `NO_CAP` budget and an empty campaign association list, matching the original script. `ApiResult.Data` is the new portfolio ID as a string.

Desktop client example (.NET Framework 4.7.2):

```csharp
AmazonAdsAccountInfo info = await agent.StartAmazonAdsSessionAsync();
string portfolioId = await agent.CreatePortfolioAsync(info, "My Portfolio");
Console.WriteLine(portfolioId);
```

The `Name` is serialized as JSON, so quotes and other characters are escaped correctly. A failed or timed-out request may have created the portfolio; check Amazon Ads before retrying to avoid duplicates. Keep `AccountInfo` private.
