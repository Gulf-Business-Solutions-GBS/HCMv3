<#
    Deploy-OnServer.ps1

    Run this ON THE SERVER, from an elevated PowerShell prompt, from inside
    the extracted package folder (the one containing api\, web\ and sql\).

        Right-click PowerShell -> Run as Administrator
        cd C:\HCMv3-Deploy
        .\Deploy-OnServer.ps1

    It checks prerequisites, creates the database, copies the files, creates
    both IIS sites and opens the firewall. It is safe to re-run: existing
    sites and pools are reused rather than duplicated.

    Add -SkipDatabase if the SQL scripts have already been run.
#>

[CmdletBinding()]
param(
    [string] $PackageRoot = $PSScriptRoot,
    [string] $SqlInstance = '.\SQLEXPRESS',
    [switch] $SkipDatabase
)

$ErrorActionPreference = 'Stop'

function Step { param($n, $t) Write-Host "`n[$n] $t" -ForegroundColor Cyan }
function Ok   { param($m) Write-Host "    OK   $m" -ForegroundColor Green }
function Warn { param($m) Write-Host "    WARN $m" -ForegroundColor Yellow }
function Die  { param($m) Write-Host "    FAIL $m" -ForegroundColor Red; exit 1 }

# --- must be elevated ------------------------------------------------------
$isAdmin = ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()
    ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { Die 'This script must be run as Administrator.' }

Step 1 'Checking prerequisites'

if (-not (Get-WindowsFeature Web-Server).Installed) { Die 'IIS is not installed.' }
Ok 'IIS present'

if (-not (Get-WindowsFeature Web-WebSockets).Installed) {
    Warn 'WebSocket Protocol is NOT enabled. Live notifications will not work.'
    Warn 'Enable with: Install-WindowsFeature Web-WebSockets'
} else { Ok 'WebSocket Protocol enabled' }

$ancm = "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (-not (Test-Path $ancm)) { Die '.NET Hosting Bundle not installed, or installed before IIS. Install it (or run its installer and choose Repair).' }
Ok 'ASP.NET Core Module v2 present'

if (-not (Test-Path "$env:SystemRoot\System32\inetsrv\rewrite.dll")) {
    Die 'IIS URL Rewrite module not installed. Angular deep links will 404 without it.'
}
Ok 'URL Rewrite present'

foreach ($p in @('api','web','sql')) {
    if (-not (Test-Path (Join-Path $PackageRoot $p))) { Die "Package folder '$p' not found under $PackageRoot" }
}
Ok "Package found at $PackageRoot"

$free = [math]::Round((Get-PSDrive C).Free / 1GB, 1)
if ($free -lt 25) { Warn "Only $free GB free on C:. The database needs about 25 GB headroom." }
else { Ok "$free GB free on C:" }

# AI Assistant: the model on this server must hold at least as many tokens as the API is told to send
# (AiAgent:ContextTokens). If it holds fewer, Ollama silently drops the START of each request -- where the
# assistant's safety rules are. So refuse to deploy settings the installed model cannot honour.
$appSettings = Join-Path $PackageRoot 'api\appsettings.Production.json'
$wantCtx = 0
try { $wantCtx = [int]((Get-Content $appSettings -Raw | ConvertFrom-Json).AiAgent.ContextTokens) } catch { }
$modelfiles = @(Get-ChildItem 'C:\Ollama\Modelfile.*' -EA SilentlyContinue)
if ($wantCtx -gt 0 -and $modelfiles.Count -gt 0) {
    foreach ($mf in $modelfiles) {
        $m = Select-String -Path $mf.FullName -Pattern 'num_ctx\s+(\d+)' | Select-Object -First 1
        $haveCtx = if ($m) { [int]$m.Matches[0].Groups[1].Value } else { 0 }
        if ($haveCtx -lt $wantCtx) {
            Die ("The AI model on this server holds $haveCtx tokens, but this release needs $wantCtx. " +
                 "Run ai\install-ollama.ps1 from this package FIRST (it upgrades the model in place), then re-run this script.")
        }
        Ok "AI model context $haveCtx tokens - enough for this release ($wantCtx)"
    }
} elseif ($wantCtx -gt 0) {
    Warn 'Ollama is not installed yet. The AI Assistant will not answer until ai\install-ollama.ps1 has been run.'
}

