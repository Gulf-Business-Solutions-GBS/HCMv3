<#
    why-no-answer.ps1

    For when the assistant says "The AI assistant is not available right now".
    That message means the API could not get an answer out of the model program
    on this server -- it is not about the question being slow.

    READ-ONLY by default: it looks, and tells you in plain words what is wrong
    and what to do. Add -Restart to also start the model program if it is
    stopped, which is the usual answer.

    Run on the server, in PowerShell (Administrator sees more):

        Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
        .\why-no-answer.ps1

    It saves a copy of the report to the Desktop, so it can be sent back.
#>

[CmdletBinding()]
param(
    [string] $BaseUrl = 'http://localhost:11434',
    [string] $Model = 'hcmv3-qwen3-4b',
    [string] $ApiLogFolder = 'C:\inetpub\HCMv3\api\App_Data\Logs',
    [switch] $Restart
)

$ErrorActionPreference = 'Continue'
$report = New-Object System.Collections.Generic.List[string]
$problems = New-Object System.Collections.Generic.List[string]

function Out-Line { param([string] $t = '') $report.Add($t); Write-Host $t }
function Head { param([string] $t) Out-Line ''; Out-Line ('=' * 70); Out-Line " $t"; Out-Line ('=' * 70) }
function Row { param([string] $k, $v) Out-Line ("  {0,-28} {1}" -f $k, $v) }
function Problem { param([string] $t) $problems.Add($t) }

Head "AI ASSISTANT - WHY NO ANSWER   $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
Row 'Computer' $env:COMPUTERNAME
Row 'Model address' $BaseUrl
Row 'Model name' $Model

# ------------------------------------------------------------------ is it running at all
Head '1. IS THE MODEL PROGRAM RUNNING?'

$process = Get-Process -Name 'ollama*' -ErrorAction SilentlyContinue
Row 'Ollama process' $(if ($process) { "yes ($($process.Count) running)" } else { 'NO' })

$task = Get-ScheduledTask -TaskName 'HCMv3 AI - Ollama' -ErrorAction SilentlyContinue
Row 'Start-at-boot task' $(if ($task) { $task.State } else { 'not installed' })

$listening = Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction SilentlyContinue
Row 'Listening on 11434' $(if ($listening) { 'yes' } else { 'NO' })

if (-not $process -or -not $listening) {
    Problem 'The model program (Ollama) is not running. That is why the assistant says it is not available.'

    if ($Restart -and $task) {
        Out-Line ''
        Out-Line '  Starting it now (-Restart was given)...'
        Start-ScheduledTask -TaskName 'HCMv3 AI - Ollama'
        Start-Sleep -Seconds 20
        $listening = Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction SilentlyContinue
        Row 'Listening now' $(if ($listening) { 'yes -- try the assistant again' } else { 'still not' })
    }
    elseif ($task) {
        Out-Line ''
        Out-Line "  To start it:  Start-ScheduledTask -TaskName 'HCMv3 AI - Ollama'"
        Out-Line '  Or run this script again with -Restart'
    }
}

# ------------------------------------------------------------------ does it answer
Head '2. DOES IT ANSWER?'

$tags = $null
try {
    $tags = Invoke-RestMethod -Uri "$BaseUrl/api/tags" -TimeoutSec 20
    Row 'Answers a simple request' 'yes'
}
catch {
    Row 'Answers a simple request' "NO -- $($_.Exception.Message)"
    Problem 'The model program is not answering on this server.'
}

if ($tags) {
    $names = @($tags.models | ForEach-Object { $_.name })
    Row 'Models installed' $(if ($names) { ($names -join ', ') } else { 'none' })

    $hasModel = $names | Where-Object { $_ -eq $Model -or $_ -eq "$Model`:latest" }
    Row "Has '$Model'" $(if ($hasModel) { 'yes' } else { 'NO' })

    if (-not $hasModel) {
        Problem "The model '$Model' is not installed any more. Re-run install-ollama.ps1, or check AiAgent:Model in api\appsettings.Production.json matches one of the models listed above."
    }
}

