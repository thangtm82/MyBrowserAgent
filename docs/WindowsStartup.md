# Start MyBrowserAgent automatically on Windows

The Agent runs Selenium with a visible Chrome window when `Headless=false`. Install an **at logon** Scheduled Task for the same Windows user that owns the Chrome profile. This starts the HTTP API and, with `AutoStartBrowser=true` in `config.json`, opens Chrome too. The task uses the interactive desktop session and starts the Agent with `--no-window`: no Agent console remains open, while Chrome stays visible. Startup output and errors are written to `%LOCALAPPDATA%\MyBrowserAgent\logs\agent-YYYYMMDD.log` for the Windows user who runs the task.

## Install on each VPS

Copy the Release output to `C:\BrowserAgent\`, create `config.json`, add the matching `driver\chromedriver.exe`, and run `scripts\configure-server.cmd 5050 local` once in an elevated Command Prompt for local access. For remote access, set `"ListenMode": "Internet"` in `config.json` and run `scripts\configure-server.cmd 5050 internet YOUR_DESKTOP_IP` instead. Stop the Agent, re-run the helper, then start it again when switching modes. Log in as the Windows user who will run Chrome, then open **Windows PowerShell as Administrator** under that same account:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\BrowserAgent\scripts\install-startup.ps1" -AgentPath "C:\BrowserAgent\MyBrowserAgent.exe"
```

Copy `scripts\install-startup.ps1` and `scripts\remove-startup.ps1` from this repository to `C:\BrowserAgent\scripts\` first. The installer checks the executable and adjacent `config.json`. It creates or replaces the task named `MyBrowserAgent` for the current user, using that executable's directory as **Start in** and passing `--no-window`. It runs only when this user is signed in, restarts up to three times after a failure, and has no 72-hour execution limit.

If you use a different folder, change `-AgentPath` accordingly. After upgrading from an older Agent, copy the newly built executable and re-run the installer to update the existing task. Stop an older running Agent before starting the updated task. To see the console while troubleshooting, run `MyBrowserAgent.exe` manually without `--no-window`. Only one MyBrowserAgent process can run on each Windows machine, even across user logon sessions. A second launch exits without starting Chrome or the API.

## Check and test

```powershell
Get-ScheduledTask -TaskName MyBrowserAgent | Format-List TaskName,State,Actions,Triggers,Principal,Settings
Get-ScheduledTask -TaskName MyBrowserAgent | Get-ScheduledTaskInfo
```

To start it in the current logged-in session without signing out, first close any manually started `MyBrowserAgent.exe`, then run:

```powershell
Start-ScheduledTask -TaskName MyBrowserAgent
```

Inspect the Agent log in the Windows account that owns the task:

```powershell
Get-Content "$env:LOCALAPPDATA\MyBrowserAgent\logs\agent-$(Get-Date -Format yyyyMMdd).log" -Tail 30
```

Confirm the API with your normal `X-Api-Key` header:

```powershell
curl.exe -H "X-Api-Key: YOUR_KEY" http://127.0.0.1:5050/api/browser/status
```

To verify the one-instance behavior, start `MyBrowserAgent.exe` manually while the scheduled Agent is running; the second process prints `MyBrowserAgent is already running. Exiting.` and exits with code 0. Stop an older deployed build before upgrading because it does not hold the new mutex.

You can also sign out and sign back in to verify the logon trigger. When leaving RDP, disconnect instead of signing out if Chrome should continue running. A logon task starts after sign-in; it does not start Chrome before any user has signed in. The task uses the current user's environment variables, so sign in again after setting `MYBROWSERAGENT_API_KEY`.

## Disable automatic startup

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\BrowserAgent\scripts\remove-startup.ps1"
```

Removing the task does not stop an Agent that is already running.
