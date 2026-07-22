param(
    [int]$TopProcesses = 15,
    [int]$RecentErrorDays = 7
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "SilentlyContinue"

Write-Host "Generating PC status report..."

$scriptDir = if ($PSScriptRoot) {
    $PSScriptRoot
} else {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}

$toolsDir = Split-Path -Parent $scriptDir
$installRootCandidate = Split-Path -Parent $toolsDir
$reportBaseDir = if ((Split-Path -Leaf $scriptDir) -ieq "scripts" -and (Split-Path -Leaf $toolsDir) -ieq "tools") {
    $installRootCandidate
} else {
    $scriptDir
}

$reportDir = Join-Path $reportBaseDir "ScriptReports"

if (!(Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir | Out-Null
}

$fileTimestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$output = Join-Path $reportDir "pc_status_report_$fileTimestamp.html"

function SafeText($value) {
    if ($null -eq $value) {
        return "N/A"
    }
    return [System.Net.WebUtility]::HtmlEncode([string]$value)
}

function ToGB($bytes) {
    if ($null -eq $bytes -or $bytes -le 0) {
        return "N/A"
    }
    return "{0:N2} GB" -f ([double]$bytes / 1GB)
}

function ToMB($bytes) {
    if ($null -eq $bytes -or $bytes -le 0) {
        return "N/A"
    }
    return "{0:N0} MB" -f ([double]$bytes / 1MB)
}

function Percent($used, $total) {
    if ($null -eq $total -or $total -eq 0) {
        return 0
    }
    return [math]::Round(($used / $total) * 100, 1)
}

function Bar($percent) {
    if ($null -eq $percent) {
        $percent = 0
    }

    if ($percent -lt 0) {
        $percent = 0
    }

    if ($percent -gt 100) {
        $percent = 100
    }

    return "<div class='bar-bg'><div class='bar' style='width: $percent%;'></div></div>"
}

function Get-CimSafe($className, $namespace = "root/cimv2", $filter = $null) {
    try {
        if ($filter) {
            return Get-CimInstance -Namespace $namespace -ClassName $className -Filter $filter -ErrorAction Stop
        } else {
            return Get-CimInstance -Namespace $namespace -ClassName $className -ErrorAction Stop
        }
    } catch {
        return @()
    }
}

$now = Get-Date
$os = Get-CimSafe "Win32_OperatingSystem" | Select-Object -First 1
$computer = Get-CimSafe "Win32_ComputerSystem" | Select-Object -First 1
$cpu = Get-CimSafe "Win32_Processor" | Select-Object -First 1
$gpus = Get-CimSafe "Win32_VideoController"
$ramModules = Get-CimSafe "Win32_PhysicalMemory"
$logicalDisks = Get-CimSafe "Win32_LogicalDisk" "root/cimv2" "DriveType=3"
$bios = Get-CimSafe "Win32_BIOS" | Select-Object -First 1
$baseboard = Get-CimSafe "Win32_BaseBoard" | Select-Object -First 1
$battery = Get-CimSafe "Win32_Battery"
$networkAdapters = Get-CimSafe "Win32_NetworkAdapterConfiguration" | Where-Object { $_.IPEnabled -eq $true }
$startupItems = Get-CimSafe "Win32_StartupCommand"
$updates = Get-CimSafe "Win32_QuickFixEngineering" | Sort-Object InstalledOn -Descending | Select-Object -First 10

$antivirus = try {
    Get-CimInstance -Namespace "root/SecurityCenter2" -ClassName "AntiVirusProduct" -ErrorAction Stop
} catch {
    @()
}

$totalRam = $computer.TotalPhysicalMemory
$freeRam = if ($os.FreePhysicalMemory) { [double]$os.FreePhysicalMemory * 1KB } else { 0 }
$usedRam = $totalRam - $freeRam
$ramUsedPercent = Percent $usedRam $totalRam

$totalDisk = ($logicalDisks | Measure-Object -Property Size -Sum).Sum
$freeDisk = ($logicalDisks | Measure-Object -Property FreeSpace -Sum).Sum
$usedDisk = $totalDisk - $freeDisk
$diskUsedPercent = Percent $usedDisk $totalDisk

$bootTime = $os.LastBootUpTime
$uptime = if ($bootTime) {
    New-TimeSpan -Start $bootTime -End $now
} else {
    $null
}

$uptimeText = if ($uptime) {
    "{0} days {1} hours {2} minutes" -f $uptime.Days, $uptime.Hours, $uptime.Minutes
} else {
    "N/A"
}

$topProcesses = Get-Process |
    Sort-Object WorkingSet64 -Descending |
    Select-Object -First $TopProcesses

$recentErrors = try {
    Get-WinEvent -FilterHashtable @{
        LogName = "System"
        Level = 2
        StartTime = (Get-Date).AddDays(-$RecentErrorDays)
    } -MaxEvents 12 -ErrorAction Stop
} catch {
    @()
}

$warnings = @()

if ($diskUsedPercent -ge 90) {
    $warnings += "Disk usage is very high. Consider cleaning large files."
} elseif ($diskUsedPercent -ge 80) {
    $warnings += "Disk usage is above 80%."
}

if ($ramUsedPercent -ge 85) {
    $warnings += "Memory usage is high."
}

if ($uptime -and $uptime.Days -ge 7) {
    $warnings += "System has been running for more than 7 days. A restart may help performance."
}

if ($recentErrors.Count -gt 0) {
    $warnings += "Recent system errors found in Windows Event Log."
}

if ($warnings.Count -eq 0) {
    $warnings += "No obvious warning found."
}

$summaryCards = @"
<div class="cards">
    <div class="card">
        <div class="card-title">OS</div>
        <div class="card-value">$(SafeText $os.Caption)</div>
        <div class="card-sub">Build $(SafeText $os.BuildNumber)</div>
    </div>
    <div class="card">
        <div class="card-title">CPU</div>
        <div class="card-value">$(SafeText $cpu.Name)</div>
        <div class="card-sub">$($cpu.NumberOfCores) cores / $($cpu.NumberOfLogicalProcessors) threads</div>
    </div>
    <div class="card">
        <div class="card-title">RAM Usage</div>
        <div class="card-value">$ramUsedPercent%</div>
        <div class="card-sub">$(ToGB $usedRam) / $(ToGB $totalRam)</div>
    </div>
    <div class="card">
        <div class="card-title">Disk Usage</div>
        <div class="card-value">$diskUsedPercent%</div>
        <div class="card-sub">$(ToGB $usedDisk) / $(ToGB $totalDisk)</div>
    </div>
</div>
"@

$systemRows = @"
<tr><td>Computer Name</td><td>$(SafeText $env:COMPUTERNAME)</td></tr>
<tr><td>User</td><td>$(SafeText $env:USERNAME)</td></tr>
<tr><td>Manufacturer</td><td>$(SafeText $computer.Manufacturer)</td></tr>
<tr><td>Model</td><td>$(SafeText $computer.Model)</td></tr>
<tr><td>System Type</td><td>$(SafeText $computer.SystemType)</td></tr>
<tr><td>OS</td><td>$(SafeText $os.Caption)</td></tr>
<tr><td>OS Version</td><td>$(SafeText $os.Version)</td></tr>
<tr><td>Build Number</td><td>$(SafeText $os.BuildNumber)</td></tr>
<tr><td>Install Date</td><td>$(SafeText $os.InstallDate)</td></tr>
<tr><td>Last Boot Time</td><td>$(SafeText $bootTime)</td></tr>
<tr><td>Uptime</td><td>$(SafeText $uptimeText)</td></tr>
"@

$hardwareRows = @"
<tr><td>CPU</td><td>$(SafeText $cpu.Name)</td></tr>
<tr><td>CPU Cores</td><td>$($cpu.NumberOfCores)</td></tr>
<tr><td>CPU Threads</td><td>$($cpu.NumberOfLogicalProcessors)</td></tr>
<tr><td>CPU Max Clock</td><td>$($cpu.MaxClockSpeed) MHz</td></tr>
<tr><td>Total RAM</td><td>$(ToGB $totalRam)</td></tr>
<tr><td>Used RAM</td><td>$(ToGB $usedRam) / $ramUsedPercent%</td></tr>
<tr><td>Motherboard</td><td>$(SafeText $baseboard.Manufacturer) $(SafeText $baseboard.Product)</td></tr>
<tr><td>BIOS</td><td>$(SafeText $bios.Manufacturer) / $(SafeText $bios.SMBIOSBIOSVersion)</td></tr>
<tr><td>BIOS Release Date</td><td>$(SafeText $bios.ReleaseDate)</td></tr>
"@

$gpuRows = foreach ($gpu in $gpus) {
    $vram = if ($gpu.AdapterRAM) { ToGB $gpu.AdapterRAM } else { "N/A" }
    @"
<tr>
    <td>$(SafeText $gpu.Name)</td>
    <td>$vram</td>
    <td>$(SafeText $gpu.DriverVersion)</td>
    <td>$(SafeText $gpu.VideoProcessor)</td>
</tr>
"@
}
$gpuRows = $gpuRows -join "`n"
if ([string]::IsNullOrWhiteSpace($gpuRows)) {
    $gpuRows = "<tr><td colspan='4'>No GPU data found.</td></tr>"
}

$ramRows = foreach ($ram in $ramModules) {
    @"
<tr>
    <td>$(SafeText $ram.Manufacturer)</td>
    <td>$(ToGB $ram.Capacity)</td>
    <td>$(SafeText $ram.Speed) MHz</td>
    <td>$(SafeText $ram.PartNumber)</td>
</tr>
"@
}
$ramRows = $ramRows -join "`n"
if ([string]::IsNullOrWhiteSpace($ramRows)) {
    $ramRows = "<tr><td colspan='4'>No RAM module data found.</td></tr>"
}

$diskRows = foreach ($disk in $logicalDisks) {
    $used = $disk.Size - $disk.FreeSpace
    $usedPercent = Percent $used $disk.Size
    $bar = Bar $usedPercent

    @"
<tr>
    <td>$(SafeText $disk.DeviceID)</td>
    <td>$(SafeText $disk.VolumeName)</td>
    <td>$(ToGB $disk.Size)</td>
    <td>$(ToGB $used)</td>
    <td>$(ToGB $disk.FreeSpace)</td>
    <td>$usedPercent%</td>
    <td>$bar</td>
</tr>
"@
}
$diskRows = $diskRows -join "`n"
if ([string]::IsNullOrWhiteSpace($diskRows)) {
    $diskRows = "<tr><td colspan='7'>No disk data found.</td></tr>"
}

$batteryRows = foreach ($b in $battery) {
    @"
<tr>
    <td>$(SafeText $b.Name)</td>
    <td>$(SafeText $b.EstimatedChargeRemaining)%</td>
    <td>$(SafeText $b.BatteryStatus)</td>
    <td>$(SafeText $b.EstimatedRunTime)</td>
</tr>
"@
}
$batteryRows = $batteryRows -join "`n"
if ([string]::IsNullOrWhiteSpace($batteryRows)) {
    $batteryRows = "<tr><td colspan='4'>No battery data found. This is normal for desktop PCs.</td></tr>"
}

$networkRows = foreach ($net in $networkAdapters) {
    $ips = if ($net.IPAddress) { ($net.IPAddress -join ", ") } else { "N/A" }
    $gateways = if ($net.DefaultIPGateway) { ($net.DefaultIPGateway -join ", ") } else { "N/A" }
    $dns = if ($net.DNSServerSearchOrder) { ($net.DNSServerSearchOrder -join ", ") } else { "N/A" }

    @"
<tr>
    <td>$(SafeText $net.Description)</td>
    <td>$(SafeText $net.MACAddress)</td>
    <td>$(SafeText $ips)</td>
    <td>$(SafeText $gateways)</td>
    <td>$(SafeText $dns)</td>
</tr>
"@
}
$networkRows = $networkRows -join "`n"
if ([string]::IsNullOrWhiteSpace($networkRows)) {
    $networkRows = "<tr><td colspan='5'>No active network adapter data found.</td></tr>"
}

$processRows = foreach ($p in $topProcesses) {
    @"
<tr>
    <td>$(SafeText $p.ProcessName)</td>
    <td>$($p.Id)</td>
    <td>$(ToMB $p.WorkingSet64)</td>
    <td>$(ToMB $p.PrivateMemorySize64)</td>
    <td>$([math]::Round($p.CPU, 2))</td>
</tr>
"@
}
$processRows = $processRows -join "`n"
if ([string]::IsNullOrWhiteSpace($processRows)) {
    $processRows = "<tr><td colspan='5'>No process data found.</td></tr>"
}

$startupRows = foreach ($item in ($startupItems | Select-Object -First 40)) {
    @"
<tr>
    <td>$(SafeText $item.Name)</td>
    <td>$(SafeText $item.Location)</td>
    <td class="path">$(SafeText $item.Command)</td>
</tr>
"@
}
$startupRows = $startupRows -join "`n"
if ([string]::IsNullOrWhiteSpace($startupRows)) {
    $startupRows = "<tr><td colspan='3'>No startup item data found.</td></tr>"
}

$updateRows = foreach ($u in $updates) {
    @"
<tr>
    <td>$(SafeText $u.HotFixID)</td>
    <td>$(SafeText $u.Description)</td>
    <td>$(SafeText $u.InstalledOn)</td>
</tr>
"@
}
$updateRows = $updateRows -join "`n"
if ([string]::IsNullOrWhiteSpace($updateRows)) {
    $updateRows = "<tr><td colspan='3'>No update data found.</td></tr>"
}

$antivirusRows = foreach ($av in $antivirus) {
    @"
<tr>
    <td>$(SafeText $av.displayName)</td>
    <td>$(SafeText $av.pathToSignedProductExe)</td>
</tr>
"@
}
$antivirusRows = $antivirusRows -join "`n"
if ([string]::IsNullOrWhiteSpace($antivirusRows)) {
    $antivirusRows = "<tr><td colspan='2'>No antivirus data found.</td></tr>"
}

$errorRows = foreach ($e in $recentErrors) {
    $msg = $e.Message
    if ($msg -and $msg.Length -gt 250) {
        $msg = $msg.Substring(0, 250) + "..."
    }

    @"
<tr>
    <td>$(SafeText $e.TimeCreated)</td>
    <td>$(SafeText $e.ProviderName)</td>
    <td>$(SafeText $e.Id)</td>
    <td class="path">$(SafeText $msg)</td>
</tr>
"@
}
$errorRows = $errorRows -join "`n"
if ([string]::IsNullOrWhiteSpace($errorRows)) {
    $errorRows = "<tr><td colspan='4'>No recent system errors found.</td></tr>"
}

$warningRows = foreach ($w in $warnings) {
    "<li>$(SafeText $w)</li>"
}
$warningRows = $warningRows -join "`n"

$generatedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$html = @"
<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<title>PC Status Report</title>
<style>
    body {
        font-family: "Microsoft YaHei", Arial, sans-serif;
        background: #111827;
        color: #e5e7eb;
        padding: 28px;
    }
    h1 {
        margin-bottom: 8px;
        color: #f9fafb;
    }
    h2 {
        margin-top: 32px;
        color: #f9fafb;
        border-left: 5px solid #60a5fa;
        padding-left: 10px;
    }
    .sub {
        color: #9ca3af;
        margin-bottom: 20px;
    }
    .cards {
        display: grid;
        grid-template-columns: repeat(4, minmax(180px, 1fr));
        gap: 16px;
        margin-top: 20px;
        margin-bottom: 24px;
    }
    .card {
        background: #1f2937;
        border: 1px solid #374151;
        border-radius: 14px;
        padding: 18px;
    }
    .card-title {
        color: #9ca3af;
        font-size: 13px;
        margin-bottom: 8px;
    }
    .card-value {
        color: #f9fafb;
        font-size: 20px;
        font-weight: 700;
        word-break: break-word;
    }
    .card-sub {
        color: #9ca3af;
        margin-top: 8px;
        font-size: 13px;
    }
    table {
        width: 100%;
        border-collapse: collapse;
        background: #1f2937;
        border-radius: 10px;
        overflow: hidden;
        margin-top: 14px;
    }
    th, td {
        padding: 11px 13px;
        border-bottom: 1px solid #374151;
        text-align: left;
        vertical-align: top;
    }
    th {
        background: #374151;
        color: #f9fafb;
    }
    td {
        color: #e5e7eb;
    }
    .path {
        color: #9ca3af;
        font-size: 13px;
        word-break: break-all;
    }
    .bar-bg {
        width: 100%;
        height: 16px;
        background: #374151;
        border-radius: 999px;
        overflow: hidden;
    }
    .bar {
        height: 100%;
        background: linear-gradient(90deg, #60a5fa, #a78bfa);
    }
    .warning-box {
        background: #1f2937;
        border: 1px solid #f59e0b;
        border-radius: 12px;
        padding: 16px 20px;
        margin-top: 20px;
    }
    .warning-box ul {
        margin-top: 8px;
        margin-bottom: 0;
    }
    @media (max-width: 1000px) {
        .cards {
            grid-template-columns: repeat(2, minmax(180px, 1fr));
        }
    }
</style>
</head>
<body>
    <h1>PC Status Report</h1>
    <div class="sub">Generated at: $generatedAt</div>
    <div class="sub">Computer: $(SafeText $env:COMPUTERNAME)</div>

    $summaryCards

    <div class="warning-box">
        <strong>Quick Notes</strong>
        <ul>
            $warningRows
        </ul>
    </div>

    <h2>System Overview</h2>
    <table>
        <tr><th>Item</th><th>Value</th></tr>
        $systemRows
    </table>

    <h2>Hardware Overview</h2>
    <table>
        <tr><th>Item</th><th>Value</th></tr>
        $hardwareRows
    </table>

    <h2>GPU</h2>
    <table>
        <tr><th>Name</th><th>VRAM</th><th>Driver Version</th><th>Video Processor</th></tr>
        $gpuRows
    </table>

    <h2>RAM Modules</h2>
    <table>
        <tr><th>Manufacturer</th><th>Capacity</th><th>Speed</th><th>Part Number</th></tr>
        $ramRows
    </table>

    <h2>Disk Usage</h2>
    <table>
        <tr><th>Drive</th><th>Label</th><th>Total</th><th>Used</th><th>Free</th><th>Used %</th><th>Bar</th></tr>
        $diskRows
    </table>

    <h2>Battery</h2>
    <table>
        <tr><th>Name</th><th>Charge</th><th>Status Code</th><th>Estimated Runtime</th></tr>
        $batteryRows
    </table>

    <h2>Network</h2>
    <table>
        <tr><th>Adapter</th><th>MAC</th><th>IP Address</th><th>Gateway</th><th>DNS</th></tr>
        $networkRows
    </table>

    <h2>Top Memory Processes</h2>
    <table>
        <tr><th>Process</th><th>PID</th><th>Working Set</th><th>Private Memory</th><th>CPU Time</th></tr>
        $processRows
    </table>

    <h2>Startup Items</h2>
    <table>
        <tr><th>Name</th><th>Location</th><th>Command</th></tr>
        $startupRows
    </table>

    <h2>Antivirus</h2>
    <table>
        <tr><th>Name</th><th>Path</th></tr>
        $antivirusRows
    </table>

    <h2>Recent Windows Updates</h2>
    <table>
        <tr><th>HotFix ID</th><th>Description</th><th>Installed On</th></tr>
        $updateRows
    </table>

    <h2>Recent System Errors</h2>
    <table>
        <tr><th>Time</th><th>Source</th><th>Event ID</th><th>Message</th></tr>
        $errorRows
    </table>
</body>
</html>
"@

$html | Out-File -FilePath $output -Encoding utf8

Write-Host "Report generated:"
Write-Host $output

Start-Process $output
