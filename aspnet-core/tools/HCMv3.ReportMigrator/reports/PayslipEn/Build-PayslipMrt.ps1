<#
.SYNOPSIS
    Produces the HCMv3 payslip .mrt from the converted legacy layout plus the rewritten SQL.

.DESCRIPTION
    Step 3 of the per-report pipeline:

        1. Export-LegacyRepx.ps1     HCM10.Module.dll  ->  .repx
        2. HCMv3.RepxToMrt           .repx             ->  .mrt   (layout only; SQL is legacy)
        3. Build-PayslipMrt.ps1      .mrt + queries\*.sql  ->  .mrt   (SQL rewritten for HCMv3)  <-- here
        4. HCMv3.MrtRender           .mrt              ->  .pdf   (acceptance gate)

    Keeping the rewritten SQL in plain .sql files rather than hand-editing the .mrt matters: the .mrt
    is a machine-generated artefact that gets regenerated whenever the importer improves, and any SQL
    edited directly into it would be lost. This script is re-runnable and idempotent.

    Three things are rewritten, not just the SQL text:

    * Variables. The importer emits every DevExpress report parameter as an untyped System.String
      variable with an EMPTY category, while the data-source parameter expressions it generates refer
      to them as [Parameters.<name>] -- a category that no longer exists, so every parameter silently
      binds to null. Both halves are regenerated here with real types.

    * Data-source parameters. Rebuilt from the @names actually present in each rewritten .sql, so
      adding @TenantId to a query is all that is needed for it to be bound.

    * TenantId. Absent from the entire legacy corpus and mandatory in HCMv3.
#>
[CmdletBinding()]
param(
    [string]$SourceMrt = (Join-Path $PSScriptRoot "..\..\out\mrt-all\HCM10.Module.Reports.Payroll.rptPayslipEn.mrt"),
    [string]$OutMrt    = (Join-Path $PSScriptRoot "rptPayslipEn.HCMv3.mrt"),
    # The .repx is still needed at this stage: it carries per-control formatting the importer drops.
    [string]$SourceRepx = (Join-Path $PSScriptRoot "..\..\out\repx-all\HCM10.Module.Reports.Payroll.rptPayslipEn.repx")
)

$ErrorActionPreference = "Stop"
$sqlDir = Join-Path $PSScriptRoot "queries"

# Every parameter any payslip query can reference, and its CLR type. Stimulsoft needs the type to
# build a correctly-typed SqlParameter; getting DateTime wrong here turns an equality test on
# ForPeriod into a string comparison that never matches.
$parameterTypes = @{
    TenantId          = "System.Int32"
    PrmEmpCode        = "System.String"
    empcode           = "System.String"
    PrmPayrollArea    = "System.String"
    PrmMonth          = "System.Int32"
    PrmYear           = "System.Int32"
    PrmPayrollRunType = "System.Int32"
    RunType           = "System.Int32"
    PrmEncashment     = "System.Int32"
    PrmEmployeeStatus = "System.Int32"
    ForPeriod         = "System.DateTime"
    InPeriod          = "System.DateTime"
}

# The 4th field of a Stimulsoft data-source parameter is System.Data.SqlDbType-as-int, and the
# importer leaves it at 0 -- which is SqlDbType.BigInt, not "unspecified". Every string and date
# parameter therefore arrives at SQL Server as a bigint and blows up with
#   "Failed to convert parameter value from a String to a Int64".
# (The importer does try: ProcessQuery sets NVarChar when the expression starts with the literal
# "(System.String)", but the expression it generates is assembly-qualified -- "(System.String,
# mscorlib, ...)" -- so the test never fires. Reported as an importer gap; corrected here.)
$sqlDbTypes = @{
    "System.String"   = 12   # NVarChar
    "System.Int32"    = 8    # Int
    "System.DateTime" = 4    # DateTime
}

