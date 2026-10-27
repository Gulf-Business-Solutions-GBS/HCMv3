# HCMv3.ReportMigrator

Tooling for migrating the 481 legacy DevExpress XtraReports in `HCM10.Module.dll` to Stimulsoft
`.mrt` layouts running against the HCMv3 schema.

Follows the `aspnet-core/tools/` precedent set by `HCMv3.LegacyMigrator`,
`HCMv3.TimeEngineRunner` and `HCMv3.PayrollEngineRunner`.

## The pipeline

```
  HCM10.Module.dll  --(1)-->  *.repx  --(2)-->  *.mrt  --(3)-->  *.HCMv3.mrt  --(4)-->  *.pdf
```

| # | Component | What it does |
|---|-----------|--------------|
| 1 | `Export-LegacyRepx.ps1` | Instantiates every `XtraReport` subclass and calls `SaveLayoutToXml`. **Windows PowerShell 5.1 only** -- DevExpress v17.2 is .NET Framework. |
| 2 | `HCMv3.RepxToMrt/` | Batch-converts `.repx` -> `.mrt` using Stimulsoft's own importer (vendored + patched). No DevExpress dependency. |
| 3 | `reports/<Report>/Build-*.mrt.ps1` | Per-report: injects HCMv3-rewritten SQL, rebinds the layout, restores dropped formats, parameterises code-behind. |
| 4 | `HCMv3.MrtRender/` | Renders a `.mrt` against a live HCMv3 database to PDF + per-page PNG. **This is the acceptance gate.** |

Run the whole corpus:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Export-LegacyRepx.ps1 -OutDir .\out\repx-all
dotnet run --project .\HCMv3.RepxToMrt -c Release -- .\out\repx-all .\out\mrt-all
```

Take one report end to end:

```powershell
.\reports\PayslipEn\Build-PayslipMrt.ps1
.\reports\PayslipEn\Test-PayslipSql.ps1
dotnet run --project .\HCMv3.MrtRender -c Release -- `
    .\reports\PayslipEn\rptPayslipEn.HCMv3.mrt .\out\payslip.pdf `
    TenantId=1 PrmEmpCode=3400 empcode=3400 PrmMonth=6 PrmYear=2024 `
    PrmPayrollRunType=0 RunType=0 PrmEncashment=0 PrmEmployeeStatus=-1 `
    ForPeriod=2024-06-30 InPeriod=2024-06-30 LoggedUserName=someone@gbskw.com
```

## Current status

| Stage | Result |
|-------|--------|
| Export | **481 of 482** report types export. The one failure is `rptSubordPayslipAr`, whose constructor dereferences `FontCollection1.Families[0]` from a private font collection that is empty outside the deployed web app. Unrelated to security or data; every other report that does the same thing has the line commented out. |
| Convert | **481 of 481**, zero failures, 7,936 warnings. See `IMPORTER-GAPS.md`. |
| End to end | `rptPayslipEn` -- see `reports/PayslipEn/PORTING-NOTES.md`. |

## Packages

No new NuGet package enters the solution. Both tools reference
`Stimulsoft.Reports.Engine.NetCore` **2026.2.4**, the version already referenced by
`HCMv3.Application/HCMv3.Application.csproj`.

Rendered output carries a Stimulsoft **trial watermark**. Judge fidelity and Arabic shaping, not
polish.

## Safety rules baked into the tooling

* `HRMS_PRODUCTION` and `HRMS_RCF` are **read-only**. `Export-LegacyRepx.ps1` refuses to run if its
  sandbox connection string names `HRMS_RCF`, because `XPObjectSpaceProvider` defaults to
  `AutoCreateOption.DatabaseAndSchema` and will ALTER whatever database it is handed.
  It creates and seeds a throwaway `HCM10_RepxExportSandbox` instead.
* `appsettings.json` is never read or written. `HCMv3.MrtRender` takes its connection string from
  the `ConnectionStrings__Default` environment variable.

## Why the export harness is as complicated as it is

38 of the 482 reports -- the whole payslip family -- run real application code in their
**constructor**: `SecuritySystem.CurrentUserName`, an `XPObjectSpaceProvider` against
`ConfigurationManager.ConnectionStrings["HCM10ConnectionString"]`, and a `PermissionPolicyUser`
lookup. Outside a hosted XAF application all three are absent and the constructor NREs.
Skipping the constructor is not an option -- `FormatterServices.GetUninitializedObject` leaves the
`XtraReport` base state uninitialised and `SaveLayoutToXml` emits an empty layout.

The harness therefore supplies, in this order:

1. an `AssemblyResolve` hook onto the legacy `bin\Debug` (203 DevExpress assemblies);
2. a sandbox database, its schema created through XPO and one `PermissionPolicyUser` row seeded --
   the constructors' `XPObjectSpaceProvider(connectionString, null)` overload assumes the schema
   already exists and will not create it;
3. an `HCM10ConnectionString` entry injected into the runtime-read-only
   `ConfigurationManager.ConnectionStrings` collection (two private flags cleared by reflection),
   prefixed `XpoProvider=MSSqlServer;` because XPO v17.2 does not sniff providers;
4. a 15-line `ISecurityStrategyBase` stub installed via the public `SecuritySystem.SetInstance`;
5. `EnumProcessingHelper.RegisterEnum` for all 136 HCM10 enums, which the XAF application normally
   does at startup and without which any designer `FilterString` comparing an enum column throws
   `CriteriaParserException`;
6. a seeded `PayrollArea` row, because `rptPayrollSheet_Custom028`'s constructor contains a live
   legacy bug (`DateTime.DaysInMonth(year, YEAR)`) that only fires when no payroll area exists.