# --- database --------------------------------------------------------------
if ($SkipDatabase) {
    Step 2 'Database - skipped (-SkipDatabase)'
} else {
    Step 2 'Creating database and applying migrations'
    $sqlcmd = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
    if (-not $sqlcmd) {
        Warn 'sqlcmd.exe not found. Run the two scripts in sql\ manually via SSMS, then re-run with -SkipDatabase.'
    } else {
        & sqlcmd.exe -S $SqlInstance -E -C -I -b -i (Join-Path $PackageRoot 'sql\01-create-database.sql')
        if ($LASTEXITCODE -ne 0) { Die 'Script 01 failed. Check the message above.' }
        Ok 'Database and login created'

        Write-Host '    Applying 141 migrations, this takes several minutes...'
        & sqlcmd.exe -S $SqlInstance -E -C -I -b -d HCMv3Db -i (Join-Path $PackageRoot 'sql\02-schema-all-migrations.sql')
        if ($LASTEXITCODE -ne 0) { Die 'Script 02 failed. Check the message above.' }

        $count = & sqlcmd.exe -S $SqlInstance -E -C -I -d HCMv3Db -h -1 -W `
                   -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM __EFMigrationsHistory"
        Ok "Migrations applied: $($count.Trim())  (expected 141)"
    }
}

# --- files -----------------------------------------------------------------
Step 3 'Copying application files'
$apiDst = 'C:\inetpub\hcm-api'
$webDst = 'C:\inetpub\hcm-web'

# A running site holds its DLLs open. Without stopping first, robocopy sits on
# the locked files retrying (its default is a million attempts, 30s apart) and
# looks exactly like a hang. Harmless on a first install, fatal on a redeploy.
Import-Module WebAdministration
foreach ($s in @('hcm-api','hcm-web')) {
    if (Get-Website -Name $s -ErrorAction SilentlyContinue) {
        Stop-Website -Name $s -ErrorAction SilentlyContinue
    }
}
if (Test-Path IIS:\AppPools\hcm-api-pool) {
    Stop-WebAppPool -Name hcm-api-pool -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 5   # let w3wp actually exit and release its handles
Ok 'Sites stopped so files can be replaced'

foreach ($pair in @(@{s='api';d=$apiDst}, @{s='web';d=$webDst})) {
    $src = Join-Path $PackageRoot $pair.s
    # /R:2 /W:2 - fail fast on a genuinely locked file rather than retrying forever
    robocopy $src $pair.d /E /R:2 /W:2 /NFL /NDL /NJH /NJS /NP /MT:16 | Out-Null
    if ($LASTEXITCODE -ge 8) {
        Die "Copy of $($pair.s) failed (robocopy $LASTEXITCODE). Something still has these files open - check for an open Explorer window or editor, then re-run."
    }
    Ok "$($pair.s) -> $($pair.d)"
}

# --- IIS -------------------------------------------------------------------
Step 4 'Configuring IIS'
Import-Module WebAdministration

# Default Web Site also binds port 80, so hcm-web cannot start while it runs.
# Stopping is not enough: it auto-starts again after iisreset or a reboot, and
# then hcm-web silently refuses to start. Turn its auto-start off as well.
if (Get-Website -Name 'Default Web Site' -ErrorAction SilentlyContinue) {
    Stop-Website -Name 'Default Web Site' -ErrorAction SilentlyContinue
    Set-ItemProperty 'IIS:\Sites\Default Web Site' -Name serverAutoStart -Value $false -ErrorAction SilentlyContinue
    Ok 'Default Web Site stopped and auto-start disabled (frees port 80 permanently)'
}

if (-not (Test-Path IIS:\AppPools\hcm-api-pool)) { New-WebAppPool -Name hcm-api-pool | Out-Null }
Set-ItemProperty IIS:\AppPools\hcm-api-pool -Name managedRuntimeVersion -Value ''
Set-ItemProperty IIS:\AppPools\hcm-api-pool -Name startMode -Value 'AlwaysRunning'
Ok 'App pool hcm-api-pool ready (No Managed Code)'

if (-not (Get-Website -Name hcm-api -ErrorAction SilentlyContinue)) {
    New-Website -Name hcm-api -Port 9901 -PhysicalPath $apiDst -ApplicationPool hcm-api-pool | Out-Null
}
Ok 'Site hcm-api on port 9901'

if (-not (Get-Website -Name hcm-web -ErrorAction SilentlyContinue)) {
    New-Website -Name hcm-web -Port 80 -PhysicalPath $webDst | Out-Null
}
Ok 'Site hcm-web on port 80'

# App pool identity needs read+execute across the application folder...
icacls $apiDst /grant "IIS AppPool\hcm-api-pool:(OI)(CI)RX" /T /Q | Out-Null
Ok 'Read access granted on api folder'

# ...and WRITE access on App_Data, or log4net cannot create Logs.txt and the
# application fails with no diagnostics at all.
$appDataLogs = Join-Path $apiDst 'App_Data\Logs'
New-Item -ItemType Directory -Path $appDataLogs -Force | Out-Null
icacls (Join-Path $apiDst 'App_Data') /grant "IIS AppPool\hcm-api-pool:(OI)(CI)M" /T /Q | Out-Null
Ok 'Write access granted on App_Data (application logs)'

# Restart the pool explicitly - Start-Website does not start a stopped pool,
# and step 3 stopped it in order to replace the locked DLLs.
if ((Get-WebAppPoolState -Name hcm-api-pool).Value -ne 'Started') {
    Start-WebAppPool -Name hcm-api-pool -ErrorAction SilentlyContinue
}
Start-Website -Name hcm-api -ErrorAction SilentlyContinue
Start-Website -Name hcm-web -ErrorAction SilentlyContinue
Ok 'Sites and app pool started'

# --- firewall --------------------------------------------------------------
Step 5 'Firewall'
foreach ($r in @(@{n='HCM Web 80';p=80}, @{n='HCM API 9901';p=9901})) {
    if (-not (Get-NetFirewallRule -DisplayName $r.n -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $r.n -Direction Inbound -Protocol TCP `
            -LocalPort $r.p -Action Allow | Out-Null
    }
    Ok "Port $($r.p) open"
}

