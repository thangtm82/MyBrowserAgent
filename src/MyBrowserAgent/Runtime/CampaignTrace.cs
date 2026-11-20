using System;
using System.Globalization;
using System.IO;
using MyBrowserAgent.Models;
using Newtonsoft.Json;

namespace MyBrowserAgent.Runtime
{
    internal static class CampaignTrace
    {
        private static readonly object Sync = new object();
        private const int MaxResponseLength = 8192;

        public static void Write(string traceId, string endpoint, string market, AmazonAdsAccountInfo account,
            int? statusCode = null, string responseBody = null, Exception exception = null)
        {
            var now = DateTime.Now;
            var secrets = new[]
            {
                BrowserAgentRuntime.Config?.ApiKey, account?.ClientId, account?.CsrfToken, account?.SessionId
            };
            var body = Redact(responseBody, secrets);
            if (body != null && body.Length > MaxResponseLength)
                body = body.Substring(0, MaxResponseLength) + " [truncated]";

            var line = JsonConvert.SerializeObject(new
            {
                Timestamp = now.ToString("O", CultureInfo.InvariantCulture),
                TraceId = traceId,
                Endpoint = "POST /api/amazon-ads/" + endpoint,
                Market = Redact(market, secrets),
                AmazonHttpStatus = statusCode,
                ResponseBody = body,
                Exception = Redact(exception?.ToString(), secrets)
            });

            try
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MyBrowserAgent", "logs");
                var file = "campaign-errors-" + now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log";
                lock (Sync)
                {
                    Directory.CreateDirectory(directory);
                    File.AppendAllText(Path.Combine(directory, file), line + Environment.NewLine);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                try
                {
                    Console.Error.WriteLine("Could not write the campaign trace file. " + line);
                }
                catch (IOException)
                {
                    // Logging must not change the result of a campaign submission.
                }
            }
        }

        private static string Redact(string value, string[] secrets)
        {
            if (value == null) return null;
            foreach (var secret in secrets)
                if (!string.IsNullOrEmpty(secret))
                    value = value.Replace(secret, "[REDACTED]");
            return value;
        }
    }
}
