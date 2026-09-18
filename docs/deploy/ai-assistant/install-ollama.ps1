<#
.SYNOPSIS
    Installs Ollama and the Qwen model on the HCMv3 server for the AI assistant, as a background task that
    starts with Windows, reachable from this machine only, and cut off from the internet.

.DESCRIPTION
    Run ONCE per server, in an ADMINISTRATOR PowerShell, from the folder holding this script and
    OllamaSetup.exe. Safe to re-run: every step checks before it changes anything.

      1. Checks the server has the memory and disk for the model.
      2. Installs Ollama silently to C:\Ollama (not per-user), and removes the tray app's autostart --
         on a server nobody is signed in, so the tray app would never start and would only compete for
         the port if someone did sign in.
      3. Creates a start script with the settings below and a scheduled task that runs it at startup as
         SYSTEM, whether or not anyone is signed in.
      4. Downloads the model (the ONLY time Ollama needs the internet).
      5. Blocks Ollama's outbound internet access with Windows Firewall.
      6. Checks it answers, on 127.0.0.1 only.

    Written 18 Sep 2026 against Ollama 0.34.1 on the dev laptop. NOT yet run on a server -- read the output
    of each step.

.PARAMETER Model
    Default qwen3:4b, the model the assistant was tested with. On a server with a GPU or plenty of RAM,
    qwen3:8b is noticeably better at tool calls; set AiAgent:Model in appsettings to match.

.PARAMETER Force
    Continue even if the memory check fails.
#>
[CmdletBinding()]
param(
    [string] $Model = "qwen3:4b",
    [string] $InstallDir = "C:\Ollama",
    [switch] $Force
)

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

function Step($text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }

# ------------------------------------------------------------------ 0. administrator?
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw "Run this in an ADMINISTRATOR PowerShell." }

# ------------------------------------------------------------------ 1. capacity
Step "1. Checking memory and disk"
$os = Get-CimInstance Win32_OperatingSystem
$totalGb = [math]::Round($os.TotalVisibleMemorySize / 1MB, 1)
$freeGb = [math]::Round($os.FreePhysicalMemory / 1MB, 1)
$cores = (Get-CimInstance Win32_Processor | Measure-Object NumberOfLogicalProcessors -Sum).Sum
$diskGb = [math]::Round((Get-PSDrive ($InstallDir.Substring(0, 1))).Free / 1GB, 1)
Write-Host "RAM total $totalGb GB, free now $freeGb GB, CPU threads $cores, free disk $diskGb GB"

# qwen3:4b needs about 3.5 GB while answering, on top of SQL Server and IIS.
if (($totalGb -lt 8 -or $freeGb -lt 4) -and -not $Force) {
    throw "Not enough memory for the model alongside SQL Server and the app (need 8 GB total and 4 GB free). " +
          "Stop here and decide on a bigger server, or re-run with -Force to try anyway."
}
if ($diskGb -lt 6) { throw "Need at least 6 GB free on $($InstallDir.Substring(0,2)) for Ollama and the model." }

