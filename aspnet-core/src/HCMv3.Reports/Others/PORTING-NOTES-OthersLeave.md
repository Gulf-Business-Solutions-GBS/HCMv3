# Porting notes -- batch `OthersLeave`

Legacy source: `C:\GBS\HCM\HRMS_Production\HCM10.Module\Reports\Others\`
Target:        `C:\GBS\HCM\HCMv3\aspnet-core\src\HCMv3.Reports\Others\`

Assigned: 7 reports (base variants only; all `*_CustomNNN` files ignored).
Delivered: **5 ported, 2 BLOCKED.**

| Report | Outcome |
|---|---|
| rptAnnualLeaveSheet | ported |
| rptSickLeaveForm (+ `srptSickLeaveForm` subreport) | ported |
| rptSickLeaveMedicalForm | ported |
| rptMonthlyLeaveAccrual | ported |
| rptMonthlyIndemnityAccrual | ported |
| rptAnnualLeaveRequest | **BLOCKED** |
| rptLeaveForm | **BLOCKED** |

`srptSickLeaveForm` was not on the assigned list but is a hard dependency of
rptSickLeaveForm (`xrSubreport1.ReportSource`), so it was ported with it. It reads only
Employees / EmployeeAbsences / AbsenceTypes and is not a standalone report -- it has no
`ReportName` and must NOT be added to the registry or the catalogue.

---

## csproj entries needed

Three image files were extracted from the legacy `.resx` files. Per the shared brief a
`System.Drawing.Bitmap` `.resx` entry breaks the multi-targeted build, so the bytes ship as
embedded files and are attached in each report constructor. The bytes are byte-identical to
the legacy resources (the legacy entries used the `bytearray.base64` mimetype, i.e. the raw
file, not a serialised object) and are deliberately NOT recompressed or resized --
`rptSickLeaveForm`'s is a scan of an official Ministry of Health paper form whose field
positions the labels are aligned against.

Note the existing `Others\` entries in the csproj are explicit filenames, not a wildcard, so
these need adding:

```xml
<EmbeddedResource Include="Others\rptSickLeaveForm.xrPictureBox1.png" />
<EmbeddedResource Include="Others\rptSickLeaveMedicalForm.xrPictureBox2.png" />
<EmbeddedResource Include="Others\rptSickLeaveMedicalForm.xrPictureBox3.jpg" />
```

Until these lines are added the project still builds and the reports still render -- the
loaders return `null` on a missing manifest resource and never throw -- but the form scan and
the two graphics print blank. **This is the one item in this batch that is not self-contained.**

## Registry entries

```csharp
[rptAnnualLeaveSheet.ReportName]        = () => new rptAnnualLeaveSheet(),
[rptSickLeaveForm.ReportName]           = () => new rptSickLeaveForm(),
[rptSickLeaveMedicalForm.ReportName]    = () => new rptSickLeaveMedicalForm(),
[rptMonthlyLeaveAccrual.ReportName]     = () => new rptMonthlyLeaveAccrual(),
[rptMonthlyIndemnityAccrual.ReportName] = () => new rptMonthlyIndemnityAccrual(),
```

(`srptSickLeaveForm` is deliberately absent -- subreport, instantiated by its parent.)

## Catalog rows

```csharp
("PR???", "AnnualLeaveSheet",        "Annual Leave Sheet",        "كشف الإجازة السنوية",           ReportCategory.Default),
("PR???", "SickLeaveForm",           "Sick Leave Form",           "نموذج الإجازة المرضية",         ReportCategory.Default),
("PR???", "SickLeaveMedicalForm",    "Sick Leave Medical Form",   "النموذج الطبي للإجازة المرضية", ReportCategory.Default),
("PR???", "MonthlyLeaveAccrual",     "Monthly Leave Accrual",     "استحقاق الإجازة الشهري",        ReportCategory.Default),
("PR???", "MonthlyIndemnityAccrual", "Monthly Indemnity Accrual", "استحقاق المكافأة الشهري",       ReportCategory.Default),
```

## Tenant audit

Audited by alias, not by count: every table alias introduced by a `FROM` / `JOIN` /
`OUTER APPLY` must appear in its own `<alias>.TenantId = @TenantId` predicate. SQL line
comments were stripped before auditing so commented-out text is not credited.

**70 table references, 70 tenant predicates, 0 misses.**

| SQL constant | Table refs | `TenantId = @TenantId` predicates |
|---|---|---|
| `AnnualLeaveSheetQuery` | 40 | 40 |
| `SickLeaveFormQuery` | 7 | 7 |
| `SickLeaveFormAbsencesQuery` | 3 | 3 |
| `SickLeaveMedicalFormQuery` | 3 | 3 |
| `MonthlyLeaveAccrualQuery` | 10 | 10 |
| `MonthlyIndemnityAccrualQuery` | 7 | 7 |

**Lookup tables with no `TenantId` column: none.** Every table touched by this batch is
tenant-owned and soft-deleted, so every reference also carries `IsDeleted = 0`. That includes
`AbpOrganizationUnits`, where `TenantId` is nullable -- the predicate therefore also excludes
host-tenant org units, which is correct for these reports.

Runtime wiring was verified by constructing each report and inspecting its queries: all six
have `PrmTenantId` present (defaulting to `0`, i.e. fails closed) and a `TenantId`
`QueryParameter` auto-wired by `EnsureHcmParameters()`:

```
rptAnnualLeaveSheet        LeaveAccrualQuery   params=[PrmEmpCode,PrmAsOnDate,TenantId]
rptSickLeaveForm           SickLeaveFormQuery  params=[PrmEmpCode,TenantId]   subreport -> srptSickLeaveForm
srptSickLeaveForm          Query               params=[PrmEmpCode,TenantId]
rptSickLeaveMedicalForm    SickLeaveFormQuery  params=[PrmEmpCode,PrmFormDate,TenantId]
rptMonthlyLeaveAccrual     LeaveAccrualQuery   params=[prmEmpCode,TenantId]
rptMonthlyIndemnityAccrual LeaveAccrualQuery   params=[prmEmpCode,TenantId]
```

## BLOCKED

Both blockers are the known missing self-service module. Confirmed against the HCMv3 EF Core
model: `HCMv3DbContextModelSnapshot.cs` has **no** `LeaveRequests` / `AbsenceRequests` /
`ReturnFromLeaveRequests` table, and `HCMv3.Core` declares no such entity class. The related
workflow tables (`WorkflowRequests`, `RequestLogs`, `EmployeeWorkflowRoles`, `WorkflowRoles`,
`EmployeeSignatures`) *do* exist -- but they are the approval-trail side, not the leave request
itself, and neither report can be driven without the request row.

Nothing was faked and no substitute table was invented.

### rptAnnualLeaveRequest

Missing legacy type: **`HCM10.Module.BusinessObjects.Workflow.LeaveRequest`** (table
`LeaveRequest`).

The first half of the query is portable -- it is the same leave-balance ledger as
rptAnnualLeaveSheet (TimeTransfer / TimeRegister / EmployeeAbsence / EmployeeAbsAdjustment).
The second half is not. The outer `SELECT` joins

```sql
LEFT JOIN (select Lr.*, E.FullNameEn from LeaveRequest LR ...) LR On LR.AbsenceType = AbsenceType.OID
LEFT JOIN WorkflowRequest WR on WR.OID = LR.OID
```

and then five correlated signature subqueries, every one of which is keyed on
`R.WorkflowRequest = LR.OID` -- the leave-request id. The printed columns
`LeaveBalance` (`LR.QuotaUsed`), `Address` (`LR.Address`), `EmpHandOver`
(`LR.FullNameEn`, the delegate), `EmpManagerSign`, `DeptHeadSign`, `ExecutiveSign` and
`MgmtSign` all come off the leave request or its approval trail. The signature block is the
substance of this form -- it is an approval document -- so there is no meaningful subset to
port.

Also required and absent: `HolidayPeriod` is present in HCMv3 (`HolidayPeriods`), so the
`HoldayCount` column would have been portable; it is blocked only by association.

### rptLeaveForm

Missing legacy type: **`HCM10.Module.BusinessObjects.Workflow.LeaveRequest`** (table
`LeaveRequest`), reached through
`HCM10.Module.BusinessObjects.Workflow.WorkflowRequest`.

`LeaveRequest` is the **driving table** and supplies the **only** filter:

```sql
FROM LeaveRequest
LEFT JOIN WorkflowRequest ON LeaveRequest.OID = WorkflowRequest.OID
...
WHERE LeaveRequest.OID = @PrmLeaveRequestOID
```

The code-behind matches -- `rptLeaveForm(object[] args)` casts a `WorkflowRequest` to
`LeaveRequest` and sets `PrmLeaveRequestOID` from `LRObj.Oid`.

Worth recording explicitly, because it is the tempting near-miss in this batch: this report
*does* read a table that exists in HCMv3 --
`EmployeeAbsenceQuota` (`EmployeeAbsenceQuotas`), for the `QuotaBalance` column -- and it
prints leave dates and type. It is still blocked, because the report is **identified by a
leave-request id**. There is no employee+date key to substitute: the request supplies the
start/end dates, the absence type and the request number that the form prints. Rewriting it
around `EmployeeAbsences` would be inventing a different report, not porting this one.

### Unblocking

Both become straightforward ports once a `LeaveRequest` entity exists in HCMv3 with (at
minimum) `EmployeeId`, `AbsenceTypeId`, `StartDate`, `StartTime`, `EndDate`, `EndTime`,
`DelegatedToId`, `Address`, `QuotaUsed`, and a link to `WorkflowRequests` for the approval
trail. Re-porting is then mechanical against `OthersLeaveSql.cs`, which already contains the
balance-ledger half of rptAnnualLeaveRequest as `AnnualLeaveSheetQuery`.

## Defects carried over

Reproduced faithfully rather than fixed:

1. **`rptSickLeaveForm` / `rptSickLeaveMedicalForm`: `CalcTotalDays` is dead.** The calculated
   field is `DataMember = "LeaveFormQuery"`, but neither report has a query by that name (they
   have `SickLeaveFormQuery`), and its expression references `[Parameters].[PrmStartDate]` /
   `[PrmEndDate]`, which neither report declares. No control binds it. Kept as-is because it is
   inert; correct behaviour would be to delete it.
2. **`rptSickLeaveForm` / `rptSickLeaveMedicalForm`: unused `LeaveFormReportDS` field.** A
   `SqlDataSource` field declared in the legacy designer, never instantiated or added to
   `ComponentStorage`. Kept verbatim; it produces the only two build warnings this batch emits
   (`CS0169`). Correct behaviour would be to delete it.
3. **`rptMonthlyIndemnityAccrual`: no opening-balance correction.** The legacy query has the
   `HasLastRegister` / `TransferCount` first-period correction (which its sibling
   rptMonthlyLeaveAccrual applies) present but **commented out**, and hard-codes
   `QuotaAdjusted = 0`. Both are carried over exactly. Whether indemnity should get the same
   first-period correction as leave is a product question, not a port question.
4. **`rptAnnualLeaveSheet` "MONTHLY ACCRUAL" leg subtracts the whole opening balance.** The
   accrual leg computes `SUM(AnnualLeaveAccrualPerDay) - ISNULL(Opening.TransferCount, 0)`
   where `Opening` is the employee's *total* opening transfer, not the portion inside the
   accrual window. Carried over unchanged.

## Layout deviations

The layouts are otherwise byte-for-byte the legacy ones; the changes below are the v23.2 API
breaks and the two schema gaps.

**Mechanical v23.2 breaks applied to all six designer files** (per the shared brief):
`System.Drawing.Printing.PaperKind` -> `DevExpress.Drawing.Printing.DXPaperKind`;
`System.Drawing.Printing.PrintEventHandler` -> `DevExpress.XtraReports.UI.BeforePrintEventHandler`
with `CancelEventArgs` at report level; `ResultSchemaSerializable` dropped;
`ComponentResourceManager` dropped; `Version = "17.2"` -> `"23.2"`. No `SqlDataSource`
`BeginInit`/`EndInit` calls existed to remove (only the report's own and the `XRTable`s', which
are kept). No `Scripts.OnBeforePrint` in this batch. No `Margins` exceeded `PageHeight`.

Genuine deviations:

1. **Company logo prints blank (FEATURE LOST).** Four of the five layouts have an
   `XRPictureBox` bound `ExpressionBinding("BeforePrint", "Image", "[Companylogo]")`.
   `AbpOrganizationUnits` has no `Logo` column and HCMv3 has no Company entity carrying one, so
   `Companylogo` is projected as a typed `NULL`. The picture box and its binding are kept so
   the layout is unchanged and the feature is restorable. Same gap `PayslipSql` and
   `SalaryCertificateSql` already record.
2. **`CompanyEn` / `CompanyAr` are the employee's own org unit, and are the same string.**
   Legacy walked seven flat `OrgLevel1..7` FK columns looking for the first whose
   `OrganizationUnitType` was named `'Company'`. HCMv3 has a single
   `EmployeeOrgAssignments.OrganizationUnitId` into an ABP materialised-path tree with no
   unit-type concept, so "the ancestor whose type is Company" is not expressible; the
   employee's own unit's `DisplayName` is used. `AbpOrganizationUnits` has one name column, not
   a `NameEn`/`NameAr` pair, so the Arabic and English company headers now print the same
   text. **SEMANTIC DEVIATION -- the same one `PayslipSql.PayslipOrgV2Query` and
   `SalaryCertificateSql` carry; it needs the same single product decision, not three.**
3. **`FilterString` removed from `rptSickLeaveForm` and `srptSickLeaveForm`.** Both narrowed to
   one employee client-side (`"[Code] In (?PrmEmpCode)"` / `"(?empCode)"`) over a raw
   `SqlDataSource`, i.e. every tenant's rows were fetched to the report engine before being
   discarded. Replaced by `emp.Code = @PrmEmpCode` in SQL, which required adding the
   `PrmEmpCode` `QueryParameter` neither report previously had. Same rewrite
   `SalaryCertificateSql` note 3 records. Rendered output is unchanged.
4. **One-row-per-employee joins pinned with `OUTER APPLY`.** `rptSickLeaveForm` legacy-joined
   `EmployeeOrgAssignment` and `EmployeeSponsor` with a plain `LEFT JOIN` on `EmployeeId`, so
   an employee with N assignments and M sponsor records printed N*M copies of a single-page
   government form. `rptAnnualLeaveSheet`'s date-bounded org-assignment join could likewise fan
   out the ledger. Both are now `OUTER APPLY (SELECT TOP 1 ... ORDER BY EndDate DESC)`. This
   changes the rendered page count where the legacy data fanned out -- deliberately, and in the
   direction the form requires.
5. **`rptSickLeaveForm` / `rptSickLeaveMedicalForm` constructors taking `object[] args`
   dropped.** Those overloads existed only to unpack a `LeaveRequest`; neither report's query
   ever read one. rptSickLeaveForm's copied `Employee.Code` and `DateTime.Now`;
   rptSickLeaveMedicalForm's body was entirely commented out apart from `PrmFormDate`. Both are
   replaced by a typed `(string employeeCode, DateTime formDate)` constructor -- which is
   exactly what the legacy "unified leave request" overload already did. **The reports
   themselves are fully ported; only the leave-request-shaped call sugar is gone.**
6. **Page-footer "printed by" label.** `rptAnnualLeaveSheet`, `rptMonthlyLeaveAccrual` and
   `rptMonthlyIndemnityAccrual` opened an `XPObjectSpaceProvider` in their constructor to turn
   `SecuritySystem.CurrentUserName` into the employee's `FullNameEn`. A DevExpress-constructed
   report has no DI container, so the name now arrives via
   `HcmReportBase.ApplyLoggedUserName`. Precedence is preserved (caller-supplied name if there
   is one); the fallback is an empty label rather than the XAF login name, which this layer
   cannot see.
7. **Designer surface: picture boxes look empty in Visual Studio.** Because the three images
   are attached in the constructor rather than inside `InitializeComponent`. Rendered output is
   unaffected.

## Verification performed

1. **Build.** `HCMv3.Reports.csproj` currently reports 58 errors, **all of them in other
   batches' in-progress files** (`Payroll\rptLeaveSettlement*`, `Payroll\rptPayslip_w*`,
   `Others\rptWarningLetter`, `Others\rptEmployeeProfile`, ... -- `.Designer.cs` files whose
   `.cs` partial has not landed yet, hence `CS0115`). None is in a file this batch created.
   To prove that rather than assume it, the twelve files of this batch plus `HcmReportBase.cs`
   were compiled in an isolated scratch project against DevExpress 23.2.13:
   **Build succeeded, 0 errors, 2 warnings** (the `CS0169` dead-field pair in "Defects carried
   over" #2).
2. **Silent-failure greps** over this batch's `*.Designer.cs`: zero hits for
   `.Text = "["` mail-merge, zero for `!format`, zero `XRBinding(` (all data binding in these
   layouts is via `ExpressionBindings`, 62 of them across the six files, which is the form the
   brief prescribes).
3. **Construction smoke test.** All six reports were instantiated and their data sources
   inspected -- see the table under "Tenant audit". No `CustomSqlQuery` rejected its SQL (none
   starts with `DECLARE`/`WITH`/`BEGIN`, none contains `REPLACE()`), the subreport resolved
   (`rptSickLeaveForm -> srptSickLeaveForm`), and every `ConnectionName` is
   `HCMv3ConnectionString` via `HcmReportBase.DesignTimeConnectionName`.

**NOT done: no report in this batch has been rendered against a database.** There was no
HCMv3 database available to this batch, so column-name correctness rests on the EF Core model
snapshot rather than on execution. Every column used was read from
`HCMv3DbContextModelSnapshot.cs`; none was guessed. The queries most worth exercising first
are the two `PIVOT` accrual queries -- the pivot source deliberately projects exactly
`(Number, WageType, SalaryRegisterId)` so that `PIVOT`'s implicit `GROUP BY` is over
`SalaryRegisterId` alone, and adding a column to that `SELECT` list would silently change the
grouping.
