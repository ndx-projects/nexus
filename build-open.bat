@echo off
rem Nexus (wereldwijde versie, eigen vormgeving per setup) bouwen: publish\open\Nexus.exe
cd /d "%~dp0"
if exist obj rmdir /s /q obj
if exist bin rmdir /s /q bin
dotnet publish Nexus.csproj -c Release -r win-x64 --self-contained true -p:Edition=Open -o publish\open
if errorlevel 1 (echo Bouwen mislukt & pause & exit /b 1)
echo.
echo Klaar: publish\open\Nexus.exe
pause