# A real question, timed. This is the one that catches a server that answers but is far too slow.
if ($tags) {
    Out-Line ''
    Out-Line '  Asking the model a short question (this can take a few minutes on a slow server)...'

    $body = @{
        model    = $Model
        messages = @(@{ role = 'user'; content = 'Reply with the single word: ready' })
        stream   = $false
    } | ConvertTo-Json -Depth 5

    $started = Get-Date
    try {
        $answer = Invoke-RestMethod -Uri "$BaseUrl/v1/chat/completions" -Method Post -Body $body -ContentType 'application/json' -TimeoutSec 900
        $seconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
        Row 'Answered in' "$seconds seconds"
        Row 'Said' ($answer.choices[0].message.content -replace '\s+', ' ').Trim()

        if ($seconds -gt 240) {
            Problem "The model answers, but took $seconds seconds for one word. Real questions will take much longer. The server is short of memory or too busy -- check section 3."
        }
    }
    catch {
        $seconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
        Row 'Failed after' "$seconds seconds"
        Row 'Error' $_.Exception.Message
        Problem 'The model program is there but could not answer a question. Look at C:\Ollama\ollama-serve.log for the reason -- running out of memory is the usual one.'
    }
}

# ------------------------------------------------------------------ room to work
Head '3. HAS THE SERVER ROOM TO RUN IT?'

$os = Get-CimInstance Win32_OperatingSystem
$totalGb = [math]::Round($os.TotalVisibleMemorySize / 1MB, 1)
$freeGb = [math]::Round($os.FreePhysicalMemory / 1MB, 1)
Row 'Memory total' "$totalGb GB"
Row 'Memory free' "$freeGb GB"

if ($freeGb -lt 4) {
    Problem "Only $freeGb GB of memory is free; the model needs about 4 GB to answer. Something else on the server is using it, or the server is too small."
}

$disk = Get-PSDrive -Name C -ErrorAction SilentlyContinue
if ($disk) {
    Row 'Disk free (C:)' "$([math]::Round($disk.Free / 1GB, 1)) GB"
}

$log = 'C:\Ollama\ollama-serve.log'
if (Test-Path $log) {
    $size = [math]::Round((Get-Item $log).Length / 1MB, 1)
    Row 'Model log' "$log ($size MB)"
    Out-Line ''
    Out-Line '  Last lines of the model log:'
    Get-Content $log -Tail 15 | ForEach-Object { Out-Line "    $_" }
}
else {
    Row 'Model log' 'not found'
}

# ------------------------------------------------------------------ what the API recorded
Head '4. WHAT THE API RECORDED'

if (Test-Path $ApiLogFolder) {
    $logFile = Get-ChildItem $ApiLogFolder -Filter '*.txt' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if ($logFile) {
        Row 'API log' $logFile.FullName
        $lines = Get-Content $logFile.FullName -Tail 4000 | Where-Object { $_ -match 'AI agent model call failed|AiAgent|ollama' }

        if ($lines) {
            Out-Line ''
            Out-Line '  The last few AI lines:'
            $lines | Select-Object -Last 12 | ForEach-Object { Out-Line "    $_" }
        }
        else {
            Row 'AI lines found' 'none in the recent log'
        }
    }
}
else {
    Row 'API log folder' "not found at $ApiLogFolder (pass -ApiLogFolder)"
}

# ------------------------------------------------------------------ verdict
Head 'WHAT TO DO'

if ($problems.Count -eq 0) {
    Out-Line '  Nothing wrong found: the model program is running and answered a question.'
    Out-Line '  If the assistant still fails, the API may not have been restarted since'
    Out-Line '  appsettings was changed -- recycle the site in IIS and try again.'
}
else {
    $n = 0
    foreach ($problem in $problems) {
        $n++
        Out-Line "  $n. $problem"
        Out-Line ''
    }
}

$path = Join-Path ([Environment]::GetFolderPath('Desktop')) "AI-why-no-answer-$(Get-Date -Format 'yyyy-MM-dd-HHmm').txt"
$report | Set-Content -Path $path -Encoding utf8
Out-Line ''
Out-Line "Report saved to: $path"
