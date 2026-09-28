using System;
using MyBrowserAgent.Models;

namespace MyBrowserAgent.Services
{
    internal static class AmazonAdsAccountInfoValidator
    {
        public static void Validate(AmazonAdsAccountInfo info)
        {
            if (info == null)
                throw new ArgumentException("AccountInfo is required.");

            if (string.IsNullOrWhiteSpace(info.EntityId) ||
                string.IsNullOrWhiteSpace(info.GlobalAccountId) ||
                string.IsNullOrWhiteSpace(info.MarketplaceId) ||
                string.IsNullOrWhiteSpace(info.ClientId) ||
                string.IsNullOrWhiteSpace(info.CsrfToken))
                throw new ArgumentException(
                    "AccountInfo requires EntityId, GlobalAccountId, MarketplaceId, ClientId and CsrfToken.");
        }
    }
}
