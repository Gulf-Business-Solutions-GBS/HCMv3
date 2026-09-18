<#
    check-server.ps1

    READ-ONLY health check for the Webtropia server, run BEFORE installing
    Ollama for the AI Assistant. It changes nothing: no installs, no settings,
    no restarts.

    Run on the server in PowerShell (Administrator gives a fuller report, but
    is not required):

        Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
        .\check-server.ps1

    It prints the report and also saves a copy to the Desktop as
    AI-server-check-<date>.txt, so it can be sent back for review.

    The model thresholds match install-ollama.ps1 (qwen3:4b needs 8 GB RAM in
    total, 4 GB free, and 6 GB free disk).
#>

[CmdletBinding()]
param(
    [string] $Drive = 'C'
)

$ErrorActionPreference = 'Continue'
$report = New-Object System.Collections.Generic.List[string]

function Out-Line { param([string] $t = '') $report.Add($t); Write-Host $t }
function Head     { param([string] $t) Out-Line ''; Out-Line ('=' * 70); Out-Line " $t"; Out-Line ('=' * 70) }
function Row      { param([string] $k, $v) Out-Line ("  {0,-30} {1}" -f $k, $v) }
function Gb       { param($bytes) if ($null -eq $bytes) { return $null } [math]::Round([double]$bytes / 1GB, 1) }

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
           ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

Head "AI ASSISTANT - SERVER CHECK   $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
Row 'Computer'          $env:COMPUTERNAME
Row 'Run as admin'      $(if ($isAdmin) { 'yes' } else { 'no (some details may be missing)' })

# ---------------------------------------------------------------- system
Head 'SYSTEM'
$os  = Get-CimInstance Win32_OperatingSystem
$cs  = Get-CimInstance Win32_ComputerSystem
Row 'Windows'           "$($os.Caption) (build $($os.BuildNumber))"
Row 'Up since'          $os.LastBootUpTime.ToString('yyyy-MM-dd HH:mm')
Row 'Manufacturer'      "$($cs.Manufacturer) / $($cs.Model)"
Row 'Virtual machine'   $(if ($cs.Model -match 'Virtual|VMware|KVM|QEMU|HVM|Standard PC' -or $cs.Manufacturer -match 'QEMU|Xen|VMware|Microsoft Corporation|innotek') { 'yes - a VPS rarely has a usable GPU' } else { 'probably not' })

# ---------------------------------------------------------------- CPU
Head 'CPU'
$cpus    = @(Get-CimInstance Win32_Processor)
$cores   = ($cpus | Measure-Object NumberOfCores -Sum).Sum
$threads = ($cpus | Measure-Object NumberOfLogicalProcessors -Sum).Sum
Row 'Processor'         ($cpus[0].Name -replace '\s+', ' ').Trim()
Row 'Cores / threads'   "$cores / $threads"
Row 'Current load'      "$([math]::Round(($cpus | Measure-Object LoadPercentage -Average).Average))%"

# AVX2 matters: Ollama runs far slower on CPUs without it.
$avx2 = $null
try {
    Add-Type -Namespace Chk -Name Cpu -MemberDefinition '[DllImport("kernel32.dll")] public static extern bool IsProcessorFeaturePresent(uint f);' -EA Stop
    $avx2 = [Chk.Cpu]::IsProcessorFeaturePresent(40)   # PF_AVX2_INSTRUCTIONS_AVAILABLE
} catch { }
Row 'AVX2 support'      $(if ($avx2 -eq $true) { 'yes' } elseif ($avx2 -eq $false) { 'NO - model will be very slow' } else { 'could not tell' })

