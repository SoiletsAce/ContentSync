@echo off
echo ========================================
echo Password Depot Content Sync - Build Tool
echo ========================================
echo.

REM Check if dotnet is installed
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK ist nicht installiert!
    echo Bitte installieren Sie das .NET 8.0 SDK von:
    echo https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [1] Debug Build
echo [2] Release Build  
echo [3] Standalone EXE erstellen (selbstständig lauffähig)
echo [4] Projekt bereinigen
echo [5] Projekt ausführen
echo.
set /p choice="Wählen Sie eine Option (1-5): "

if "%choice%"=="1" (
    echo.
    echo Building Debug version...
    dotnet build -c Debug
    if %errorlevel% equ 0 (
        echo.
        echo ✓ Debug Build erfolgreich!
        echo Ausgabe: bin\Debug\net8.0-windows\
    )
)

if "%choice%"=="2" (
    echo.
    echo Building Release version...
    dotnet build -c Release
    if %errorlevel% equ 0 (
        echo.
        echo ✓ Release Build erfolgreich!
        echo Ausgabe: bin\Release\net8.0-windows\
    )
)

if "%choice%"=="3" (
    echo.
    echo Erstelle Standalone EXE...
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true
    if %errorlevel% equ 0 (
        echo.
        echo ✓ Standalone EXE erfolgreich erstellt!
        echo Ausgabe: bin\Release\net8.0-windows\win-x64\publish\
        echo.
        echo Die EXE kann ohne .NET Runtime ausgeführt werden.
    )
)

if "%choice%"=="4" (
    echo.
    echo Bereinige Projekt...
    dotnet clean
    if exist bin rmdir /s /q bin
    if exist obj rmdir /s /q obj
    echo ✓ Projekt bereinigt!
)

if "%choice%"=="5" (
    echo.
    echo Starte Anwendung...
    dotnet run
)

echo.
pause
