$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseDirectory = Join-Path $root "release"
$setupSource = Join-Path $root "installer-output\PcGuardianLiteSetup.exe"
$setupTarget = Join-Path $releaseDirectory "PcGuardianLiteSetup.exe"
$readmeSource = Join-Path $root "README.md"

& (Join-Path $root "build_installer.ps1")

if (!(Test-Path $setupSource)) {
    throw "Installer was not created: $setupSource"
}

if (Test-Path $releaseDirectory) {
    Remove-Item -LiteralPath $releaseDirectory -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
Copy-Item -LiteralPath $setupSource -Destination $setupTarget -Force
Copy-Item -LiteralPath $readmeSource -Destination (Join-Path $releaseDirectory "README.md") -Force

@"
PcGuardianLite release package

1. Double-click PcGuardianLiteSetup.exe to install.
2. The installer lets users choose whether to create a desktop shortcut.
3. After installation, the floating ball appears on the desktop.
4. Do not send GitHub Source code.zip to normal users as the installer.
5. See README.md for the full Chinese guide.
"@ | Set-Content -LiteralPath (Join-Path $releaseDirectory "quick-start.txt") -Encoding UTF8

Write-Host ""
Write-Host "Release ready:"
Write-Host $releaseDirectory
Write-Host $setupTarget