# ---------------------------------------------------------------- memory
Head 'MEMORY'
$totalGb = Gb ($os.TotalVisibleMemorySize * 1KB)
$freeGb  = Gb ($os.FreePhysicalMemory * 1KB)
$availGb = $freeGb
try { $availGb = [math]::Round((Get-Counter '\Memory\Available MBytes' -EA Stop).CounterSamples[0].CookedValue / 1024, 1) } catch { }
Row 'Total RAM'         "$totalGb GB"
Row 'Available now'     "$availGb GB  (free + reclaimable cache)"
Row 'Committed'         "$(Gb (($os.TotalVirtualMemorySize - $os.FreeVirtualMemory) * 1KB)) GB of $(Gb ($os.TotalVirtualMemorySize * 1KB)) GB limit"
$pf = Get-CimInstance Win32_PageFileUsage -EA SilentlyContinue
if ($pf) { Row 'Page file' "$(($pf | Measure-Object AllocatedBaseSize -Sum).Sum) MB on $(($pf.Name) -join ', ')" }

Out-Line ''
Out-Line '  Biggest memory users right now:'
Get-Process | Group-Object ProcessName | ForEach-Object {
    [pscustomobject]@{ Name = $_.Name; MB = [math]::Round(($_.Group | Measure-Object WorkingSet64 -Sum).Sum / 1MB) }
} | Sort-Object MB -Descending | Select-Object -First 8 | ForEach-Object {
    Out-Line ("    {0,-26} {1,7:N0} MB" -f $_.Name, $_.MB)
}

# ---------------------------------------------------------------- disk
Head "DISK ($Drive`:)"
$vol = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='$Drive`:'"
$diskFreeGb = Gb $vol.FreeSpace
Row 'Size'              "$(Gb $vol.Size) GB"
Row 'Free'              "$diskFreeGb GB  ($([math]::Round(100 * $vol.FreeSpace / $vol.Size))% free)"
foreach ($p in 'C:\SQLData', 'C:\SQLLogs', 'C:\inetpub\hcm-api\App_Data\Logs', 'C:\Ollama') {
    if (Test-Path $p) {
        $sz = (Get-ChildItem $p -Recurse -File -EA SilentlyContinue | Measure-Object Length -Sum).Sum
        Row "  $p" "$(Gb $sz) GB"
    }
}

# ---------------------------------------------------------------- GPU
Head 'GPU'
$gpus = @(Get-CimInstance Win32_VideoController)
foreach ($g in $gpus) {
    Row 'Adapter'       "$($g.Name)  (driver $($g.DriverVersion))"
}
$nvsmi = Get-Command nvidia-smi.exe -EA SilentlyContinue
if (-not $nvsmi -and (Test-Path 'C:\Windows\System32\nvidia-smi.exe')) { $nvsmi = 'C:\Windows\System32\nvidia-smi.exe' }
$hasGpu = $false
if ($nvsmi) {
    $q = & $nvsmi --query-gpu=name,memory.total,memory.used,driver_version --format=csv,noheader 2>$null
    if ($LASTEXITCODE -eq 0 -and $q) {
        $hasGpu = $true
        foreach ($line in $q) { Row 'NVIDIA GPU' $line }
    }
}
if (-not $hasGpu) {
    Row 'Usable NVIDIA GPU' 'none found - the model will run on the CPU'
}

# ---------------------------------------------------------------- what else is running
Head 'SERVICES ALREADY ON THIS SERVER'
$sql = Get-Service -Name 'MSSQL*' -EA SilentlyContinue
foreach ($s in $sql) { Row $s.Name $s.Status }
if ($isAdmin) {
    try {
        Import-Module WebAdministration -EA Stop
        foreach ($site in Get-Website) { Row "IIS site $($site.Name)" $site.State }
        $pool = Get-Item IIS:\AppPools\hcm-api-pool -EA SilentlyContinue
        if ($pool) { Row 'App pool hcm-api-pool' $pool.State }
    } catch { Row 'IIS' 'could not read (WebAdministration module missing)' }
}
$sqlcmd = Get-Command sqlcmd.exe -EA SilentlyContinue
if ($sqlcmd) {
    $mem = & sqlcmd.exe -S '.\SQLEXPRESS' -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT CAST(value_in_use AS int) FROM sys.configurations WHERE name='max server memory (MB)'" 2>$null
    if ($LASTEXITCODE -eq 0 -and $mem) { Row 'SQL max server memory' "$(($mem | Select-Object -First 1).Trim()) MB (Express uses at most ~1.4 GB anyway)" }
}

