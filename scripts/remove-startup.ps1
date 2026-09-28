#Requires -Version 3.0
[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$TaskName = 'MyBrowserAgent'
)

$ErrorActionPreference = 'Stop'
Import-Module ScheduledTasks -ErrorAction Stop

$task = Get-ScheduledTask -TaskName $TaskName -TaskPath '\' -ErrorAction SilentlyContinue
if ($null -eq $task) {
    Write-Host "No scheduled task named $TaskName was found."
    return
}

Unregister-ScheduledTask -TaskName $TaskName -TaskPath '\' -Confirm:$false -ErrorAction Stop
Write-Host "Removed startup task: $TaskName"
Write-Host 'An already running Agent is not stopped.'
