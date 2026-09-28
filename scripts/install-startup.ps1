#Requires -Version 3.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AgentPath,

    [ValidateNotNullOrEmpty()]
    [string]$TaskName = 'MyBrowserAgent'
)

$ErrorActionPreference = 'Stop'
Import-Module ScheduledTasks -ErrorAction Stop

$resolvedPath = (Resolve-Path -LiteralPath $AgentPath -ErrorAction Stop).ProviderPath
if ([IO.Path]::GetFileName($resolvedPath) -ine 'MyBrowserAgent.exe') {
    throw 'AgentPath must point to MyBrowserAgent.exe.'
}

$directory = Split-Path -Path $resolvedPath -Parent
if (-not (Test-Path -LiteralPath (Join-Path $directory 'config.json') -PathType Leaf)) {
    throw "config.json was not found next to $resolvedPath."
}

$user = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$action = New-ScheduledTaskAction -Execute $resolvedPath -WorkingDirectory $directory
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $user
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -MultipleInstances IgnoreNew `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries

Register-ScheduledTask -TaskName $TaskName -TaskPath '\' `
    -Action $action -Trigger $trigger -Principal $principal -Settings $settings `
    -Description 'Start MyBrowserAgent and its Selenium Chrome session when this user signs in.' `
    -Force | Out-Null

Write-Host "Installed scheduled task: $TaskName"
Write-Host "User: $user"
Write-Host "Executable: $resolvedPath"
Write-Host 'Trigger: at user logon (interactive session).'
Write-Host "Check: Get-ScheduledTask -TaskName '$TaskName' | Get-ScheduledTaskInfo"
