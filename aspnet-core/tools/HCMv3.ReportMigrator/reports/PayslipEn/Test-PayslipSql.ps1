<#
    Executes each rewritten payslip query against HCMv3 with the sample parameters and reports
    row counts. Proves the SQL layer actually returns rows -- a converted layout bound to SQL that
    silently returns nothing is the single easiest way for this migration to look finished and
    not be.

    Connection comes from the ConnectionStrings__Default environment variable (appsettings.json is
    the shared team file and must not be touched); falls back to the local HCMv3 instance.
#>
param(
    [string]$ConnectionString = $(if ($env:ConnectionStrings__Default) { $env:ConnectionStrings__Default }
                                 else { "Data Source=localhost\SQLEXPRESS;Initial Catalog=HCMv3;Integrated Security=SSPI;TrustServerCertificate=True" }),
    [int]$TenantId = 1,
    [string]$EmpCode = "3400",
    [int]$Month = 6,
    [int]$Year = 2024,
    [int]$RunType = 0,
    [switch]$Dump
)

$sqlDir = Join-Path $PSScriptRoot "queries"
$forPeriod = (Get-Date -Year $Year -Month $Month -Day 1).AddMonths(1).AddDays(-1).Date

# Every parameter any of the 11 payslip queries references. Stimulsoft binds these by name at render
# time; here they are declared up front so each file can be executed standalone.
$parameters = @{
    TenantId           = $TenantId
    PrmEmpCode         = $EmpCode
    empcode            = $EmpCode
    PrmPayrollArea     = ""
    PrmMonth           = $Month
    PrmYear            = $Year
    PrmPayrollRunType  = $RunType
    RunType            = $RunType
    PrmEncashment      = 0
    PrmEmployeeStatus  = -1
    ForPeriod          = $forPeriod
    InPeriod           = $forPeriod
}

$results = @()
foreach ($file in Get-ChildItem $sqlDir -Filter *.sql | Sort-Object Name) {
    $sql = Get-Content $file.FullName -Raw
    $conn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        foreach ($k in $parameters.Keys) {
            if ($sql -match "@$k\b") { $cmd.Parameters.AddWithValue("@$k", $parameters[$k]) | Out-Null }
        }
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
        $table = New-Object System.Data.DataTable
        $rows = $adapter.Fill($table)
        $results += [pscustomobject]@{ Query = $file.BaseName; Rows = $rows; Columns = $table.Columns.Count; Error = "" }
        if ($Dump -and $rows -gt 0) {
            Write-Host "----- $($file.BaseName) -----"
            $table | Select-Object -First 5 | Format-List | Out-String | Write-Host
        }
    } catch {
        $results += [pscustomobject]@{ Query = $file.BaseName; Rows = -1; Columns = 0; Error = $_.Exception.Message }
    } finally { $conn.Close() }
}

$results | Format-Table -AutoSize
$bad = @($results | Where-Object { $_.Rows -lt 0 })
if ($bad.Count) { Write-Host "`nFAILED: $($bad.Count) query/queries did not execute." -ForegroundColor Red; exit 1 }
$empty = @($results | Where-Object { $_.Rows -eq 0 })
Write-Host "`nAll $($results.Count) queries executed. $($empty.Count) returned zero rows (expected for run-type-specific subreports)."
