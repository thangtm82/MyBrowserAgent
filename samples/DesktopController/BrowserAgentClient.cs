using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DesktopController
{
    public sealed class BrowserAgentClient : IDisposable
    {
        private readonly HttpClient _http;
        public string Name { get; }

        public BrowserAgentClient(string name, string baseUrl, string apiKey)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromMinutes(2)
            };
            _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        public async Task<AgentStatus> GetStatusAsync()
        {
            var response = await SendAsync<AgentStatus>(HttpMethod.Get, "api/browser/status", null);
            return response.Data;
        }

        public Task StartAsync() => SendNoResultAsync(HttpMethod.Post, "api/browser/start", null);
        public Task StopAsync() => SendNoResultAsync(HttpMethod.Post, "api/browser/stop", null);
        public Task OpenAsync(string url) => SendNoResultAsync(HttpMethod.Post, "api/browser/open", new { Url = url });
        public Task ClickAsync(string selector, string selectorType = "css") => SendNoResultAsync(HttpMethod.Post, "api/browser/click", new { Selector = selector, SelectorType = selectorType });
        public Task FillAsync(string selector, string value, string selectorType = "css") => SendNoResultAsync(HttpMethod.Post, "api/browser/fill", new { Selector = selector, SelectorType = selectorType, Value = value });
        public Task SendKeysAsync(string selector, string value, string selectorType = "css") => SendNoResultAsync(HttpMethod.Post, "api/browser/sendkeys", new { Selector = selector, SelectorType = selectorType, Value = value });

        public async Task<string> GetHtmlAsync()
        {
            var response = await SendAsync<string>(HttpMethod.Get, "api/browser/html", null);
            return response.Data;
        }

        public async Task<object> ExecuteJavaScriptAsync(string script)
        {
            var response = await SendAsync<object>(HttpMethod.Post, "api/browser/javascript", new { Script = script });
            return response.Data;
        }

        public async Task<string> GetCookiesJsonAsync(bool includeValues = false)
        {
            using (var response = await _http.GetAsync("api/browser/cookies?includeValues=" + includeValues.ToString().ToLowerInvariant()))
            {
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException(Name + ": " + body);
                return body;
            }
        }

        public async Task ScreenshotAsync(string filePath)
        {
            using (var response = await _http.GetAsync("api/browser/screenshot"))
            {
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException(Name + ": " + await response.Content.ReadAsStringAsync());

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(filePath, bytes);
            }
        }

        private async Task SendNoResultAsync(HttpMethod method, string path, object body)
        {
            await SendAsync<object>(method, path, body);
        }

        private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string path, object body)
        {
            using (var request = new HttpRequestMessage(method, path))
            {
                if (body != null)
                {
                    var json = JsonConvert.SerializeObject(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                using (var response = await _http.SendAsync(request))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException(Name + ": HTTP " + (int)response.StatusCode + " - " + json);

                    var result = JsonConvert.DeserializeObject<ApiResponse<T>>(json);
                    if (result == null) throw new InvalidOperationException(Name + ": invalid JSON response.");
                    if (!result.Success) throw new InvalidOperationException(Name + ": " + result.Error);
                    return result;
                }
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
