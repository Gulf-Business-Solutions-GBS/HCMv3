<#
.SYNOPSIS
    Exports every DevExpress XtraReport in the legacy HCM10.Module assembly to a .repx layout file.

.DESCRIPTION
    The legacy reports only exist as compiled DevExpress designer code -- there are no .repx files in
    the HRMS_Production source tree. The only way to get a portable layout out of them is to
    instantiate each report type and call XtraReport.SaveLayoutToXml().

    That requires hosting DevExpress v17.2 (a .NET Framework 4.x product), so this script must run on
    Windows PowerShell 5.1 -- NOT PowerShell 7+, which is .NET Core and cannot load these assemblies.

    Two things make the raw "new up every type" approach fail:

    1. HCM10.Module.dll's 203 dependencies are not next to this script. An AssemblyResolve hook
       redirects every load to the legacy bin\Debug folder.

    2. 38 report types (the whole payslip family) run real application code in their constructor:
       they read SecuritySystem.CurrentUserName, open an XPObjectSpaceProvider against
       ConfigurationManager.ConnectionStrings["HCM10ConnectionString"], and query PermissionPolicyUser.
       With no XAF application hosting them, CurrentUserName is "" and the user lookup returns null,
       so the constructor NREs. Bypassing the constructor (FormatterServices.GetUninitializedObject)
       does not work either -- the XtraReport base state never initialises and SaveLayoutToXml
       produces an empty layout.

       The fix is to give the process the two things the constructor asks for:
         a. a SecuritySystem instance whose UserName is a real row in PermissionPolicyUser, and
         b. an HCM10ConnectionString entry in the (normally read-only) ConfigurationManager collection.

.PARAMETER SandboxConnectionString
    Where the report constructors' XPObjectSpaceProvider points.

    *** DO NOT POINT THIS AT HRMS_RCF. ***
    XPObjectSpaceProvider defaults to AutoCreateOption.DatabaseAndSchema, so XPO will happily ALTER
    the schema of whatever database it is handed. The legacy database is read-only by policy.
    The default below is a throwaway sandbox database that this script creates and seeds itself; the
    report constructors only need *a* PermissionPolicyUser row to exist, not the real one.

