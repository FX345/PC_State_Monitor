param(
    [string[]]$PingTargets = @("1.1.1.1", "8.8.8.8", "www.microsoft.com", "github.com"),
    [string[]]$DnsTargets = @("www.microsoft.com", "github.com", "openai.com"),
    [int]$PingCount = 4,
    [switch]$CheckPublicIP
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "SilentlyContinue"

Write-Host "Generating network status report..."

$scriptDir = if ($PSScriptRoot) {
    $PSScriptRoot
} else {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}

$reportDir = Join-Path $scriptDir "ScriptReports"

if (!(Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir | Out-Null
}

$fileTimestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$output = Join-Path $reportDir "network_status_report_$fileTimestamp.html"

function SafeText($value) {
    if ($null -eq $value) {
        return "N/A"
    }

    if ($value -is [array]) {
        $value = $value -join ", "
    }

    return [System.Net.WebUtility]::HtmlEncode([string]$value)
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

function RunCommandText($command) {
    try {
        $result = Invoke-Expression $command 2>&1 | Out-String
        if ([string]::IsNullOrWhiteSpace($result)) {
            return "N/A"
        }
        return $result.Trim()
    } catch {
        return "N/A"
    }
}

function TestPing($target, $count) {
    try {
        $replies = Test-Connection -ComputerName $target -Count $count -ErrorAction SilentlyContinue

        $times = @()

        foreach ($reply in $replies) {
            if ($null -ne $reply.ResponseTime) {
                $times += [double]$reply.ResponseTime
            } elseif ($null -ne $reply.Latency) {
                $times += [double]$reply.Latency
            }
        }

        $received = $times.Count
        $lossPercent = [math]::Round((($count - $received) / $count) * 100, 1)

        $avg = if ($received -gt 0) {
            [math]::Round(($times | Measure-Object -Average).Average, 2)
        } else {
            "N/A"
        }

        $min = if ($received -gt 0) {
            [math]::Round(($times | Measure-Object -Minimum).Minimum, 2)
        } else {
            "N/A"
        }

        $max = if ($received -gt 0) {
            [math]::Round(($times | Measure-Object -Maximum).Maximum, 2)
        } else {
            "N/A"
        }

        $status = if ($received -eq $count) {
            "OK"
        } elseif ($received -gt 0) {
            "Partial"
        } else {
            "Failed"
        }

        return [PSCustomObject]@{
            Target = $target
            Status = $status
            Sent = $count
            Received = $received
            LossPercent = $lossPercent
            AvgMs = $avg
            MinMs = $min
            MaxMs = $max
        }
    } catch {
        return [PSCustomObject]@{
            Target = $target
            Status = "Failed"
            Sent = $count
            Received = 0
            LossPercent = 100
            AvgMs = "N/A"
            MinMs = "N/A"
            MaxMs = "N/A"
        }
    }
}

function TestDns($target) {
    try {
        $records = Resolve-DnsName -Name $target -ErrorAction Stop |
            Where-Object { $_.IPAddress } |
            Select-Object -ExpandProperty IPAddress

        if ($records.Count -gt 0) {
            return [PSCustomObject]@{
                Target = $target
                Status = "OK"
                Result = ($records -join ", ")
            }
        } else {
            return [PSCustomObject]@{
                Target = $target
                Status = "No IP"
                Result = "N/A"
            }
        }
    } catch {
        return [PSCustomObject]@{
            Target = $target
            Status = "Failed"
            Result = "N/A"
        }
    }
}

$generatedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$adapters = try { Get-NetAdapter | Sort-Object Status, Name } catch { @() }
$ipConfigs = try { Get-NetIPConfiguration } catch { @() }
$dnsClient = try { Get-DnsClientServerAddress | Where-Object { $_.ServerAddresses.Count -gt 0 } } catch { @() }
$routes = try { Get-NetRoute -DestinationPrefix "0.0.0.0/0" | Sort-Object RouteMetric | Select-Object -First 8 } catch { @() }
$tcpConnections = try { Get-NetTCPConnection -State Established | Select-Object -First 40 } catch { @() }

$proxySettings = try {
    Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings"
} catch {
    $null
}

$proxyEnabled = if ($proxySettings.ProxyEnable -eq 1) { "Enabled" } else { "Disabled" }
$proxyServer = if ($proxySettings.ProxyServer) { $proxySettings.ProxyServer } else { "N/A" }
$autoConfigUrl = if ($proxySettings.AutoConfigURL) { $proxySettings.AutoConfigURL } else { "N/A" }

$winHttpProxy = RunCommandText "netsh winhttp show proxy"
$wifiInfo = RunCommandText "netsh wlan show interfaces"

$publicIP = "Not checked. Run with -CheckPublicIP to check public IP."

if ($CheckPublicIP) {
    try {
        $publicIP = Invoke-RestMethod -Uri "https://api.ipify.org" -TimeoutSec 5
    } catch {
        $publicIP = "Failed to check public IP."
    }
}

$pingResults = foreach ($target in $PingTargets) {
    TestPing $target $PingCount
}

$dnsResults = foreach ($target in $DnsTargets) {
    TestDns $target
}

$pingOkCount = ($pingResults | Where-Object { $_.Received -gt 0 }).Count
$dnsOkCount = ($dnsResults | Where-Object { $_.Status -eq "OK" }).Count
$activeAdapterCount = ($adapters | Where-Object { $_.Status -eq "Up" }).Count

$warnings = @()

if ($pingOkCount -eq 0) {
    $warnings += "All ping tests failed. Internet connection may be down, blocked, or affected by firewall/proxy settings."
} elseif ($pingOkCount -lt $PingTargets.Count) {
    $warnings += "Some ping tests failed. Network may be partially blocked or unstable."
}

if ($dnsOkCount -eq 0) {
    $warnings += "All DNS resolution tests failed. DNS settings may have issues."
} elseif ($dnsOkCount -lt $DnsTargets.Count) {
    $warnings += "Some DNS resolution tests failed."
}

if ($proxyEnabled -eq "Enabled") {
    $warnings += "System proxy is enabled. This may affect browsers, launchers, and some apps."
}

if ($activeAdapterCount -eq 0) {
    $warnings += "No active network adapter found."
}

if ($warnings.Count -eq 0) {
    $warnings += "No obvious network warning found."
}

$adapterRows = foreach ($adapter in $adapters) {
@"
<tr>
    <td>$(SafeText $adapter.Name)</td>
    <td>$(SafeText $adapter.InterfaceDescription)</td>
    <td>$(SafeText $adapter.Status)</td>
    <td>$(SafeText $adapter.LinkSpeed)</td>
    <td>$(SafeText $adapter.MacAddress)</td>
</tr>
"@
}
$adapterRows = $adapterRows -join "`n"

if ([string]::IsNullOrWhiteSpace($adapterRows)) {
    $adapterRows = "<tr><td colspan='5'>No adapter data found.</td></tr>"
}

$ipRows = foreach ($config in $ipConfigs) {
    $ipv4 = if ($config.IPv4Address) { ($config.IPv4Address.IPAddress -join ", ") } else { "N/A" }
    $ipv6 = if ($config.IPv6Address) { ($config.IPv6Address.IPAddress -join ", ") } else { "N/A" }
    $gateway = if ($config.IPv4DefaultGateway) { ($config.IPv4DefaultGateway.NextHop -join ", ") } else { "N/A" }
    $dns = if ($config.DNSServer) { ($config.DNSServer.ServerAddresses -join ", ") } else { "N/A" }

@"
<tr>
    <td>$(SafeText $config.InterfaceAlias)</td>
    <td>$(SafeText $ipv4)</td>
    <td>$(SafeText $ipv6)</td>
    <td>$(SafeText $gateway)</td>
    <td>$(SafeText $dns)</td>
</tr>
"@
}
$ipRows = $ipRows -join "`n"

if ([string]::IsNullOrWhiteSpace($ipRows)) {
    $ipRows = "<tr><td colspan='5'>No IP configuration data found.</td></tr>"
}

$dnsRows = foreach ($dns in $dnsClient) {
@"
<tr>
    <td>$(SafeText $dns.InterfaceAlias)</td>
    <td>$(SafeText $dns.AddressFamily)</td>
    <td>$(SafeText $dns.ServerAddresses)</td>
</tr>
"@
}
$dnsRows = $dnsRows -join "`n"

if ([string]::IsNullOrWhiteSpace($dnsRows)) {
    $dnsRows = "<tr><td colspan='3'>No DNS server data found.</td></tr>"
}

$routeRows = foreach ($route in $routes) {
@"
<tr>
    <td>$(SafeText $route.InterfaceAlias)</td>
    <td>$(SafeText $route.NextHop)</td>
    <td>$(SafeText $route.RouteMetric)</td>
    <td>$(SafeText $route.InterfaceMetric)</td>
</tr>
"@
}
$routeRows = $routeRows -join "`n"

if ([string]::IsNullOrWhiteSpace($routeRows)) {
    $routeRows = "<tr><td colspan='4'>No default route data found.</td></tr>"
}

$pingRows = foreach ($p in $pingResults) {
    $successPercent = if ($p.Sent -gt 0) {
        [math]::Round(($p.Received / $p.Sent) * 100, 1)
    } else {
        0
    }

    $bar = Bar $successPercent

@"
<tr>
    <td>$(SafeText $p.Target)</td>
    <td>$(SafeText $p.Status)</td>
    <td>$($p.Received) / $($p.Sent)</td>
    <td>$($p.LossPercent)%</td>
    <td>$($p.AvgMs)</td>
    <td>$($p.MinMs)</td>
    <td>$($p.MaxMs)</td>
    <td>$bar</td>
</tr>
"@
}
$pingRows = $pingRows -join "`n"

$dnsTestRows = foreach ($d in $dnsResults) {
@"
<tr>
    <td>$(SafeText $d.Target)</td>
    <td>$(SafeText $d.Status)</td>
    <td>$(SafeText $d.Result)</td>
</tr>
"@
}
$dnsTestRows = $dnsTestRows -join "`n"

$tcpRows = foreach ($conn in $tcpConnections) {
    $processName = "N/A"

    try {
        $processName = (Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue).ProcessName
    } catch {
        $processName = "N/A"
    }

@"
<tr>
    <td>$(SafeText $conn.LocalAddress):$(SafeText $conn.LocalPort)</td>
    <td>$(SafeText $conn.RemoteAddress):$(SafeText $conn.RemotePort)</td>
    <td>$(SafeText $conn.State)</td>
    <td>$(SafeText $conn.OwningProcess)</td>
    <td>$(SafeText $processName)</td>
</tr>
"@
}
$tcpRows = $tcpRows -join "`n"

if ([string]::IsNullOrWhiteSpace($tcpRows)) {
    $tcpRows = "<tr><td colspan='5'>No established TCP connection data found.</td></tr>"
}

$warningRows = foreach ($w in $warnings) {
    "<li>$(SafeText $w)</li>"
}
$warningRows = $warningRows -join "`n"

$wifiInfoSafe = SafeText $wifiInfo
$winHttpProxySafe = SafeText $winHttpProxy

$html = @"
<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<title>Network Status Report</title>
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
        margin-bottom: 8px;
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
    pre {
        background: #1f2937;
        border: 1px solid #374151;
        border-radius: 10px;
        padding: 14px;
        white-space: pre-wrap;
        word-break: break-word;
        color: #d1d5db;
    }
    @media (max-width: 1000px) {
        .cards {
            grid-template-columns: repeat(2, minmax(180px, 1fr));
        }
    }
</style>
</head>
<body>
    <h1>Network Status Report</h1>
    <div class="sub">Generated at: $generatedAt</div>
    <div class="sub">Computer: $(SafeText $env:COMPUTERNAME)</div>

    <div class="cards">
        <div class="card">
            <div class="card-title">Active Adapters</div>
            <div class="card-value">$activeAdapterCount</div>
            <div class="card-sub">Network adapters currently up</div>
        </div>
        <div class="card">
            <div class="card-title">Ping Tests</div>
            <div class="card-value">$pingOkCount / $($PingTargets.Count)</div>
            <div class="card-sub">Targets reachable</div>
        </div>
        <div class="card">
            <div class="card-title">DNS Tests</div>
            <div class="card-value">$dnsOkCount / $($DnsTargets.Count)</div>
            <div class="card-sub">Domains resolved</div>
        </div>
        <div class="card">
            <div class="card-title">System Proxy</div>
            <div class="card-value">$(SafeText $proxyEnabled)</div>
            <div class="card-sub">$(SafeText $proxyServer)</div>
        </div>
    </div>

    <div class="warning-box">
        <strong>Quick Notes</strong>
        <ul>
            $warningRows
        </ul>
    </div>

    <h2>Basic Network Info</h2>
    <table>
        <tr><th>Item</th><th>Value</th></tr>
        <tr><td>Public IP</td><td>$(SafeText $publicIP)</td></tr>
        <tr><td>System Proxy</td><td>$(SafeText $proxyEnabled)</td></tr>
        <tr><td>Proxy Server</td><td>$(SafeText $proxyServer)</td></tr>
        <tr><td>Auto Config URL</td><td>$(SafeText $autoConfigUrl)</td></tr>
    </table>

    <h2>Network Adapters</h2>
    <table>
        <tr><th>Name</th><th>Description</th><th>Status</th><th>Link Speed</th><th>MAC</th></tr>
        $adapterRows
    </table>

    <h2>IP Configuration</h2>
    <table>
        <tr><th>Interface</th><th>IPv4</th><th>IPv6</th><th>Gateway</th><th>DNS</th></tr>
        $ipRows
    </table>

    <h2>DNS Server Settings</h2>
    <table>
        <tr><th>Interface</th><th>Address Family</th><th>DNS Servers</th></tr>
        $dnsRows
    </table>

    <h2>Default Routes</h2>
    <table>
        <tr><th>Interface</th><th>Next Hop</th><th>Route Metric</th><th>Interface Metric</th></tr>
        $routeRows
    </table>

    <h2>Ping Tests</h2>
    <table>
        <tr><th>Target</th><th>Status</th><th>Received</th><th>Loss</th><th>Avg ms</th><th>Min ms</th><th>Max ms</th><th>Bar</th></tr>
        $pingRows
    </table>

    <h2>DNS Resolution Tests</h2>
    <table>
        <tr><th>Domain</th><th>Status</th><th>Result</th></tr>
        $dnsTestRows
    </table>

    <h2>Established TCP Connections</h2>
    <table>
        <tr><th>Local</th><th>Remote</th><th>State</th><th>PID</th><th>Process</th></tr>
        $tcpRows
    </table>

    <h2>Wi-Fi Info</h2>
    <pre>$wifiInfoSafe</pre>

    <h2>WinHTTP Proxy</h2>
    <pre>$winHttpProxySafe</pre>
</body>
</html>
"@

$html | Out-File -FilePath $output -Encoding utf8

Write-Host "Report generated:"
Write-Host $output

Start-Process $output