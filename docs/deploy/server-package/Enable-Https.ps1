<#
    Enable-Https.ps1

    Run ON THE SERVER, elevated. Puts both sites on HTTPS and repoints the
    two config files, because the Angular app cannot start over plain HTTP:
    MSAL needs window.crypto.subtle, which browsers only expose on a secure
    origin.

    By default it creates a SELF-SIGNED certificate, which produces a browser
    warning you click through once. That is fine for testing that the app
    works, but not for showing anyone.

    For a real certificate, install win-acme first and run:
        .\Enable-Https.ps1 -UseLetsEncrypt
#>

[CmdletBinding()]
param(
    [string] $HostName = 'vps2656276.fastwebserver.de',
    [int]    $WebPort  = 443,
    [int]    $ApiPort  = 9443,
    [switch] $UseLetsEncrypt,

    # Let's Encrypt sends certificate-expiry warnings here. Prefer a shared
    # team mailbox over a personal address: renewal runs unattended every 90
    # days, and this notice is the only warning if it ever stops working.
    [string] $ContactEmail = ''
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

function Ok   { param($m) Write-Host "    OK   $m" -ForegroundColor Green }
function Info { param($m) Write-Host "    ..   $m" -ForegroundColor Gray }
function Die  { param($m) Write-Host "    FAIL $m" -ForegroundColor Red; exit 1 }

$isAdmin = ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()
    ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { Die 'Must be run as Administrator.' }

# --- 1. certificate --------------------------------------------------------
Write-Host "`n[1] Certificate" -ForegroundColor Cyan

if ($UseLetsEncrypt) {
    $wacs = Get-ChildItem -Path C:\ -Filter wacs.exe -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1
    if (-not $wacs) { Die 'wacs.exe not found. Download win-acme from win-acme.com and unzip it on this server.' }
    Info "Running win-acme from $($wacs.FullName)"
    # --installation none: this script binds the certificate itself with netsh,
    #   so win-acme only has to place it in the Windows store. Using
    #   "--installation iis" is what demanded --installationsiteid.
    # --validation filesystem: drops the challenge file into the Angular site's
    #   webroot, which IIS already serves on port 80. The selfhosting validator
    #   wants that port for itself and collides with IIS.
    # ACME challenge tokens are extensionless files. IIS refuses to serve those
    # (404) unless a MIME type is mapped for them, and the Angular site's
    # rewrite rules must not touch the .well-known path either. A web.config
    # scoped to the challenge folder fixes both without affecting the app.
    $challengeDir = 'C:\inetpub\hcm-web\.well-known\acme-challenge'
    New-Item -ItemType Directory -Path $challengeDir -Force | Out-Null
    @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <staticContent>
      <remove fileExtension="." />
      <mimeMap fileExtension="." mimeType="text/plain" />
    </staticContent>
    <rewrite>
      <rules>
        <clear />
      </rules>
    </rewrite>
    <handlers>
      <clear />
      <add name="AcmeStatic" path="*" verb="GET" modules="StaticFileModule"
           resourceType="Either" requireAccess="Read" />
    </handlers>
  </system.webServer>
</configuration>
'@ | Set-Content -Path (Join-Path $challengeDir 'web.config') -Encoding UTF8
    Ok 'ACME challenge folder prepared (extensionless MIME map, rewrite bypassed)'

    # Prove IIS will actually serve an extensionless file before asking Let's
    # Encrypt to fetch one - otherwise the failure surfaces as a confusing 403.
    $probe = Join-Path $challengeDir 'selftest'
    'ok' | Set-Content -Path $probe -Encoding ASCII -NoNewline
    try {
        $res = Invoke-WebRequest "http://localhost/.well-known/acme-challenge/selftest" -UseBasicParsing -TimeoutSec 15
        if ($res.Content.Trim() -ne 'ok') { Die "Challenge self-test returned unexpected content: $($res.Content)" }
        Ok 'Challenge self-test passed (IIS serves extensionless files)'
    } catch {
        Die "Challenge self-test failed: $($_.Exception.Message). IIS will not serve the ACME token, so Let's Encrypt cannot validate. Use the self-signed route instead."
    } finally {
        Remove-Item $probe -Force -ErrorAction SilentlyContinue
    }

    # Without --emailaddress, win-acme stops and prompts interactively the first
    # time it registers an ACME account, which defeats running this unattended.
    $wacsArgs = @(
        '--target','manual','--host',$HostName,
        '--validationmode','http-01','--validation','filesystem','--webroot','C:\inetpub\hcm-web',
        '--store','certificatestore','--certificatestore','My',
        '--installation','none','--accepttos'
    )
    if ($ContactEmail) { $wacsArgs += @('--emailaddress', $ContactEmail) }
    else { Info 'No -ContactEmail given; win-acme will prompt for one if this is a new account.' }

    & $wacs.FullName @wacsArgs
    if ($LASTEXITCODE -ne 0) { Die 'win-acme failed. Fall back to the self-signed route (run without -UseLetsEncrypt).' }

    # Skip self-signed certificates (Issuer equals Subject). Without this filter
    # the self-signed cert from an earlier run wins the sort, since it is valid
    # for two years while a Let's Encrypt certificate lasts ninety days.
    $cert = Get-ChildItem Cert:\LocalMachine\WebHosting, Cert:\LocalMachine\My |
            Where-Object { $_.Subject -like "*$HostName*" -and $_.Issuer -ne $_.Subject } |
            Sort-Object NotAfter -Descending | Select-Object -First 1
    if (-not $cert) { Die 'win-acme reported success but no trusted certificate was found in the store.' }
    Ok "Trusted certificate installed, expires $($cert.NotAfter.ToString('yyyy-MM-dd'))"
}
else {
    $cert = Get-ChildItem Cert:\LocalMachine\My |
            Where-Object { $_.Subject -eq "CN=$HostName" -and $_.NotAfter -gt (Get-Date) } |
            Sort-Object NotAfter -Descending | Select-Object -First 1
    if (-not $cert) {
        $cert = New-SelfSignedCertificate -DnsName $HostName `
                    -CertStoreLocation 'Cert:\LocalMachine\My' `
                    -FriendlyName 'HCM demo certificate' `
                    -NotAfter (Get-Date).AddYears(2)
        Ok 'Self-signed certificate created'
    } else {
        Ok 'Reusing existing self-signed certificate'
    }
    Write-Host '    NOTE Browsers will show a warning for this certificate.' -ForegroundColor Yellow
}

$thumb = $cert.Thumbprint

# --- 2. bindings -----------------------------------------------------------
Write-Host "`n[2] HTTPS bindings" -ForegroundColor Cyan

foreach ($b in @(@{site='hcm-web'; port=$WebPort}, @{site='hcm-api'; port=$ApiPort})) {
    if (-not (Get-Website -Name $b.site -ErrorAction SilentlyContinue)) { Die "Site $($b.site) does not exist. Run Deploy-OnServer.ps1 first." }

    $existing = Get-WebBinding -Name $b.site -Protocol https -Port $b.port -ErrorAction SilentlyContinue
    if (-not $existing) {
        New-WebBinding -Name $b.site -Protocol https -Port $b.port -IPAddress '*' | Out-Null
    }

    # bind the certificate to the port
    $null = netsh http delete sslcert ipport=0.0.0.0:$($b.port) 2>$null
    $appid = '{' + [guid]::NewGuid().ToString() + '}'
    $null = netsh http add sslcert ipport=0.0.0.0:$($b.port) certhash=$thumb appid=$appid certstorename=MY
    if ($LASTEXITCODE -ne 0) { Die "Failed to bind certificate to port $($b.port)" }
    Ok "$($b.site) listening on https port $($b.port)"
}

# --- 3. firewall -----------------------------------------------------------
Write-Host "`n[3] Firewall" -ForegroundColor Cyan
foreach ($p in @($WebPort, $ApiPort)) {
    $name = "HCM HTTPS $p"
    if (-not (Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $name -Direction Inbound -Protocol TCP -LocalPort $p -Action Allow | Out-Null
    }
    Ok "Port $p open"
}

# --- 4. configuration ------------------------------------------------------
Write-Host "`n[4] Repointing configuration to https" -ForegroundColor Cyan

$apiCfg = 'C:\inetpub\hcm-api\appsettings.Production.json'
$webCfg = 'C:\inetpub\hcm-web\assets\appconfig.production.json'
foreach ($f in @($apiCfg, $webCfg)) { if (-not (Test-Path $f)) { Die "Not found: $f" } }

$t = [IO.File]::ReadAllText($apiCfg)
$t = $t -replace '"ServerRootAddress":\s*"[^"]*"', """ServerRootAddress"": ""https://$HostName`:$ApiPort/"""
$t = $t -replace '"ClientRootAddress":\s*"[^"]*"', """ClientRootAddress"": ""https://$HostName/"""
$t = $t -replace '"CorsOrigins":\s*"[^"]*"',       """CorsOrigins"": ""https://$HostName"""
[IO.File]::WriteAllText($apiCfg, $t, (New-Object Text.UTF8Encoding $false))
Ok 'appsettings.Production.json updated'

$t = [IO.File]::ReadAllText($webCfg)
$t = $t -replace '"remoteServiceBaseUrl":\s*"[^"]*"', """remoteServiceBaseUrl"": ""https://$HostName`:$ApiPort"""
$t = $t -replace '"appBaseUrl":\s*"[^"]*"',           """appBaseUrl"": ""https://$HostName"""
[IO.File]::WriteAllText($webCfg, $t, (New-Object Text.UTF8Encoding $false))
Ok 'appconfig.production.json updated'

# --- 5. restart ------------------------------------------------------------
Write-Host "`n[5] Restarting" -ForegroundColor Cyan
# Restart-WebAppPool fails outright if the pool is already stopped ("You have
# to start stopped object before restarting it"), which happens whenever a
# deployment stopped it to replace locked files. Check state and act.
if (Test-Path IIS:\AppPools\hcm-api-pool) {
    if ((Get-WebAppPoolState -Name hcm-api-pool).Value -eq 'Started') {
        Restart-WebAppPool -Name hcm-api-pool -ErrorAction SilentlyContinue
    } else {
        Start-WebAppPool -Name hcm-api-pool -ErrorAction SilentlyContinue
    }
    Ok 'App pool running'
}
foreach ($s in @('hcm-api','hcm-web')) {
    Stop-Website  -Name $s -ErrorAction SilentlyContinue
    Start-Website -Name $s -ErrorAction SilentlyContinue
    Ok "$s restarted"
}

Write-Host @"

=====================================================================
 HTTPS enabled.

   Application : https://$HostName
   API/Swagger : https://$HostName`:$ApiPort/swagger

 If you used the self-signed certificate you will get a browser
 warning. Click Advanced, then Proceed. The app will then load,
 because the origin is https and window.crypto.subtle exists.

 Before showing anyone: re-run with -UseLetsEncrypt for a trusted
 certificate, and change the admin password.
=====================================================================
"@ -ForegroundColor Cyan