.EXAMPLE
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Export-LegacyRepx.ps1 -OutDir C:\temp\repx
#>
[CmdletBinding()]
param(
    [string]$LegacyBin = "C:\GBS\HCM\HRMS_Production\HCM10.Module\bin\Debug",
    [string]$OutDir,
    # NOTE the XpoProvider= prefix. XPO v17.2 does not sniff providers from a plain ADO.NET connection
    # string; without it every constructor dies with CannotFindAppropriateConnectionProviderException.
    # It also does not understand TrustServerCertificate, so that keyword must not appear here.
    [string]$SandboxConnectionString = "XpoProvider=MSSqlServer;Data Source=localhost\SQLEXPRESS;Initial Catalog=HCM10_RepxExportSandbox;Integrated Security=SSPI;",
    [string]$SandboxUserName = "RepxExportUser",
    [string[]]$OnlyTypes,
    # -File cannot pass arrays, so a newline-delimited file of full type names works too.
    [string]$OnlyTypesFile
)
if ($OnlyTypesFile) { $OnlyTypes = @(Get-Content $OnlyTypesFile | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }

$ErrorActionPreference = "Continue"

if (-not $OutDir) { $OutDir = Join-Path $PSScriptRoot "out\repx" }
New-Item -ItemType Directory -Force $OutDir | Out-Null

if ($PSVersionTable.PSVersion.Major -ge 6) {
    throw "Run this under Windows PowerShell 5.1. DevExpress v17.2 is .NET Framework only."
}

# --------------------------------------------------------------------------------------------------
# 1. Resolve HCM10.Module's 203 dependencies out of the legacy bin folder.
# --------------------------------------------------------------------------------------------------
$script:ResolveBin = $LegacyBin
$onResolve = [System.ResolveEventHandler] {
    param($sender, $e)
    $name = (New-Object System.Reflection.AssemblyName($e.Name)).Name
    if ($name.EndsWith(".resources")) { return $null }   # satellite probes recurse forever otherwise
    $p = Join-Path "C:\GBS\HCM\HRMS_Production\HCM10.Module\bin\Debug" "$name.dll"
    if (Test-Path $p) { return [System.Reflection.Assembly]::LoadFrom($p) }
    return $null
}
[System.AppDomain]::CurrentDomain.add_AssemblyResolve($onResolve)

# --------------------------------------------------------------------------------------------------
# 2. Create + seed the sandbox database the report constructors will connect to.
#    Only PermissionPolicyUser matters: rptPayslipEn's ctor does
#        object_Space.GetObjectsQuery<PermissionPolicyUser>().Where(x => x.UserName == CurrentUserName)
#    and then dereferences the result. XPO creates the table on first access; we insert one row.
# --------------------------------------------------------------------------------------------------
function Invoke-Sql([string]$connectionString, [string]$sql) {
    $c = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $c.Open()
    try { $cmd = $c.CreateCommand(); $cmd.CommandText = $sql; return $cmd.ExecuteNonQuery() }
    finally { $c.Close() }
}

# Strip the XPO-only prefix before handing the string to SqlClient.
$adoConnectionString = ($SandboxConnectionString -replace '(?i)XpoProvider=[^;]*;', '')
$b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($adoConnectionString)
$sandboxDb = [string]$b["Initial Catalog"]
if ($sandboxDb -ieq "HRMS_RCF") {
    throw "Refusing to run: the sandbox connection string points at the read-only legacy database HRMS_RCF."
}
$b["Initial Catalog"] = "master"
Invoke-Sql $b.ConnectionString "if db_id('$sandboxDb') is null create database [$sandboxDb];" | Out-Null
Write-Host "SANDBOX DB: $sandboxDb"

# --------------------------------------------------------------------------------------------------
# 3. Inject HCM10ConnectionString into ConfigurationManager.
#    powershell.exe.config is not ours to edit, and the ConnectionStrings collection is read-only at
#    runtime. Two private flags guard it (one on ConfigurationElementCollection, one on the
#    ConfigurationElement base); clearing both makes Add() legal for the life of this process only.
# --------------------------------------------------------------------------------------------------
Add-Type -AssemblyName System.Configuration
$connStrings = [System.Configuration.ConfigurationManager]::ConnectionStrings
[System.Configuration.ConfigurationElementCollection].GetField("bReadOnly", "NonPublic,Instance").SetValue($connStrings, $false)
[System.Configuration.ConfigurationElement].GetField("_bReadOnly", "NonPublic,Instance").SetValue($connStrings, $false)
if ($connStrings["HCM10ConnectionString"] -eq $null) {
    $connStrings.Add((New-Object System.Configuration.ConnectionStringSettings(
        "HCM10ConnectionString", $SandboxConnectionString, "System.Data.SqlClient")))
}
Write-Host "HCM10ConnectionString: $($connStrings['HCM10ConnectionString'].ConnectionString)"

# --------------------------------------------------------------------------------------------------
# 4. Install a SecuritySystem instance.
#    DevExpress.ExpressApp.SecuritySystem.CurrentUserName just forwards to Instance.UserName and
#    returns "" when Instance is null (which is the case outside a hosted XAF application).
#    SecuritySystem.SetInstance(ISecurityStrategyBase) is public, so a 15-line stub is enough -- we
#    are exporting layouts, not enforcing permissions.
# --------------------------------------------------------------------------------------------------
$expressAppDll = Join-Path $LegacyBin "DevExpress.ExpressApp.v17.2.dll"
Add-Type -ReferencedAssemblies $expressAppDll, "System.Data", "System.Xml" -TypeDefinition @'
using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;

/// <summary>
/// Minimal ISecurityStrategyBase so legacy report constructors that read
/// SecuritySystem.CurrentUserName see a real user name instead of "".
/// Layout export only -- this grants nothing and is never loaded by the application.
/// </summary>
public class RepxExportSecurityStub : ISecurityStrategyBase
{
    private readonly string userName;
    public RepxExportSecurityStub(string userName) { this.userName = userName; }
    public bool IsAuthenticated { get { return true; } }
    public Type UserType { get { return typeof(object); } }
    public object User { get { return null; } }
    public string UserName { get { return userName; } }
    public object UserId { get { return userName; } }
    public object LogonParameters { get { return null; } }
    public bool NeedLogonParameters { get { return false; } }
    public bool IsLogoffEnabled { get { return false; } }
    public IObjectSpace LogonObjectSpace { get { return null; } }
    public void Logon(IObjectSpace objectSpace) { }
    public void Logoff() { }
    public void ClearSecuredLogonParameters() { }
    public void ReloadPermissions() { }
    public Type GetModuleType() { return null; }
    public IList<Type> GetBusinessClasses() { return new List<Type>(); }
}
'@

$expressApp = [System.Reflection.Assembly]::LoadFrom($expressAppDll)
$securitySystem = $expressApp.GetType("DevExpress.ExpressApp.SecuritySystem")
$stub = (New-Object RepxExportSecurityStub($SandboxUserName)).psobject.BaseObject   # unwrap: PS would otherwise pass a PSObject and SetInstance rejects it
$securitySystem.GetMethod("SetInstance").Invoke($null, @($stub))
Write-Host "SecuritySystem.CurrentUserName: '$($securitySystem.GetProperty('CurrentUserName').GetValue($null))'"

# --------------------------------------------------------------------------------------------------
# 5. Enumerate report types and export.
# --------------------------------------------------------------------------------------------------
$module = [System.Reflection.Assembly]::LoadFrom((Join-Path $LegacyBin "HCM10.Module.dll"))
$types = $null
try { $types = $module.GetTypes() }
catch [System.Reflection.ReflectionTypeLoadException] {
    # Expected: a handful of types reference designer-only assemblies. Partial load is fine.
    $types = $_.Exception.Types | Where-Object { $_ -ne $null }
}

$xtra = [System.Reflection.Assembly]::LoadFrom((Join-Path $LegacyBin "DevExpress.XtraReports.v17.2.dll"))
$xtraReport = $xtra.GetType("DevExpress.XtraReports.UI.XtraReport")

$reports = $types | Where-Object {
    $_ -ne $null -and $_.IsClass -and -not $_.IsAbstract -and
    $xtraReport.IsAssignableFrom($_) -and $_.GetConstructor([Type]::EmptyTypes) -ne $null
}
if ($OnlyTypes) { $reports = $reports | Where-Object { $OnlyTypes -contains $_.FullName } }
Write-Host "CANDIDATE REPORT TYPES: $(@($reports).Count)"

$rows = @()
function Export-One($ty) {
    $rec = [ordered]@{ Type = $ty.FullName; Status = ""; Error = ""; Bytes = 0 }
    try {
        $inst = [System.Activator]::CreateInstance($ty)
        $f = Join-Path $OutDir ("{0}.repx" -f $ty.FullName)
        $inst.SaveLayoutToXml($f)
        $rec.Status = "OK"; $rec.Bytes = (Get-Item $f).Length
        try { $inst.Dispose() } catch { }
    } catch {
        $ex = $_.Exception
        while ($ex.InnerException) { $ex = $ex.InnerException }
        $rec.Status = "FAIL"; $rec.Error = "$($ex.GetType().Name): $($ex.Message)"
    }
    return New-Object PSObject -Property $rec
}

# --------------------------------------------------------------------------------------------------
# 5b. Prepare the sandbox schema + seed row.
#
#     The report constructors build `new XPObjectSpaceProvider(connectionString, null)`, an overload
#     that assumes the schema already exists -- it will NOT create tables, it just fails with
#     "Invalid object name 'dbo.PermissionPolicyUser'". So we create the schema ourselves first,
#     through our own data layer with AutoCreateOption.DatabaseAndSchema.
#
#     Only the four classes the constructors actually touch are needed:
#       PermissionPolicyUser  -- the user lookup that NREs
#       Employee              -- .Where(x => x.LoginID == user.UserName).Count()
#       ManagePayrollSettings -- SysService.GetPayrollSettings(...).PayslipSignature
#       SysTenantConfig       -- read by GetPayrollSettings when no settings row exists
#     UpdateSchema pulls in referenced classes transitively, so this is a few dozen tables, not the
#     whole 900-table HCM10 schema.
#
#     The seed row is written through XPO rather than raw INSERT so that XPObjectType and every
#     mapped column are populated exactly the way XPO expects to read them back.
# --------------------------------------------------------------------------------------------------
$xpoAsm  = [System.Reflection.Assembly]::LoadFrom((Join-Path $LegacyBin "DevExpress.Xpo.v17.2.dll"))
$dataAsm = [System.Reflection.Assembly]::LoadFrom((Join-Path $LegacyBin "DevExpress.Data.v17.2.dll"))
$xpoDefault      = $xpoAsm.GetType("DevExpress.Xpo.XpoDefault")
$sessionType     = $xpoAsm.GetType("DevExpress.Xpo.Session")
$simpleDataLayer = $xpoAsm.GetType("DevExpress.Xpo.SimpleDataLayer")
$autoCreate      = $dataAsm.GetType("DevExpress.Xpo.DB.AutoCreateOption")          # lives in DevExpress.Data, not .Xpo
$binaryOperator  = $dataAsm.GetType("DevExpress.Data.Filtering.BinaryOperator")

$store = $xpoDefault.GetMethod("GetConnectionProvider", [Type[]]@([string], $autoCreate)).Invoke(
    $null, @([object]$SandboxConnectionString, [Enum]::Parse($autoCreate, "DatabaseAndSchema")))
$dataLayer = [System.Activator]::CreateInstance($simpleDataLayer, @($store))
$session = [System.Activator]::CreateInstance($sessionType, @($dataLayer))

$schemaTypeNames = @(
    "DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyUser",
    "HCM10.Module.BusinessObjects.PersonnelAdministration.Employee",
    "HCM10.Module.BusinessObjects.SysSettings.ManagePayrollSettings",
    "HCM10.Module.BusinessObjects.SysSecurity.SysTenantConfig",
    # rptPayrollSheet_Custom028's ctor needs a PayrollArea row -- see the seeding note below.
    "HCM10.Module.BusinessObjects.PayrollManagement.PayrollArea"
)
$permissionPolicyUserType = $null
$schemaTypes = @()
foreach ($n in $schemaTypeNames) {
    $t = $module.GetType($n)
    if ($t -eq $null) {
        foreach ($asm in [System.AppDomain]::CurrentDomain.GetAssemblies()) { $t = $asm.GetType($n); if ($t) { break } }
    }
    if ($t) { $schemaTypes += $t } else { Write-Host "SCHEMA WARNING: type not found $n" }
    if ($n -like "*PermissionPolicyUser") { $permissionPolicyUserType = $t }
}
$session.UpdateSchema([Type[]]$schemaTypes)
Write-Host "SANDBOX SCHEMA: created/verified for $($schemaTypes.Count) root classes"

$existing = $session.FindObject($permissionPolicyUserType,
    [System.Activator]::CreateInstance($binaryOperator, @([object]"UserName", [object]$SandboxUserName)))
if ($existing -eq $null) {
    $user = [System.Activator]::CreateInstance($permissionPolicyUserType, @($session))
    $user.UserName = $SandboxUserName
    $user.IsActive = $true
    $user.Save()
    $session.Save($user)
    Write-Host "SANDBOX SEED: created PermissionPolicyUser '$SandboxUserName'"
} else {
    Write-Host "SANDBOX SEED: PermissionPolicyUser '$SandboxUserName' already present"
}

# rptPayrollSheet_Custom028's constructor contains a live legacy bug: when no PayrollArea row exists it
# falls into the else-branch and calls DateTime.DaysInMonth(year, YEAR) -- passing the year where the
# month belongs -- which throws "Month must be between one and twelve". It never fires in production
# because a PayrollArea always exists, so seeding one row is both the smallest fix and the faithful one.
# Seeded with raw SQL rather than through XPO: PayrollArea carries a chain of XAF RuleRequiredField
# validations (Name, PayrollPeriod, Currency, ...) that only make sense for a real payroll area. The
# report constructor reads exactly one column, CutOffDay, so a bare row is the honest minimum.
$session.Disconnect()
try {
    Invoke-Sql $adoConnectionString @"
if object_id('PayrollArea') is not null and not exists (select 1 from PayrollArea)
    insert into PayrollArea (Code, Name, CutOffDay, OptimisticLockField)
    values ('SANDBOX', 'SANDBOX', 25, 0);
"@ | Out-Null
    Write-Host "SANDBOX SEED: PayrollArea (CutOffDay=25) present"
} catch {
    Write-Host "SANDBOX SEED WARNING (PayrollArea): $($_.Exception.Message)"
}

# --------------------------------------------------------------------------------------------------
# 5c. Register HCM10's enums with the criteria parser.
#     Report FilterStrings serialise enum literals as ##Enum#Namespace.Type,Member#. DevExpress'
#     CriteriaLexer refuses to materialise those unless the enum type has been registered, which the
#     XAF application normally does at startup. Without this, srptMonthAttendanceEn (and any other
#     report whose designer filter compares an enum column) dies in InitializeComponent with
#     CriteriaParserException.
# --------------------------------------------------------------------------------------------------
$enumHelper = $dataAsm.GetType("DevExpress.Data.Filtering.EnumProcessingHelper")
$registerEnum = $enumHelper.GetMethod("RegisterEnum", [Type[]]@([Type]))
$enumCount = 0
foreach ($t in $types) {
    if ($t -ne $null -and $t.IsEnum) {
        try { $registerEnum.Invoke($null, @([object]$t)) | Out-Null; $enumCount++ } catch { }
    }
}
Write-Host "CRITERIA ENUMS REGISTERED: $enumCount"

$ok = 0; $fail = 0
foreach ($ty in $reports) {
    $r = Export-One $ty
    $rows += $r
    if ($r.Status -eq "OK") { $ok++ } else { $fail++; Write-Host "FAIL $($r.Type) -> $($r.Error)" }
}

$csv = Join-Path (Split-Path $OutDir -Parent) "export-results.csv"
$rows | Export-Csv -NoTypeInformation -Encoding utf8 $csv
Write-Host "OK=$ok FAIL=$fail  ->  $OutDir"
Write-Host "results: $csv"