# ---------------------------------------------------------------- Ollama
Head 'OLLAMA'
$ollamaExe = @('C:\Ollama\ollama.exe', "$env:LOCALAPPDATA\Programs\Ollama\ollama.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
Row 'Installed'         $(if ($ollamaExe) { $ollamaExe } else { 'no' })
$listen = Get-NetTCPConnection -LocalPort 11434 -State Listen -EA SilentlyContinue
if ($listen) {
    $addrs = ($listen.LocalAddress | Sort-Object -Unique) -join ', '
    Row 'Listening on 11434' $addrs
    if ($listen.LocalAddress | Where-Object { $_ -ne '127.0.0.1' }) { Row 'WARNING' 'Ollama is reachable from outside this server - it must be 127.0.0.1 only' }
} else { Row 'Listening on 11434' 'no' }
$fw = Get-NetFirewallRule -EA SilentlyContinue | Where-Object { $_.DisplayName -match 'Ollama' }
Row 'Ollama firewall rules' $(if ($fw) { ($fw.DisplayName -join '; ') } else { 'none' })

# ---------------------------------------------------------------- internet (model download)
Head 'MODEL DOWNLOAD REACHABILITY'
foreach ($h in 'ollama.com', 'registry.ollama.ai') {
    $ok = $false
    try { $ok = (Test-NetConnection -ComputerName $h -Port 443 -WarningAction SilentlyContinue -InformationLevel Quiet) } catch { }
    Row $h $(if ($ok) { 'reachable' } else { 'NOT reachable - model must be copied in manually' })
}

# ---------------------------------------------------------------- verdict
Head 'WHICH MODEL FITS'
Out-Line '  RAM needed = while answering, with an 8k context, on top of SQL Server and IIS.'
Out-Line '  Figures are for the default 4-bit Ollama builds.'
Out-Line ''
Out-Line ("  {0,-12} {1,10} {2,10} {3,10}  {4}" -f 'Model', 'Download', 'RAM need', 'Disk need', 'Verdict')
Out-Line ("  {0,-12} {1,10} {2,10} {3,10}  {4}" -f '-----', '--------', '--------', '---------', '-------')

$models = @(
    @{ n = 'qwen3:4b';  dl = 2.6;  ram = 3.5;  total = 8;  disk = 6  },
    @{ n = 'qwen3:8b';  dl = 5.2;  ram = 6.5;  total = 12; disk = 9  },
    @{ n = 'qwen3:14b'; dl = 9.3;  ram = 11;   total = 16; disk = 13 },
    @{ n = 'qwen3:32b'; dl = 20;   ram = 23;   total = 32; disk = 25 }
)
foreach ($m in $models) {
    $reasons = @()
    if ($totalGb -lt $m.total)   { $reasons += "needs $($m.total) GB total RAM" }
    if ($availGb -lt $m.ram)     { $reasons += "needs $($m.ram) GB free RAM now" }
    if ($diskFreeGb -lt $m.disk) { $reasons += "needs $($m.disk) GB free disk" }
    $verdict = if ($reasons) { 'NO - ' + ($reasons -join ', ') } else { 'fits' }
    Out-Line ("  {0,-12} {1,8} GB {2,8} GB {3,8} GB  {4}" -f $m.n, $m.dl, $m.ram, $m.disk, $verdict)
}

Out-Line ''
if (-not $hasGpu) {
    Out-Line '  Speed: no GPU, so answers come from the CPU. On the dev laptop qwen3:4b took'
    Out-Line '  roughly 1 minute for leave questions and 3-6 minutes for pay changes.'
    Out-Line "  This server has $threads CPU threads; more threads help, a GPU helps far more."
}

# ---------------------------------------------------------------- save
$out = Join-Path ([Environment]::GetFolderPath('Desktop')) ("AI-server-check-{0}.txt" -f (Get-Date -Format 'yyyyMMdd-HHmm'))
try {
    $report | Set-Content -Path $out -Encoding UTF8
    Write-Host ''
    Write-Host "Report saved to: $out" -ForegroundColor Green
} catch {
    Write-Host "Could not save the report file: $($_.Exception.Message)" -ForegroundColor Yellow
}
