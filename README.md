# MyBrowserAgent

Control a Chrome browser running on Windows through a small HTTP API, either locally or from another machine.

This project is designed specifically for environments that must remain on **Windows Server 2012 R2** and **.NET Framework 4.7.2**.

## Architecture

```text
Desktop / C# controller
        |
        | HTTP + X-Api-Key
        v
BrowserAgent.exe (VPS)
        |
        v
Selenium WebDriver
        |
        v
Chrome 109 + persistent Chrome profile
```

Run one BrowserAgent on each machine. In Internet mode your desktop application can call the VPS HTTP API; all browser automation happens on the machine running the Agent.

## Important compatibility note

Windows Server 2012 R2 is no longer supported by current Chrome releases. Use a compatible pair such as:

- Chrome 109.x
- ChromeDriver 109.x (same major version)
- .NET Framework 4.7.2

Do **not** let Chrome update beyond the version supported by Server 2012 R2.

Chrome/ChromeDriver binaries are intentionally not committed to this repository.

## Projects

- `src/MyBrowserAgent` — BrowserAgent HTTP API, Selenium browser host.
- `samples/DesktopController` — .NET Framework 4.7.2 example client for controlling multiple VPS machines.
- `scripts/configure-server.cmd` — URL ACL and firewall configuration for Local/Internet mode.
- `scripts/install-startup.ps1` and `scripts/remove-startup.ps1` — Windows logon task setup.

## Build

Recommended: build on your desktop with Visual Studio 2019/2022 or a compatible .NET SDK/Build Tools installation.

```powershell
dotnet restore
dotnet build MyBrowserAgent.sln -c Release
```

The BrowserAgent output will be under:

```text
src\MyBrowserAgent\bin\Release\net472\
```

Copy that output folder to the VPS, for example:

```text
C:\BrowserAgent\
```

## VPS preparation

### 1. Install .NET Framework 4.7.2

Verify that .NET Framework 4.7.2 is installed on the VPS.

### 2. Install Chrome 109

Install a Chrome release compatible with Windows Server 2012 R2.

### 3. Add matching ChromeDriver

Create:

```text
C:\BrowserAgent\driver\
```

and place the matching `chromedriver.exe` there.

Example layout:

```text
C:\BrowserAgent\
|-- MyBrowserAgent.exe
|-- MyBrowserAgent.dll
|-- config.json
|-- driver\
|   `-- chromedriver.exe
`-- ChromeProfile\
```

### 4. Create config.json

Copy:

```text
config.sample.json
```

to:

```text
config.json
```

Then edit it.

Example:

```json
{
  "Port": 5050,
  "ListenMode": "Local",
  "ApiKey": "replace-with-a-long-random-key",
  "ChromeDriverDirectory": "driver",
  "ChromeProfileDirectory": "ChromeProfile",
  "ChromeBinary": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
  "Headless": false,
  "AutoStartBrowser": true,
  "PageLoadTimeoutSeconds": 60,
  "ImplicitWaitSeconds": 1
}
```

You may keep `ApiKey` empty in `config.json` and provide it using:

```powershell
setx MYBROWSERAGENT_API_KEY "your-long-random-key"
```

Open a new logon/session after changing a user environment variable.

### 5. Choose the listening mode

`ListenMode` defaults to `Local` when omitted. Set it in `config.json`, then run the matching command once in an elevated Command Prompt:

| Mode | `config.json` | Command | API address |
|---|---|---|---|
| Local | `"ListenMode": "Local"` | `scripts\configure-server.cmd 5050 local` | `http://localhost:5050/` on this machine |
| Internet | `"ListenMode": "Internet"` | `scripts\configure-server.cmd 5050 internet 192.168.1.20` | `http://YOUR_SERVER_IP:5050/` from the allowed IP |

In Local mode the helper removes the Agent's previous inbound firewall rule and wildcard URL reservation. The API also rejects non-loopback connections. In Internet mode the helper opens inbound TCP port 5050; omit the final IP argument only if you intend to allow any remote IP. Keep your cloud firewall/NAT rules aligned with this choice. Stop the Agent, re-run the helper, then start it again each time you switch modes or ports. Run the helper as the same Windows user who runs the Agent.

The old `configure-server.cmd 5050 192.168.1.20` syntax has changed; specify `internet` before the remote IP. Use a private network such as Tailscale/WireGuard when possible.

## Run

```cmd
C:\BrowserAgent\MyBrowserAgent.exe
```

Expected output:

```text
MyBrowserAgent
Listen mode: Local
Listening: http://localhost:5050/
Browser: starting...
READY
```

