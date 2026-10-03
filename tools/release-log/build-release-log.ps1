# Writes release-log.json: the build's version number and the log of every change, read from git.
#
# Run by HCMv3.Core's build (see HCMv3.Core.csproj). Nobody types a version: the build number is the count of
# commits in the build, so any deploy with new code gets a higher number, and "what changed since build P" is
# simply every change numbered above P.
#
# For each change it also works out what a tenant may have to set up, from what the change touched:
#   - a new permission      (AppPermissions.cs)
#   - a new screen          (ScreenCatalog.cs)
#   - a new setting         (AppSettings.cs)
#   - a database change     (a new EF migration)
#   - anything else         (a "Setup: ..." line in the commit message)
#
# Numbering. Walk the first-parent line (the branch as it was built) oldest to newest; each commit's number is how
# many commits the build holds once it is in. Commits a merge brings in take the merge's number: that is the build
# they arrived in.

param(
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [Parameter(Mandatory = $true)][string]$OutFile,
    [string]$BaseVersion = '3.0'
)

$ErrorActionPreference = 'Stop'
$sep = [char]0x1f   # unit separator: cannot appear in a commit subject
$rec = [char]0x1e   # record separator

function Invoke-Git {
    param([string[]]$GitArgs)
    $prev = [Console]::OutputEncoding
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    try {
        $out = & git -C $RepoRoot -c core.quotepath=false @GitArgs 2>$null
        if ($LASTEXITCODE -ne 0) { throw "git $($GitArgs -join ' ') failed ($LASTEXITCODE)" }
        return $out
    }
    finally { [Console]::OutputEncoding = $prev }
}

$head = (Invoke-Git @('rev-parse', 'HEAD')) | Select-Object -First 1
$dirty = [bool](Invoke-Git @('status', '--porcelain', '--untracked-files=no'))

# Nothing new since the last run: keep the file, so an ordinary rebuild costs one git call.
if (Test-Path $OutFile) {
    try {
        $existing = Get-Content $OutFile -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($existing.commit -eq $head -and $existing.baseVersion -eq $BaseVersion -and $existing.dirty -eq $dirty) { exit 0 }
    }
    catch { }
}

# ---- every commit: hash, parents, date, subject, body
$commits = @{}
$raw = (Invoke-Git @('log', "--format=%H$sep%P$sep%ad$sep%s$sep%b$rec", '--date=format:%Y-%m-%d')) -join "`n"
foreach ($chunk in $raw.Split($rec)) {
    $text = $chunk.Trim("`n", "`r", ' ')
    if (-not $text) { continue }
    $f = $text.Split($sep)
    $setup = @()
    foreach ($line in ($f[4] -split "`n")) {
        if ($line -match '^\s*Setup:\s*(.+)$') { $setup += [ordered]@{ kind = 'Manual'; text = $Matches[1].Trim() } }
    }
    $commits[$f[0]] = @{
        hash = $f[0]; parents = @($f[1].Split(' ', [StringSplitOptions]::RemoveEmptyEntries))
        date = $f[2]; subject = $f[3]; setup = $setup; n = 0; module = ''
    }
}

# ---- number them along the first-parent line
$line = New-Object System.Collections.Generic.List[string]
$cursor = $head
while ($cursor -and $commits.ContainsKey($cursor)) {
    $line.Add($cursor)
    $cursor = if ($commits[$cursor].parents.Count -gt 0) { $commits[$cursor].parents[0] } else { $null }
}
$line.Reverse()

$seen = New-Object System.Collections.Generic.HashSet[string]
foreach ($mainline in $line) {
    $arrived = New-Object System.Collections.Generic.List[string]
    $stack = New-Object System.Collections.Generic.Stack[string]
    $stack.Push($mainline)
    while ($stack.Count -gt 0) {
        $c = $stack.Pop()
        if (-not $commits.ContainsKey($c) -or -not $seen.Add($c)) { continue }
        $arrived.Add($c)
        foreach ($p in $commits[$c].parents) { $stack.Push($p) }
    }
    foreach ($c in $arrived) { $commits[$c].n = $seen.Count }
}

