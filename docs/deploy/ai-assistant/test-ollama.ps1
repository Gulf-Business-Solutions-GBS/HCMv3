<#
.SYNOPSIS
    Checks the AI model is running and can do what the HCMv3 assistant needs: answer on 127.0.0.1, and turn a
    request into a tool call with the right arguments. Takes one to several minutes on a server without a GPU.
#>
[CmdletBinding()]
param([string] $Model = "hcmv3-qwen3-4b")

$ErrorActionPreference = "Stop"
$url = "http://127.0.0.1:11434/v1/chat/completions"

$body = @{
    model       = $Model
    stream      = $false
    temperature = 0.2
    messages    = @(
        @{ role = "system"; content = "You are the HR assistant inside HCMv3. Today is 2026-09-18. This user is an HR/payroll administrator. Use the tools. /no_think" },
        @{ role = "user"; content = "add 50kd car allowance in basic pays to employee 12001 starting 01/10/2026" }
    )
    tools       = @(
        @{
            type     = "function"
            function = @{
                name        = "prepare_basic_pay_change"
                description = "Prepare a change to an employee's basic pay from an effective date, for the user to confirm."
                parameters  = @{
                    type       = "object"
                    properties = @{
                        employeeCode  = @{ type = "string" }
                        effectiveDate = @{ type = "string"; description = "yyyy-MM-dd" }
                        changes       = @{ type = "array"; items = @{ type = "object"; properties = @{ wageType = @{ type = "string" }; amount = @{ type = "number" } } } }
                    }
                }
            }
        }
    )
} | ConvertTo-Json -Depth 12

Write-Host "Asking $Model (this can take a few minutes on CPU)..."
$sw = [Diagnostics.Stopwatch]::StartNew()
$r = Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 900
$sw.Stop()

$call = $r.choices[0].message.tool_calls | Select-Object -First 1
Write-Host ("Answered in {0:N0}s" -f $sw.Elapsed.TotalSeconds)

if (-not $call) {
    Write-Host "FAIL: no tool call. Reply was: $($r.choices[0].message.content)" -ForegroundColor Red
    exit 1
}

$callArgs = $call.function.arguments | ConvertFrom-Json
Write-Host "Tool: $($call.function.name)  Arguments: $($call.function.arguments)"

if ($call.function.name -eq "prepare_basic_pay_change" -and $callArgs.employeeCode -eq "12001" -and $callArgs.effectiveDate -eq "2026-10-01") {
    Write-Host "PASS: the model is working for the assistant." -ForegroundColor Green
} else {
    Write-Host "CHECK: a tool was called but the arguments differ from expected (12001, 2026-10-01). Look at them above." -ForegroundColor Yellow
}