# Stimulsoft stores list entries as comma-separated fields inside a single <value> element, so any
# character that would break that encoding is escaped _xNNNN_ style.
function Encode-StiField([string]$s) {
    $map = [ordered]@{ '(' = '_x0028_'; ')' = '_x0029_'; ',' = '_x002C_'; ' ' = '_x0020_';
                       '=' = '_x003D_'; '[' = '_x005B_'; ']' = '_x005D_' }
    foreach ($k in $map.Keys) { $s = $s.Replace($k, $map[$k]) }
    return $s
}

[xml]$doc = Get-Content $SourceMrt -Encoding UTF8

# ---------------------------------------------------------------------------------------------
# 1. Regenerate the report variables with real types.
# ---------------------------------------------------------------------------------------------
$variablesNode = $doc.SelectSingleNode("//Dictionary/Variables")
while ($variablesNode.HasChildNodes) { $variablesNode.RemoveChild($variablesNode.FirstChild) | Out-Null }
foreach ($name in $parameterTypes.Keys) {
    $value = $doc.CreateElement("value")
    # Category,Name,Alias,Description,Type,Value,ReadOnly,Required,AllowUseAsSqlParameter,RequestFromUser
    $value.InnerText = ",$name,,,$($parameterTypes[$name]),,False,False,True,False"
    $variablesNode.AppendChild($value) | Out-Null
}
$variablesNode.SetAttribute("count", $parameterTypes.Count)
Write-Host "variables: $($parameterTypes.Count) regenerated with real CLR types"

