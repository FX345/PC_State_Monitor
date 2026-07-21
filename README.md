# PcGuardianLite

PcGuardianLite is a lightweight Windows desktop monitor bundled with conservative diagnostic and report scripts.

It starts as a small always-on-top floating ball and expands into a compact status panel. It shows CPU usage, memory usage, upload speed, download speed, disk usage, and a conservative temperature status. The first version does not install drivers, clean files, kill processes, or change system settings.

## Features

- Draggable floating ball.
- Click-to-expand status panel.
- CPU, memory, disk, upload, and download status.
- Temperature displays `Not supported` unless a safe native reading is available.
- Buttons for the existing `pc_report.ps1`, `network_report.ps1`, and `folder_radar.ps1` scripts.
- Button to open `D:\scripts\ScriptReports`.
- Safe cleanup panel with scan-before-clean, selectable items, conservative temp-file cleanup, and optional recycle bin cleanup.

## Bundled Scripts

The PowerShell helper scripts are stored in:

```text
PcGuardianLite\scripts
```

The installer copies these scripts next to `PcGuardianLite.exe`, so the report buttons work on another computer without needing the original `D:\scripts` folder.

## Test

```powershell
dotnet run --project .\PcGuardianLite\tests\PcGuardianLite.Tests\PcGuardianLite.Tests.csproj
```

## Build

```powershell
dotnet build .\PcGuardianLite\PcGuardianLite.sln
```

## Publish

```powershell
dotnet publish .\PcGuardianLite\src\PcGuardianLite.App\PcGuardianLite.App.csproj -c Release -r win-x64 --self-contained false
```

The published exe is:

```text
D:\scripts\PcGuardianLite\src\PcGuardianLite.App\bin\Release\net8.0-windows\win-x64\publish\PcGuardianLite.exe
```

## Publish Single Exe Launcher

```powershell
dotnet publish .\PcGuardianLite\src\PcGuardianLite.App\PcGuardianLite.App.csproj /p:PublishProfile=SingleExe
```

Or double-click:

```text
D:\scripts\PcGuardianLite\publish_single_exe.bat
```

The single-file launcher is:

```text
D:\scripts\PcGuardianLite\publish-single\PcGuardianLite.exe
```

This is a small framework-dependent single exe. It expects .NET 8 Windows Desktop Runtime to be installed on the computer.

## Build Installer Exe

Use this when you want to send the app to another computer:

```powershell
.\PcGuardianLite\build_installer.ps1
```

Or double-click:

```text
D:\scripts\PcGuardianLite\build_installer.bat
```

The installer output is:

```text
D:\scripts\PcGuardianLite\installer-output\PcGuardianLiteSetup.exe
```

Send `PcGuardianLiteSetup.exe` to the other person. After they double-click it, the installer copies the app and bundled scripts to:

```text
%LOCALAPPDATA%\PcGuardianLite
```

The installer includes an option to create a desktop shortcut and an option to launch the app after installation. It is self-contained for Windows x64, so the target computer should not need a separate .NET runtime install.

## Uninstall

The installer registers PcGuardianLite in the current user's Windows uninstall list:

```text
Settings > Apps > Installed apps > PcGuardianLite > Uninstall
```

The install folder also contains:

```text
%LOCALAPPDATA%\PcGuardianLite\Uninstall PcGuardianLite.bat
```

Uninstall removes the app files, the desktop shortcut, and the current-user uninstall registry entry. Generated report folders such as `ScriptReports` are kept.

## Safe Cleanup

The cleanup panel is conservative by design:

- Scans first and lists the items that can be cleaned.
- Cleans only checked items.
- User temp files must be older than 24 hours.
- Windows temp files must be older than 7 days.
- Recycle Bin cleanup is optional and asks for confirmation.
- Reparse points, links, browser caches, Downloads, Desktop, Registry, Prefetch, Windows Update cache, and report folders are not cleaned.
- Files that are locked or not allowed by Windows are skipped instead of forced.

## Notes

The app finds PowerShell scripts by walking upward from its own executable folder until it sees either the scripts directly or a `scripts` folder containing them.