# ------------------------------------------------------------------ 2. install
Step "2. Installing Ollama to $InstallDir"
$exe = Join-Path $InstallDir "ollama.exe"
if (Test-Path $exe) {
    Write-Host "Already installed: $(& $exe --version)"
} else {
    $setup = Join-Path $here "OllamaSetup.exe"
    if (-not (Test-Path $setup)) { throw "OllamaSetup.exe not found next to this script." }
    # Not "Start-Process -Wait": that waits for the installer AND every process it starts, and the installer
    # finishes by starting Ollama, which never exits -- so -Wait hangs forever. WaitForExit waits for the
    # installer alone; the Ollama it started is stopped just below.
    $installer = Start-Process -FilePath $setup -ArgumentList "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/DIR=`"$InstallDir`"" -PassThru
    $installer.WaitForExit()
    if (-not (Test-Path $exe)) { throw "Install finished but $exe is missing. Check where it installed." }
    Write-Host "Installed: $(& $exe --version)"
}

# The installer starts the tray app and adds it to the installing user's Startup folder. Stop both: the
# scheduled task below is the one copy that should run.
Get-Process "ollama app", "ollama" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-ChildItem "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup", "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp" -Filter "Ollama*.lnk" -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-Item $_.FullName -Force; Write-Host "Removed autostart: $($_.FullName)" }

# ------------------------------------------------------------------ 3. settings + startup task
Step "3. Settings and startup task"
$models = Join-Path $InstallDir "models"
New-Item -ItemType Directory -Force -Path $models | Out-Null

# Set inside the start script rather than as machine environment variables, so they apply whatever account
# or session starts Ollama, and are visible in one file.
#   OLLAMA_HOST            127.0.0.1 only: no other computer can reach the model.
#   OLLAMA_MODELS          a fixed folder, not the SYSTEM account's profile.
#   OLLAMA_CONTEXT_LENGTH  8192. The default (4096) is smaller than a long administrator conversation with
#                          all its tool definitions, and Ollama then drops the START of the request -- the
#                          system prompt with the assistant's rules -- without any error.
#   OLLAMA_NUM_PARALLEL    1: one answer at a time; others queue. Keeps memory predictable on a small server.
#   OLLAMA_KEEP_ALIVE      24h: the model stays loaded, so users do not wait for it to load each time.
#   OLLAMA_NOPRUNE         keep downloaded model files even while the firewall blocks updates.
$startScript = Join-Path $InstallDir "start-ollama.cmd"
@"
@echo off
set OLLAMA_HOST=127.0.0.1:11434
set OLLAMA_MODELS=$models
set OLLAMA_CONTEXT_LENGTH=8192
set OLLAMA_NUM_PARALLEL=1
set OLLAMA_MAX_LOADED_MODELS=1
set OLLAMA_KEEP_ALIVE=24h
set OLLAMA_NOPRUNE=1
"$exe" serve >> "$InstallDir\ollama-serve.log" 2>&1
"@ | Set-Content -Path $startScript -Encoding ASCII
Write-Host "Wrote $startScript"

$taskName = "HCMv3 AI - Ollama"
$action = New-ScheduledTaskAction -Execute "cmd.exe" -Argument "/c `"$startScript`""
$trigger = New-ScheduledTaskTrigger -AtStartup
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -StartWhenAvailable
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
Start-ScheduledTask -TaskName $taskName
Write-Host "Scheduled task '$taskName' registered and started."

$deadline = (Get-Date).AddSeconds(60)
while (-not (Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) { Start-Sleep 2 }
$listen = Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction SilentlyContinue
if (-not $listen) { throw "Ollama did not start. See $InstallDir\ollama-serve.log" }
if ($listen.LocalAddress | Where-Object { $_ -ne "127.0.0.1" }) { throw "Ollama is listening on $($listen.LocalAddress) -- it must be 127.0.0.1 only." }
Write-Host "Listening on 127.0.0.1:11434 only."

# ------------------------------------------------------------------ 4. model
Step "4. Downloading $Model (the only internet access Ollama needs)"
$env:OLLAMA_HOST = "127.0.0.1:11434"
& $exe pull $Model
if ($LASTEXITCODE -ne 0) { throw "Model download failed." }

# A named variant with the context window BUILT IN, so the size the API is configured for (AiAgent:ContextTokens
# = 8192) is guaranteed by the model itself, not by an environment variable someone could lose. The API trims
# every request to that size; a model that silently held less would drop the rules at the start.
$variant = "hcmv3-" + ($Model -replace '[:/]', '-')
$modelfile = Join-Path $InstallDir "Modelfile.$variant"
"FROM $Model`nPARAMETER num_ctx 8192" | Set-Content -Path $modelfile -Encoding ASCII
& $exe create $variant -f $modelfile
if ($LASTEXITCODE -ne 0) { throw "Could not create the model variant $variant." }
Write-Host "Created $variant (8192-token context). Set AiAgent:Model to '$variant' and AiAgent:ContextTokens to 8192."
& $exe list

# ------------------------------------------------------------------ 5. firewall
Step "5. Blocking Ollama from the internet"
foreach ($program in @($exe, (Join-Path $InstallDir "ollama app.exe"))) {
    $name = "HCMv3 AI - block internet - " + (Split-Path $program -Leaf)
    if (-not (Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $name -Direction Outbound -Action Block -Program $program -Profile Any | Out-Null
        Write-Host "Created: $name"
    } else {
        Write-Host "Already present: $name"
    }
}
# Loopback traffic (the app calling 127.0.0.1:11434) is not subject to these rules.

# ------------------------------------------------------------------ 6. check
Step "6. Checking it answers"
& (Join-Path $here "test-ollama.ps1") -Model $variant

Write-Host "`nDone. To download another model later: Disable-NetFirewallRule -DisplayName 'HCMv3 AI - block internet*', pull, then Enable-NetFirewallRule -DisplayName 'HCMv3 AI - block internet*'." -ForegroundColor Green
