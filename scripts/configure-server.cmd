@echo off
setlocal

set "PORT=%~1"
set "LISTEN_MODE=%~2"
set "REMOTE_IP=%~3"
if "%PORT%"=="" set "PORT=5050"
if "%LISTEN_MODE%"=="" set "LISTEN_MODE=local"

echo(%PORT%| findstr /r "^[0-9][0-9]*$" >nul
if errorlevel 1 goto :usage
if %PORT% LSS 1 goto :usage
if %PORT% GTR 65535 goto :usage
if /i not "%LISTEN_MODE%"=="local" if /i not "%LISTEN_MODE%"=="internet" goto :usage
if /i "%LISTEN_MODE%"=="local" if not "%REMOTE_IP%"=="" goto :usage
if not "%~4"=="" goto :usage

net session >nul 2>&1
if errorlevel 1 goto :error

echo Removing old Agent firewall rule and URL reservations for port %PORT%...
netsh advfirewall firewall delete rule name="MyBrowserAgent %PORT%" >nul 2>&1
netsh http delete urlacl url=http://+:%PORT%/ >nul 2>&1
netsh http delete urlacl url=http://localhost:%PORT%/ >nul 2>&1

if /i "%LISTEN_MODE%"=="local" (
    echo Configuring local access only...
    netsh http add urlacl url=http://localhost:%PORT%/ user="%USERDOMAIN%\%USERNAME%"
    if errorlevel 1 goto :error
    echo No inbound firewall rule added.
    echo URL: http://localhost:%PORT%/
) else (
    echo Configuring Internet access...
    netsh http add urlacl url=http://+:%PORT%/ user="%USERDOMAIN%\%USERNAME%"
    if errorlevel 1 goto :error

    if "%REMOTE_IP%"=="" (
        echo WARNING: Inbound port %PORT% will be allowed from all remote IPs.
        netsh advfirewall firewall add rule name="MyBrowserAgent %PORT%" dir=in action=allow protocol=TCP localport=%PORT%
    ) else (
        echo Adding firewall rule restricted to %REMOTE_IP%...
        netsh advfirewall firewall add rule name="MyBrowserAgent %PORT%" dir=in action=allow protocol=TCP localport=%PORT% remoteip=%REMOTE_IP%
    )
    if errorlevel 1 goto :error
    echo URL: http://+:%PORT%/
)

echo.
echo Done. Set ListenMode to %LISTEN_MODE% in config.json and restart MyBrowserAgent.
exit /b 0

:usage
echo Usage: configure-server.cmd [PORT] [local^|internet] [REMOTE_IP]
echo Example: configure-server.cmd 5050 local
echo Example: configure-server.cmd 5050 internet 192.168.1.20
exit /b 2

:error
echo.
echo ERROR: configuration failed. Run this script from an elevated Command Prompt.
exit /b 1
