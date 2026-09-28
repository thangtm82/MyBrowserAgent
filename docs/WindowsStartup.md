# Start MyBrowserAgent automatically on Windows

The Agent runs Selenium with a visible Chrome window when `Headless=false`. Install an **at logon** Scheduled Task for the same Windows user that owns the Chrome profile. This starts the HTTP API and, with `AutoStartBrowser=true` in `config.json`, opens Chrome too. The task uses the interactive desktop session.

## Install on each VPS

Copy the Release output to `C:\BrowserAgent\`, create `config.json`, add the matching `driver\chromedriver.exe`, and run `scripts\configure-server.cmd 5050 YOUR_DESKTOP_IP` once in an elevated Command Prompt. Log in as the Windows user who will run Chrome, then open **Windows PowerShell as Administrator** under that same account:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\BrowserAgent\scripts\install-startup.ps1" -AgentPath "C:\BrowserAgent\MyBrowserAgent.exe"
```

Copy `scripts\install-startup.ps1` and `scripts\remove-startup.ps1` from this repository to `C:\BrowserAgent\scripts\` first. The installer checks the executable and adjacent `config.json`. It creates or replaces the task named `MyBrowserAgent` for the current user, using that executable's directory as **Start in**. It runs only when this user is signed in, restarts up to three times after a failure, and has no 72-hour execution limit.

If you use a different folder, change `-AgentPath` accordingly. If several Agents share one Windows account, pass a distinct `-TaskName` to each installer call.

## Check and test

```powershell
Get-ScheduledTask -TaskName MyBrowserAgent | Format-List TaskName,State,Actions,Triggers,Principal,Settings
Get-ScheduledTask -TaskName MyBrowserAgent | Get-ScheduledTaskInfo
```

To start it in the current logged-in session without signing out, first close any manually started `MyBrowserAgent.exe`, then run:

```powershell
Start-ScheduledTask -TaskName MyBrowserAgent
```

Confirm the API with your normal `X-Api-Key` header:

```powershell
curl.exe -H "X-Api-Key: YOUR_KEY" http://127.0.0.1:5050/api/browser/status
```

You can also sign out and sign back in to verify the logon trigger. When leaving RDP, disconnect instead of signing out if Chrome should continue running. A logon task starts after sign-in; it does not start Chrome before any user has signed in. The task uses the current user's environment variables, so sign in again after setting `MYBROWSERAGENT_API_KEY`.

## Disable automatic startup

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\BrowserAgent\scripts\remove-startup.ps1"
```

Removing the task does not stop an Agent that is already running.