# ---- which module each change is mostly about, from the paths it touched
$current = $null
$tally = @{}
function Save-Module {
    if ($script:current -and $script:tally.Count -gt 0 -and $commits.ContainsKey($script:current)) {
        $commits[$script:current].module = ($script:tally.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Key
    }
}
foreach ($l in (Invoke-Git @('log', '--name-only', "--format=$rec%H"))) {
    if ($l.StartsWith($rec)) { Save-Module; $current = $l.Substring(1); $tally = @{}; continue }
    if ($l -match 'HCMv3\.(?:Application|Application\.Shared|Core)/([A-Za-z]+)/' -or
        $l -match 'angular/src/app/(?:admin|main)/([a-z0-9-]+)/' -or
        $l -match 'test/HCMv3\.Tests/([A-Za-z]+)/') {
        $name = (($Matches[1] -split '-') | ForEach-Object { if ($_) { $_.Substring(0, 1).ToUpper() + $_.Substring(1) } }) -join ''
        $tally[$name] = 1 + [int]$tally[$name]
    }
}
Save-Module

# ---- what each change added that a tenant may have to set up
function Add-Setup([string]$hash, [string]$kind, [string]$text) {
    if ($commits.ContainsKey($hash)) { $commits[$hash].setup += [ordered]@{ kind = $kind; text = $text } }
}

$watched = @(
    'aspnet-core/src/HCMv3.Core.Shared/Authorization/AppPermissions.cs',
    'aspnet-core/src/HCMv3.Core/Navigation/ScreenCatalog.cs',
    'aspnet-core/src/HCMv3.Core/Configuration/AppSettings.cs'
)
$current = $null; $file = ''
foreach ($l in (Invoke-Git (@('log', '-p', '--unified=0', '--no-color', "--format=$rec%H", '--') + $watched))) {
    if ($l.StartsWith($rec)) { $current = $l.Substring(1); continue }
    if ($l.StartsWith('+++ ')) { $file = $l; continue }
    if (-not $l.StartsWith('+') -or $l.StartsWith('+++')) { continue }

    if ($file -like '*AppPermissions.cs' -and $l -match 'public const string \w+\s*=\s*"([^"]+)"') {
        Add-Setup $current 'Permission' $Matches[1]
    }
    elseif ($file -like '*ScreenCatalog.cs' -and $l -match 'new\("[^"]+",\s*"([A-Z]+\d+)"') {
        Add-Setup $current 'Screen' $Matches[1]
    }
    elseif ($file -like '*AppSettings.cs' -and $l -match 'public const string \w+\s*=\s*"(App\.[^"]+)"') {
        Add-Setup $current 'Setting' $Matches[1]
    }
}

$current = $null
foreach ($l in (Invoke-Git @('log', '--diff-filter=A', '--name-only', "--format=$rec%H", '--', 'aspnet-core/src/HCMv3.EntityFrameworkCore/Migrations'))) {
    if ($l.StartsWith($rec)) { $current = $l.Substring(1); continue }
    if ($l -match 'Migrations/(\d{14}_[A-Za-z0-9_]+)\.cs$') { Add-Setup $current 'Database' $Matches[1] }
}

# ---- write it, newest change first
$number = $seen.Count
$changes = $commits.Values | Where-Object { $_.n -gt 0 } | Sort-Object @{ Expression = { $_.n }; Descending = $true }, @{ Expression = { $_.date }; Descending = $true } |
    ForEach-Object {
        [ordered]@{ n = $_.n; hash = $_.hash.Substring(0, 8); date = $_.date; subject = $_.subject; module = $_.module; setup = @($_.setup) }
    }

$log = [ordered]@{
    baseVersion = $BaseVersion
    version     = "$BaseVersion.$number"
    number      = $number
    commit      = $head
    dirty       = $dirty
    builtOn     = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    changes     = @($changes)
}

$dir = Split-Path $OutFile -Parent
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }
[System.IO.File]::WriteAllText($OutFile, ($log | ConvertTo-Json -Depth 6 -Compress), (New-Object System.Text.UTF8Encoding($false)))
Write-Host "release-log: $($log.version) ($($changes.Count) changes)"
