# MyBrowserAgent

Remote-control a Chrome browser running on a Windows VPS through a small HTTP API.

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

Run one BrowserAgent on each VPS. Your desktop application talks only to the HTTP API; all browser automation happens locally on the VPS.

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
- `scripts/configure-server.cmd` — URL ACL and optional firewall rule helper.

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

### 5. Configure HTTP URL ACL

Run Command Prompt as Administrator:

```cmd
scripts\configure-server.cmd 5050
```

To also add a Windows Firewall rule restricted to the desktop's public/private IP:

```cmd
scripts\configure-server.cmd 5050 192.168.1.20
```

The second form is preferred.

If you use Tailscale/WireGuard, restrict access to that private network instead of exposing the agent to the public Internet.

## Run

```cmd
C:\BrowserAgent\MyBrowserAgent.exe
```

Expected output:

```text
MyBrowserAgent
Listening: http://+:5050/
Browser: starting...
READY
```

### Recommended startup on Windows Server 2012 R2

Use **Task Scheduler**, not a LocalSystem Windows Service, when `Headless=false`.

Recommended task settings:

- Trigger: At log on
- User: the same Windows user that owns/uses the Chrome profile
- Run only when user is logged on
- Start: `C:\BrowserAgent\MyBrowserAgent.exe`
- Start in: `C:\BrowserAgent`

This keeps the visible Chrome window in the user's interactive session.

When leaving RDP, **Disconnect** rather than signing out if you want Chrome and BrowserAgent to remain running.

## HTTP API

Every request requires:

```text
X-Api-Key: <your key>
```

Main endpoints:

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/browser/status` | Browser/agent status |
| POST | `/api/browser/start` | Start Chrome |
| POST | `/api/browser/stop` | Stop Chrome |
| POST | `/api/browser/open` | Navigate to URL |
| POST | `/api/browser/click` | Click an element |
| POST | `/api/browser/fill` | Clear and fill an element |
| POST | `/api/browser/sendkeys` | Append/send keys |
| GET | `/api/browser/html` | Get page HTML |
| POST | `/api/browser/javascript` | Execute JavaScript |
| GET | `/api/browser/screenshot` | Return PNG screenshot |
| GET | `/api/browser/cookies` | Get current-domain cookies |
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
| POST | `/api/amazon-ads/account-info` | Fetch and parse Amazon Ads account data |

See [Amazon Ads account information](docs/AmazonAdsAccountInfo.md) for the request fields and a .NET Framework desktop example.

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

and configure your four VPS agents.

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

Do not open the same Chrome profile concurrently from another Chrome process while Selenium is using it.

## Security

This API can control a real logged-in browser. Treat it like remote desktop access.

Recommended:

1. Keep the port on Tailscale, WireGuard, LAN or another private network.
2. Use a different long random API key per VPS.
3. Restrict Windows Firewall to your desktop/VPN IP.
4. Do not expose port 5050 to the whole Internet.
5. Do not log cookie values, JavaScript payloads containing secrets, passwords or session tokens.
6. Keep the dedicated Chrome profile separate from personal browsing.

## License

No license has been selected yet. Add one if you plan to distribute the project.