# --- smoke test ------------------------------------------------------------
Step 6 'Smoke test'
Start-Sleep -Seconds 10   # first hit builds the EF model across 491 entities
try {
    $r = Invoke-WebRequest 'http://localhost:9901/swagger/index.html' -UseBasicParsing -TimeoutSec 120
    Ok "API responded HTTP $($r.StatusCode)"
} catch { Warn "API did not respond yet: $($_.Exception.Message)"; Warn 'Check C:\inetpub\hcm-api\App_Data\Logs\' }

try {
    $r = Invoke-WebRequest 'http://localhost/' -UseBasicParsing -TimeoutSec 60
    Ok "Web responded HTTP $($r.StatusCode)"
} catch { Warn "Web did not respond: $($_.Exception.Message)" }

Write-Host @"

=====================================================================
 Deployment complete.

   Application : http://vps2656276.fastwebserver.de
   API/Swagger : http://vps2656276.fastwebserver.de:9901/swagger

 Still to do, in this order:
   1. Log in as admin / 123qwe and CHANGE THAT PASSWORD immediately.
   2. Run win-acme for a certificate, then apply the HTTPS values in
      section 6 of DEPLOY-README.md. Until then logins cross the
      internet in clear text.
=====================================================================
"@ -ForegroundColor Cyan
