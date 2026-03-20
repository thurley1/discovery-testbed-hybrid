@echo off
REM Deployment script — run from the build server (Jim's laptop)
REM Last updated: 2021-03-15 by someone who left the company

echo ========================================
echo  CursedApp Deployment Script v2.1
echo ========================================

echo Step 1: Build
dotnet publish src\CursedApp.Api\CursedApp.Api.csproj -c Release -o deploy\api
dotnet publish src\CursedApp.Workers\CursedApp.Workers.csproj -c Release -o deploy\workers
dotnet publish src\CursedApp\CursedApp.csproj -c Release -o deploy\core

echo Step 2: Stop services
net stop "CursedApp API" 2>NUL
net stop "CursedApp Workers" 2>NUL
timeout /t 5

echo Step 3: Copy files (the scary part)
xcopy deploy\api \\prod-server\c$\CursedApp\api\ /s /y /q
xcopy deploy\workers \\prod-server\c$\CursedApp\workers\ /s /y /q

echo Step 4: Run migrations
sqlcmd -S prod-db-server -d CursedApp -i src\scripts\migrate.sql -U sa -P CursedApp2019!

echo Step 5: Start services
net start "CursedApp API"
net start "CursedApp Workers"

echo Step 6: Verify
timeout /t 10
curl -s http://prod-server:5000/health
if %ERRORLEVEL% NEQ 0 (
    echo DEPLOYMENT FAILED — API not responding
    echo Rolling back... just kidding, we don't have rollbacks
    exit /b 1
)

echo Step 7: Notify
echo Deployment complete | mail -s "CursedApp deployed" admin@cursedapp.com

echo ========================================
echo  Deployment complete (probably)
echo ========================================
