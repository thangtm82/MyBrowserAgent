@echo off
setlocal

set PORT=%~1
set REMOTE_IP=%~2
if "%PORT%"=="" set PORT=5050

echo Configuring URL ACL for port %PORT%...
netsh http delete urlacl url=http://+:%PORT%/ >nul 2>&1
netsh http add urlacl url=http://+:%PORT%/ user="%USERDOMAIN%\%USERNAME%"
if errorlevel 1 goto :error

echo Removing old firewall rule if present...
netsh advfirewall firewall delete rule name="MyBrowserAgent %PORT%" >nul 2>&1

if "%REMOTE_IP%"=="" (
    echo Adding firewall rule for all remote IPs.
    echo WARNING: Prefer passing your desktop/VPN IP as the second argument.
    netsh advfirewall firewall add rule name="MyBrowserAgent %PORT%" dir=in action=allow protocol=TCP localport=%PORT%
) else (
    echo Adding firewall rule restricted to %REMOTE_IP%...
    netsh advfirewall firewall add rule name="MyBrowserAgent %PORT%" dir=in action=allow protocol=TCP localport=%PORT% remoteip=%REMOTE_IP%
)

if errorlevel 1 goto :error
echo.
echo Done.
echo URL: http://+:%PORT%/
goto :eof

:error
echo.
echo ERROR: configuration failed. Run this script from an elevated Command Prompt.
exit /b 1
