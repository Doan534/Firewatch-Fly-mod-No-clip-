@echo off
setlocal
cd /d "%~dp0"

set VER=1.0.0
set OUT=FirewatchFly-%VER%

echo.
echo === Firewatch Fly %VER% bauen ===
echo Bitte Firewatch vorher komplett beenden.
echo.

dotnet build -c Release
if errorlevel 1 (
    echo.
    echo FEHLER: Der Build ist fehlgeschlagen. Siehe Meldungen oben.
    pause
    exit /b 1
)

if not exist "bin\Release\net35\FirewatchFly.dll" (
    echo.
    echo FEHLER: bin\Release\net35\FirewatchFly.dll wurde nicht gefunden.
    pause
    exit /b 1
)

if exist "%OUT%" rmdir /s /q "%OUT%"
if exist "%OUT%.zip" del "%OUT%.zip"
mkdir "%OUT%"

copy /y "bin\Release\net35\FirewatchFly.dll" "%OUT%\" >nul
copy /y "README.md" "%OUT%\" >nul
copy /y "README.de.md" "%OUT%\" >nul
copy /y "LICENSE" "%OUT%\" >nul

powershell -NoProfile -Command "Compress-Archive -Path '%OUT%\*' -DestinationPath '%OUT%.zip' -Force"

echo.
echo === FERTIG ===
echo Ordner: %OUT%
echo ZIP:    %OUT%.zip
echo Das ZIP ist die Datei zum Hochladen.
echo.
pause