At startup, the Agent takes a machine-wide named mutex. If another MyBrowserAgent instance is already running, the new process prints `MyBrowserAgent is already running. Exiting.` and exits with code 0 before opening Chrome or the HTTP listener. Stop an older deployed version before upgrading: older builds do not hold this mutex.

### Automatic startup on Windows Server 2012 R2

Install an interactive logon Scheduled Task under the Windows user who runs Chrome:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\BrowserAgent\scripts\install-startup.ps1" -AgentPath "C:\BrowserAgent\MyBrowserAgent.exe"
```

Copy the two scripts from `scripts\` to `C:\BrowserAgent\scripts\` first, and run the command in an elevated Windows PowerShell session under that same user. The task runs `MyBrowserAgent.exe --no-window`, hiding the Agent console while keeping Selenium's Chrome visible. Agent startup output is written to `%LOCALAPPDATA%\MyBrowserAgent\logs\agent-YYYYMMDD.log` for that Windows user. Re-run the installer after upgrading the Agent to update an existing task. `AutoStartBrowser=true` in `config.json` also starts Chrome when the Agent launches.

See [automatic Windows startup](docs/WindowsStartup.md) for setup, verification, and removal. The logon task needs a signed-in user; when leaving RDP, disconnect instead of signing out if the Agent should remain running.

## HTTP API

Every request requires:

```text
X-Api-Key: <your key>
```

Main endpoints:

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/browser/status` | Browser/agent status |
| POST | `/api/browser/start` | Start Chrome; recover from a profile conflict by closing Chrome using this Agent's profile and retrying once |
| POST | `/api/browser/stop` | Stop Chrome |
| POST | `/api/browser/open` | Navigate to URL |
| POST | `/api/browser/click` | Click an element |
| POST | `/api/browser/fill` | Clear and fill an element |
| POST | `/api/browser/sendkeys` | Append/send keys |
| GET | `/api/browser/html` | Get page HTML |
| POST | `/api/browser/javascript` | Execute JavaScript |
| GET | `/api/browser/screenshot` | Return PNG screenshot |
| GET | `/api/browser/cookies` | Get current-domain cookies |
| GET | `/api/browser/cookies/text` | Get current-domain cookies as plain text with values |
| POST | `/api/browser/cookies/clear` | Clear cookies |
| POST | `/api/browser/wait` | Wait for an element |
| POST | `/api/browser/text` | Get element text |
| POST | `/api/browser/attribute` | Get element attribute |
| GET | `/api/browser/windows` | List browser windows/tabs |
| POST | `/api/browser/window/switch` | Switch window/tab |
| POST | `/api/browser/tab/new` | Open a new tab |
| POST | `/api/browser/tab/close` | Close current tab |
| POST | `/api/browser/back` | Browser back |
| POST | `/api/browser/forward` | Browser forward |
| POST | `/api/browser/refresh` | Browser refresh |
| POST | `/api/amazon-ads/portfolios` | Create a portfolio in the signed-in Amazon Ads account and return its ID |
| PUT | `/api/amazon-ads/portfolios/external-id` | Update a portfolio name and return `portfolioExternalId` from Amazon Ads |
| POST | `/api/amazon-ads/start-session` | Restart the Agent's Chrome session, open campaign manager and return Amazon Ads account information |
| POST | `/api/amazon-ads/account-info` | Read Amazon Ads account data from the Selenium Chrome session |
| POST | `/api/amazon-ads/campaigns/auto` | Submit a Sponsored Products automatic campaign in the signed-in browser |
| POST | `/api/amazon-ads/campaigns/manual-product` | Submit a Sponsored Products manual product targeting campaign |
| POST | `/api/amazon-ads/campaigns/manual-keyword` | Submit a Sponsored Products manual keyword targeting campaign |
| POST | `/api/amazon-ads/campaigns/filter` | Filter campaigns with in-browser Amazon Ads fetch |
| POST | `/api/amazon-ads/targets/filter` | Return one target report page at the client-supplied offset (50 rows maximum) |
| PUT | `/api/amazon-ads/targets/bid` | Update one target bid and return Amazon's update result |
| PUT | `/api/amazon-ads/targets/bids` | Update multiple target bids in one Amazon request; write a trace log and return `X-Agent-Trace-Id` |

Call `POST /api/amazon-ads/start-session` (or `account-info`) to obtain `AmazonAdsAccountInfo`. Pass that object as `AccountInfo` in every campaign filter, target filter, bid update, portfolio creation, and campaign creation request. For bulk bid updates, send `{ "AccountInfo": { ... }, "Targets": [ ... ] }`; the Agent forwards only the target array to Amazon Ads. Client methods take the account info as their first argument. Keep it private and refresh it if the Chrome session changes.

