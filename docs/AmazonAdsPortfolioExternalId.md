# Update a portfolio and get its external ID

`PUT /api/amazon-ads/portfolios/external-id` sends an update for an existing portfolio through the signed-in Selenium Chrome session. The Amazon Ads update response contains `updatePortfoliosOutputList[0].portfolio.portfolioExternalId`. This operation **updates the portfolio name** to `Name` and sets its budget to `NO_CAP`; it is not a read-only lookup.

Use `POST /api/amazon-ads/start-session` to obtain `AccountInfo`, then send the usual `X-Api-Key` header and this JSON body:

```json
{
  "AccountInfo": {
    "EntityId": "ENTITY_ID",
    "GlobalAccountId": "GLOBAL_ACCOUNT_ID",
    "MarketplaceId": "ATVPDKIKX0DER",
    "ClientId": "CLIENT_ID",
    "CsrfToken": "CSRF_TOKEN"
  },
  "PortfolioId": "123456789012345",
  "Name": "My Portfolio"
}
```

The Agent verifies the selected account in Chrome, reuses the active `https://advertising.amazon.com/cb` tab or opens a temporary tab, and issues a same-origin `PUT` to `/a9g-api-gateway/cm/api/portfolios`. Chrome supplies session cookies. US marketplace `ATVPDKIKX0DER` uses USD; CA marketplace `A2EUQ1WTGCTBG2` uses CAD. Other marketplaces are rejected. On success, `ApiResult.Data` is the external ID string.

Desktop client (.NET Framework 4.7.2):

```csharp
AmazonAdsAccountInfo info = await agent.StartAmazonAdsSessionAsync();
string externalId = await agent.UpdatePortfolioAndGetExternalIdAsync(
    info, portfolioId, "My Portfolio");
Console.WriteLine(externalId);
```

If the response is missing or invalid, verify the portfolio in Amazon Ads before retrying: the update may already have succeeded.
