param(
    [string]$Path = "D:\",
    [int]$Top = 20
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "Scanning path: $Path"
Write-Host "Top folders: $Top"

$scriptDir = if ($PSScriptRoot) {
    $PSScriptRoot
} else {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}

$folders = Get-ChildItem -LiteralPath $Path -Directory -Force -ErrorAction SilentlyContinue

$results = @()

foreach ($folder in $folders) {
    Write-Host "Scanning:" $folder.FullName

    $size = (
        Get-ChildItem -LiteralPath $folder.FullName -Recurse -File -Force -ErrorAction SilentlyContinue |
        Measure-Object -Property Length -Sum
    ).Sum

    if ($null -eq $size) {
        $size = 0
    }

    $results += [PSCustomObject]@{
        Name = $folder.Name
        Path = $folder.FullName
        SizeGB = [math]::Round($size / 1GB, 2)
    }
}

$results = $results | Sort-Object SizeGB -Descending | Select-Object -First $Top

$maxSize = ($results | Measure-Object SizeGB -Maximum).Maximum

if ($null -eq $maxSize -or $maxSize -eq 0) {
    $maxSize = 1
}

function HtmlEncode($text) {
    return [System.Net.WebUtility]::HtmlEncode([string]$text)
}

$htmlRows = ""

foreach ($item in $results) {
    $percent = [math]::Round(($item.SizeGB / $maxSize) * 100, 1)
    $safeName = HtmlEncode $item.Name
    $safePath = HtmlEncode $item.Path

    $htmlRows += @"
<tr>
    <td>$safeName</td>
    <td>$($item.SizeGB) GB</td>
    <td>
        <div class="bar-bg">
            <div class="bar" style="width: $percent%;"></div>
        </div>
    </td>
    <td class="path">$safePath</td>
</tr>
"@
}

if ($htmlRows -eq "") {
    $htmlRows = @"
<tr>
    <td colspan="4">No folder data found.</td>
</tr>
"@
}

$date = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$html = @"
<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<title>Folder Radar</title>
<style>
    body {
        font-family: "Microsoft YaHei", Arial, sans-serif;
        background: #111827;
        color: #e5e7eb;
        padding: 30px;
    }
    h1 {
        color: #f9fafb;
        margin-bottom: 10px;
    }
    .info {
        color: #9ca3af;
        margin-bottom: 8px;
    }
    table {
        width: 100%;
        border-collapse: collapse;
        background: #1f2937;
        border-radius: 10px;
        overflow: hidden;
        margin-top: 20px;
    }
    th, td {
        padding: 12px 14px;
        border-bottom: 1px solid #374151;
        text-align: left;
    }
    th {
        background: #374151;
    }
    .bar-bg {
        width: 100%;
        height: 18px;
        background: #374151;
        border-radius: 999px;
        overflow: hidden;
    }
    .bar {
        height: 100%;
        background: linear-gradient(90deg, #60a5fa, #a78bfa);
    }
    .path {
        color: #9ca3af;
        font-size: 13px;
    }
</style>
</head>
<body>
    <h1>Folder Radar</h1>
    <div class="info">Scan Path: $Path</div>
    <div class="info">Top: $Top</div>
    <div class="info">Generated At: $date</div>

    <table>
        <tr>
            <th>Folder</th>
            <th>Size</th>
            <th>Ratio</th>
            <th>Path</th>
        </tr>
        $htmlRows
    </table>
</body>
</html>
"@

$reportDir = Join-Path $scriptDir "ScriptReports"

if (!(Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir | Out-Null
}

$fileTimestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$output = Join-Path $reportDir "folder_radar_report_$fileTimestamp.html"

$html | Out-File -FilePath $output -Encoding utf8

Write-Host "Report generated:" $output

Start-Process $output