# ---------------------------------------------------------------------------------------------
# 2. Swap SQL and rebuild each data source's parameter list.
# ---------------------------------------------------------------------------------------------
$replaced = 0
foreach ($ds in $doc.SelectNodes("//Dictionary/DataSources/*")) {
    $name = $ds.SelectSingleNode("Name").InnerText
    $sqlFile = Join-Path $sqlDir "$name.sql"
    if (-not (Test-Path $sqlFile)) {
        Write-Warning "no rewritten SQL for data source '$name' -- left on the LEGACY query"
        continue
    }

    $sql = Get-Content $sqlFile -Raw
    $sqlNode = $ds.SelectSingleNode("SqlCommand")
    if ($null -eq $sqlNode) {
        $sqlNode = $doc.CreateElement("SqlCommand")
        $ds.AppendChild($sqlNode) | Out-Null
    }
    $sqlNode.InnerText = $sql

    $paramsNode = $ds.SelectSingleNode("Parameters")
    if ($null -eq $paramsNode) {
        $paramsNode = $doc.CreateElement("Parameters")
        $paramsNode.SetAttribute("isList", "true")
        $ds.AppendChild($paramsNode) | Out-Null
    }
    while ($paramsNode.HasChildNodes) { $paramsNode.RemoveChild($paramsNode.FirstChild) | Out-Null }

    $used = [regex]::Matches($sql, '@(\w+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
    $count = 0
    foreach ($p in $used) {
        if (-not $parameterTypes.ContainsKey($p)) {
            # A local DECLARE inside the query body, not a report parameter.
            continue
        }
        $clr = $parameterTypes[$p]
        $expression = "($clr, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)([$p])"
        $value = $doc.CreateElement("value")
        # StiDataParameterConverter.ConvertTo serialises "Name,Expression,Type,Size" -- Type BEFORE
        # Size, which is the opposite of the StiDataParameter.Order enum declaration order.
        # Size must be non-zero for variable-length types or SqlCommand.Prepare refuses the command.
        $size = if ($clr -eq "System.String") { 4000 } else { 0 }
        $value.InnerText = "$p,$(Encode-StiField $expression),$($sqlDbTypes[$clr]),$size"
        $paramsNode.AppendChild($value) | Out-Null
        $count++
    }
    $paramsNode.SetAttribute("count", $count)

    Write-Host ("  {0,-26} sql={1,6} chars  params={2}" -f $name, $sql.Length, $count)
    $replaced++
}

Write-Host "data sources rewritten: $replaced"

# ---------------------------------------------------------------------------------------------
# 3. Rebind the layout to the data sources.
#
#     This is the part of the conversion the importer cannot do, and it is the real per-report cost.
#     Three separate defects have to be repaired:
#
#     (a) EVERY data band is emitted with DataSourceName "ds" and EVERY field expression as
#         {ds.<Column>}. DevExpress stores bindings relative to a control's DataMember, and the
#         importer has no resolver for that, so it writes a placeholder. Unbound bands render the
#         full layout with every value cell blank -- which looks like "this employee has no data",
#         not like a broken report. Nothing warns about it.
#
#     (b) Column names containing spaces ("Employee Code") are written raw. Stimulsoft stores them
#         XML-name-encoded ("Employee_x0020_Code") in the data source's column list, so the raw form
#         resolves to nothing (interpretation mode) or fails to compile (compilation mode -- this is
#         the CS1003 wall you get from StiReport.Compile()).
#
#     (c) DevExpress CalculatedFields are dropped entirely -- 458 occurrences across the corpus, the
#         single most functionally significant importer gap. Expressions referencing them
#         ({ds.CalcNetPay}) therefore evaluate to nothing. They are re-inlined here.
#
#     The page -> data source mapping below is per-report knowledge. It is mechanical to READ off the
#     .repx (each DevExpress subreport carries exactly one query) but it cannot be inferred from the
#     .mrt alone, because the importer has already thrown the association away.
# ---------------------------------------------------------------------------------------------
$pageDataSource = [ordered]@{
    "Page1"                                = "PayslipOrgV2Query"
    "sub_srptPaySlipOvertimeHoursEn"       = "OvertimeQuery"
    "sub_srptPaySlipBasicPayEn"            = "BasicPayQuery"
    "sub_srptPayslipLeaveEncashment"       = "LeaveEncashQuery"
    "sub_srptPayslipLeaveInfo"             = "LeaveInfoQuery"
    "sub_srptPayslipIndemnityDaysSummaryEn" = "PayslipIndemnityQuery"
    "sub_srptPayslipWorkingDaysSummary"    = "PayslipWorkingDayQuery"
    "sub_srptPayslipAttendenceSummaryEn"   = "PayslipAttendanceQuery"
    "sub_srptPayslipEarning"               = "PayslipEarningQuery"
    "sub_srptPayslipDeduction"             = "PayslipDeductionQuery"
}

# Legacy CalculatedFields, read out of the .repx <CalculatedFields> nodes the importer discards.
# "{ds}" is substituted with the page's data source.
$calculatedFields = [ordered]@{
    CalcDateTime = "Today"                                   # DevExpress LocalDateTimeNow()
    CalcNetPay   = 'Round(PayslipOrgV2Query["Net Pay"],3)'  # DevExpress Round([Net Pay],3)
    CalcAmount   = "{ds}.Amount"                              # DevExpress [Amount]
    # NOTE: the .repx also declares a lower-case "calcAmount" = Round([Amount],3) on a subreport that
    # this layout does not use. PowerShell hash keys are case-INSENSITIVE, so the two cannot coexist
    # here; if a future report needs both, this table must move to a case-sensitive collection.
}

function Get-ColumnAccessor([string]$dataSource, [string]$column) {
    # Legacy SELECT aliases are full of spaces ("Employee Code", "Wage Type", "Net Pay"), which are
    # legal Stimulsoft column names but NOT legal in dotted expression syntax. Two encodings look
    # plausible and only one works:
    #   {DS.Employee_x0020_Code}   -- the _xNNNN_ form Stimulsoft uses INSIDE its serialized <Columns>
    #                                 list. In an expression it silently resolves to nothing: the cell
    #                                 renders blank, no warning, no error.
    #   {DS["Employee Code"]}      -- the indexer form. This is the one that works.
    # Verified empirically by rendering: the dotted form left every spaced column blank while
    # unspaced ones (Grade, Jobtitle) populated correctly.
    if ($column -match '^[A-Za-z_][A-Za-z0-9_]*$') { return "$dataSource.$column" }
    return "$dataSource[`"$column`"]"
}

$rebound = 0
$expressionsFixed = 0
foreach ($page in $doc.SelectNodes("/StiSerializer/Pages/*")) {
    if (-not $pageDataSource.Contains($page.Name)) { continue }
    $ds = $pageDataSource[$page.Name]

    # (a) point the band at the real source
    $bandName = $null
    foreach ($dsn in $page.SelectNodes(".//DataSourceName")) {
        $dsn.InnerText = $ds
        $bandName = $dsn.ParentNode.Name
        $rebound++
    }

    foreach ($text in $page.SelectNodes(".//*[local-name()='Text']")) {
        $expr = $text.InnerText
        if ($expr -notmatch '\{') { continue }
        $original = $expr

        # (c) summary aggregates: the importer mangles DevExpress's sumSum(X) into a literal.
        $expr = [regex]::Replace($expr, 'ds\.sumSum\(\s*([^)]+?)\s*\)', {
            param($m) "Sum($bandName, $(Get-ColumnAccessor $ds $m.Groups[1].Value))"
        })

        # (c) inline the dropped calculated fields, longest name first so calcAmount/CalcAmount
        #     do not shadow each other.
        foreach ($cf in ($calculatedFields.Keys | Sort-Object { $_.Length } -Descending)) {
            $replacement = $calculatedFields[$cf].Replace("{ds}", $ds)
            $expr = [regex]::Replace($expr, "(?:ds\.)?\b$cf\b", $replacement)
        }

        # (a)+(b) remaining {ds.Column} references
        $expr = [regex]::Replace($expr, 'ds\.([A-Za-z_][A-Za-z0-9_ ]*)', {
            param($m) Get-ColumnAccessor $ds $m.Groups[1].Value.TrimEnd()
        })

        if ($expr -ne $original) {
            $text.InnerText = $expr
            $expressionsFixed++
        }
    }
}
Write-Host "layout rebound: $rebound data band(s), $expressionsFixed expression(s) repaired"

# ---------------------------------------------------------------------------------------------
# 4. Restore the number/date formats the importer drops.
#
#     DevExpress stores per-control TextFormatString ("{0:#0.000}", "{0:dd-MM-yyyy}"). The importer
#     has no case for it -- it is part of the 591 "StringFormat" warnings, which the earlier triage
#     filed under "per-control cosmetics". On a payroll document it is not cosmetic: without it a
#     KWD amount renders as 178.833333 instead of 178.833, and a date as
#     "04-Jul-07 12:00:00 AM" instead of "04-07-2007".
#
#     Rather than serialise Stimulsoft's StiTextFormat object graph, the format is applied by
#     wrapping the expression in Format(), which is equivalent and far less brittle.
#
#     Formats are read back out of the original .repx and matched by control name.
# ---------------------------------------------------------------------------------------------
if (Test-Path $SourceRepx) {
    [xml]$repx = Get-Content $SourceRepx
    $formats = @{}
    $ambiguous = @{}
    foreach ($node in $repx.SelectNodes("//*[@TextFormatString]")) {
        $ctrl = $node.GetAttribute("Name")
        $fmt = $node.GetAttribute("TextFormatString")
        if ([string]::IsNullOrWhiteSpace($ctrl) -or [string]::IsNullOrWhiteSpace($fmt)) { continue }
        if ($formats.ContainsKey($ctrl) -and $formats[$ctrl] -ne $fmt) { $ambiguous[$ctrl] = $true }
        else { $formats[$ctrl] = $fmt }
    }

    $formatted = 0
    foreach ($text in $doc.SelectNodes("//*[local-name()='Text']")) {
        $comp = $text.ParentNode.Name
        # The importer suffixes table-cell labels ("xrTableCell5Label" for cell "xrTableCell5").
        $lookup = if ($formats.ContainsKey($comp)) { $comp }
                  elseif ($comp.EndsWith("Label") -and $formats.ContainsKey($comp.Substring(0, $comp.Length - 5))) { $comp.Substring(0, $comp.Length - 5) }
                  else { $null }
        if (-not $lookup) { continue }
        if ($ambiguous.ContainsKey($lookup)) {
            Write-Warning "control '$lookup' has conflicting TextFormatStrings in the .repx -- format NOT applied"
            continue
        }
        $expr = $text.InnerText
        if ($expr -notmatch '^\{(.+)\}$') { continue }
        $inner = $Matches[1]
        if ($inner -match '^Format\(') { continue }
        $text.InnerText = '{Format("' + $formats[$lookup] + '", ' + $inner + ')}'
        $formatted++
    }
    Write-Host "formats restored from .repx: $formatted control(s), $($ambiguous.Count) ambiguous name(s) skipped"
}

# Name-based recovery only reaches the master page. The importer RENAMES controls when flattening
# subreports into pages to avoid collisions -- xrLabel11 becomes xrLabel110/xrLabel114, and, worse,
# an unrelated control inherits the name "LoggedUserNme5". So the .repx formats simply cannot be
# matched back for anything inside a subreport.
#
# For those, the format is applied by COLUMN instead. Every monetary/quantity column on this payslip
# prints with the legacy DevExpress format {0:#0.000} (which is also the KWD minor unit -- 3 places).
$numericColumns = @("Amount", "Rate", "Number", "ActualHrs", "Net Pay")
$defaulted = 0
foreach ($text in $doc.SelectNodes("//*[local-name()='Text']")) {
    $expr = $text.InnerText
    if ($expr -notmatch '^\{(.+)\}$') { continue }
    $inner = $Matches[1]
    if ($inner -match '^Format\(') { continue }
    $matchesNumeric = $false
    foreach ($col in $numericColumns) {
        if ($inner -match ("\.{0}\b" -f [regex]::Escape($col)) -or $inner -match ('\["{0}"\]' -f [regex]::Escape($col))) {
            $matchesNumeric = $true; break
        }
    }
    if (-not $matchesNumeric) { continue }
    $text.InnerText = '{Format("{0:#0.000}", ' + $inner + ')}'
    $defaulted++
}
Write-Host "numeric format applied by column: $defaulted control(s)"

# ---------------------------------------------------------------------------------------------
# 5. Code-behind.
#
#     rptPayslipEn.cs did four things. Three are handled here or in the caller; one is deliberately
#     NOT ported. See PORTING-NOTES.md for the reasoning.
#
#     * LoggedUserNme.Text = SecuritySystem.CurrentUserName -- the constructor STAMPED the current
#       user into the label at construction time, so the exported .repx (and therefore the .mrt)
#       contains whatever user the exporter ran as. It must become a report variable supplied by the
#       caller, or every payslip in HCMv3 is footed "RepxExportUser".
# ---------------------------------------------------------------------------------------------
$stamped = 0
foreach ($text in $doc.SelectNodes("//*[local-name()='Text']")) {
    if ($text.ParentNode.Name -like "LoggedUserNme*" -and $text.InnerText -notmatch '\{') {
        $text.InnerText = "{LoggedUserName}"
        $stamped++
    }
}
if ($stamped -gt 0) {
    $value = $doc.CreateElement("value")
    $value.InnerText = ",LoggedUserName,,,System.String,,False,False,False,False"
    $variablesNode.AppendChild($value) | Out-Null
    $variablesNode.SetAttribute("count", [int]$variablesNode.GetAttribute("count") + 1)
    Write-Host "code-behind: $stamped baked-in user label(s) replaced with the LoggedUserName variable"
}

$doc.Save($OutMrt)
Write-Host "wrote $OutMrt ($((Get-Item $OutMrt).Length) bytes)"
