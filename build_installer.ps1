$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$scriptSourceDirectory = Join-Path $root "scripts"
$payloadDirectory = Join-Path $root "installer-payload"
$payloadZip = Join-Path $root "src\PcGuardianLite.Setup\Payload\payload.zip"
$setupOutput = Join-Path $root "installer-output"
$appProject = Join-Path $root "src\PcGuardianLite.App\PcGuardianLite.App.csproj"
$setupProject = Join-Path $root "src\PcGuardianLite.Setup\PcGuardianLite.Setup.csproj"

$requiredScripts = @(
    "pc_report.ps1",
    "network_report.ps1",
    "folder_radar.ps1",
    "ai_review_pack.ps1",
    "cmd_for_folder_radar.txt"
)

Write-Host "Building PcGuardianLite app payload..."
if (Test-Path $payloadDirectory) {
    Remove-Item -LiteralPath $payloadDirectory -Recurse -Force
}

dotnet publish $appProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    -o $payloadDirectory

if ($LASTEXITCODE -ne 0) {
    throw "App publish failed."
}

foreach ($scriptName in $requiredScripts) {
    $source = Join-Path $scriptSourceDirectory $scriptName
    if (!(Test-Path $source)) {
        throw "Missing required script: $source"
    }

    Copy-Item -LiteralPath $source -Destination $payloadDirectory -Force
}

$requiredPayloadFiles = @("PcGuardianLite.exe") + $requiredScripts
foreach ($fileName in $requiredPayloadFiles) {
    $filePath = Join-Path $payloadDirectory $fileName
    if (!(Test-Path $filePath)) {
        throw "Payload is missing required file: $fileName"
    }
}

Write-Host "Packing embedded payload..."
$payloadFolder = Split-Path -Parent $payloadZip
New-Item -ItemType Directory -Force -Path $payloadFolder | Out-Null
if (Test-Path $payloadZip) {
    Remove-Item -LiteralPath $payloadZip -Force
}
Compress-Archive -Path (Join-Path $payloadDirectory "*") -DestinationPath $payloadZip -Force

Write-Host "Building setup executable..."
if (Test-Path $setupOutput) {
    Remove-Item -LiteralPath $setupOutput -Recurse -Force
}

dotnet publish $setupProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    -o $setupOutput

if ($LASTEXITCODE -ne 0) {
    throw "Setup publish failed."
}

$setupExe = Join-Path $setupOutput "PcGuardianLiteSetup.exe"
if (!(Test-Path $setupExe)) {
    throw "Setup executable was not created: $setupExe"
}

Write-Host ""
Write-Host "Installer ready:"
Write-Host $setupExe