See [Amazon Ads account information](docs/AmazonAdsAccountInfo.md), [campaign filtering](docs/AmazonAdsCampaignFilter.md), [target filtering](docs/AmazonAdsTargetFilter.md), [target bid updates](docs/AmazonAdsTargetBidUpdate.md), [portfolio creation](docs/AmazonAdsPortfolioCreate.md), [portfolio external ID](docs/AmazonAdsPortfolioExternalId.md), [automatic campaign creation](docs/AmazonAdsAutoCampaignCreate.md), [manual product campaign creation](docs/AmazonAdsManualProductCampaignCreate.md), and [manual keyword campaign creation](docs/AmazonAdsManualKeywordCampaignCreate.md) for .NET Framework desktop examples.

When the selected Selenium tab's URL starts with `https://advertising.amazon.com/cb`, Amazon Ads operations run on that tab and leave it open. From other pages, the Agent opens `/cb` in a temporary tab and restores the original tab afterward.

### Selector types

The following selector types are supported:

```text
css
id
name
xpath
tag
class
linktext
partiallinktext
```

Default is `css`.

## API examples

### Status

```powershell
curl.exe -H "X-Api-Key: YOUR_KEY" http://127.0.0.1:5050/api/browser/status
```

### Open URL

```powershell
curl.exe -X POST ^
  -H "X-Api-Key: YOUR_KEY" ^
  -H "Content-Type: application/json" ^
  -d "{\"Url\":\"https://example.com\"}" ^
  http://127.0.0.1:5050/api/browser/open
```

### Fill

```json
{
  "Selector": "#search",
  "SelectorType": "css",
  "Value": "hello"
}
```

### Click

```json
{
  "Selector": "#submit",
  "SelectorType": "css"
}
```

### Execute JavaScript

```json
{
  "Script": "return document.title;"
}
```

### Cookies

Cookie values are hidden by default:

```text
GET /api/browser/cookies
```

To explicitly include values:

```text
GET /api/browser/cookies?includeValues=true
```

To return the current page's cookies as raw `text/plain` in `name=value; name2=value2` format:

```text
GET /api/browser/cookies/text
```

The route requires the usual `X-Api-Key`. In the desktop sample use `await agent.GetCookiesTextAsync()`. The response includes cookie values, including HttpOnly cookies visible to Selenium, and is marked `Cache-Control: no-store`.

Treat cookie values as secrets. Do not log them.

## Desktop controller sample

Copy:

```text
samples\DesktopController\vps.sample.json
```

to:

```text
vps.json
```

and configure your four VPS agents in Internet mode.

Example:

```json
[
  {
    "Name": "VPS01",
    "BaseUrl": "http://10.0.0.11:5050/",
    "ApiKey": "key-vps01"
  },
  {
    "Name": "VPS02",
    "BaseUrl": "http://10.0.0.12:5050/",
    "ApiKey": "key-vps02"
  }
]
```

Run against every configured VPS:

```cmd
DesktopController.exe status
DesktopController.exe open https://example.com
DesktopController.exe screenshot C:\Screenshots
```

The reusable `BrowserAgentClient` class also exposes click, fill, HTML, JavaScript, cookies and screenshot methods for integration into your WinForms/WPF application.

## Persistent login/profile

BrowserAgent launches Chrome using a dedicated persistent user-data directory:

```text
ChromeProfile
```

Login state, cookies and local storage can therefore persist across BrowserAgent restarts.

Do not open the same Chrome profile concurrently from another Chrome process while Selenium is using it. If startup fails with a `DevToolsActivePort` or profile-in-use error and a running `chrome.exe` has this exact `--user-data-dir`, the Agent closes that Chrome process and retries once. Other Chrome profiles are left alone. Any unsaved tabs in the Agent profile will be closed; the on-disk profile and login data remain in place. If no matching Chrome process is found or the retry fails, the API reports the error.

## Security

This API can control a real logged-in browser. Treat it like remote desktop access.

Recommended:

1. Keep the port on Tailscale, WireGuard, LAN or another private network.
2. Use a different long random API key per VPS.
3. Restrict Windows Firewall to your desktop/VPN IP.
4. Prefer Local mode unless another machine must call the API; restrict Internet mode to trusted IPs.
5. Do not log cookie values, JavaScript payloads containing secrets, passwords or session tokens.
6. Keep the dedicated Chrome profile separate from personal browsing.

## License

No license has been selected yet. Add one if you plan to distribute the project.
