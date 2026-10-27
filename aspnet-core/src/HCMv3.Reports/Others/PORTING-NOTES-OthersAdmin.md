# Porting notes — batch **OthersAdmin**

Legacy source: `C:\GBS\HCM\HRMS_Production\HCM10.Module\Reports\Others`
Target: `aspnet-core/src/HCMv3.Reports/Others`

## Scope and status

| Legacy report | Status | Notes |
|---|---|---|
| `rptEmployeeProfile` | **Ported** | Wide multi-section dossier + 1 visible subreport. |
| `rptEmpWorklocation` | **Ported** | Arabic-culture switch not ported — see Layout deviations. |
| `rptLocationWiseEmp` | **Ported** | Sponsor semantics deviate — see below. |
| `rptHolidayCalendar` | **Ported** | Look-up `SelectQuery` → `CustomSqlQuery` for tenancy. |
| `rptPassportInOut` | **Ported** | |
| `rptPassportRequestForm` | **Ported** | Legacy namespace was `GovtForms`; port follows the folder. |
| `rptBusinessTrip` | **Ported** | |
| `rptTravelDeskReport` | **BLOCKED** | See BLOCKED. |
| `srptEmpProfileWageDetail` | **Ported** (support) | Subreport required by the two above it; not independently launchable. |

`*_CustomNNN` variants were out of scope and were not touched.

### Files this batch created (and the only files it may edit)

```
OthersAdminSql.cs
OthersAdminImageStore.cs
PORTING-NOTES-OthersAdmin.md
rptEmployeeProfile.cs / .Designer.cs / .xrPictureBox2.png
rptEmpWorklocation.cs / .Designer.cs
rptLocationWiseEmp.cs / .Designer.cs
rptHolidayCalendar.cs / .Designer.cs
rptPassportInOut.cs / .Designer.cs / .xrPictureBox1.jpg
rptPassportRequestForm.cs / .Designer.cs / .xrPictureBox1.jpg
rptBusinessTrip.cs / .Designer.cs / .xrPictureBox2.png
srptEmpProfileWageDetail.cs / .Designer.cs
```

> **Collision note for the orchestrator.** `srptEmpProfileWageDetail.*` is the BASE subreport, owned
> by this batch because `rptEmployeeProfile` (mine) is its only base-variant parent. A batch porting
> `rptEmployeeProfile_Custom033` needs `srptEmpProfileWageDetail_Custom033`, a different file, so
> there should be no clash — but `srptEmpProfileWageDetailDatewise.Designer.cs` appeared in this
> folder from another batch mid-run, so please confirm no one else also wrote the non-Datewise one.

## csproj entries needed

The four legacy `.resx` bitmaps were extracted byte-identically (the legacy entries used the
`bytearray.base64` mimetype, i.e. the raw file rather than a serialised object) and are loaded by
`Others/OthersAdminImageStore.cs`. A `.resx` `System.Drawing.Bitmap` entry breaks the multi-targeted
build, which is why they are embedded resources instead.

```xml
<EmbeddedResource Include="Others\rptEmployeeProfile.xrPictureBox2.png" />
<EmbeddedResource Include="Others\rptBusinessTrip.xrPictureBox2.png" />
<EmbeddedResource Include="Others\rptPassportInOut.xrPictureBox1.jpg" />
<EmbeddedResource Include="Others\rptPassportRequestForm.xrPictureBox1.jpg" />
```

Logical resource names must resolve to `HCMv3.Reports.Others.<filename>` — that is what the default
`Others\` folder → namespace mapping already produces, matching the existing
`Others\SalaryCertificate*Background.jpg` entries. If the orchestrator prefers a wildcard, the four
files are the only `Others\rpt*.xrPictureBox*.*` in this batch.

## Registry entries

```csharp
[rptEmployeeProfile.ReportName]     = () => new rptEmployeeProfile(),
[rptEmpWorklocation.ReportName]     = () => new rptEmpWorklocation(),
[rptLocationWiseEmp.ReportName]     = () => new rptLocationWiseEmp(),
[rptHolidayCalendar.ReportName]     = () => new rptHolidayCalendar(),
[rptPassportInOut.ReportName]       = () => new rptPassportInOut(),
[rptPassportRequestForm.ReportName] = () => new rptPassportRequestForm(),
[rptBusinessTrip.ReportName]        = () => new rptBusinessTrip(),
```

`srptEmpProfileWageDetail` is deliberately **not** registered: it is a subreport, instantiated by its
parents, and has no meaning on its own. It still declares a `ReportName`
(`"EmpProfileWageDetail"`) so the Web Report Designer can address it if the parent layout is ever
opened for editing.

## Catalog rows

```
("PR???", "EmployeeProfile",     "Employee Profile",        "ملف الموظف",              ReportCategory.Default),
("PR???", "EmpWorklocation",     "Employees by Work Location", "الموظفون حسب موقع العمل", ReportCategory.Default),
("PR???", "LocationWiseEmp",     "Location Wise Employees", "الموظفون حسب الموقع",      ReportCategory.Default),
("PR???", "HolidayCalendar",     "Holiday Calendar",        "تقويم العطلات",            ReportCategory.Default),
("PR???", "PassportInOut",       "Passport In / Out",       "تسليم واستلام جواز السفر", ReportCategory.Default),
("PR???", "PassportRequestForm", "Passport Request Form",   "نموذج طلب جواز السفر",     ReportCategory.Default),
("PR???", "BusinessTrip",        "Business Trip",           "رحلة عمل",                 ReportCategory.Default),
```

The Arabic names are translations of the English report titles, not strings recovered from the
legacy layouts (the legacy reports carried no Arabic title). Please have them reviewed.

## Tenant audit

Counted per SQL constant in `OthersAdminSql.cs`: real table references in the SQL body (comments
excluded) vs. `TenantId = @TenantId` predicates vs. `IsDeleted = 0` predicates.

| Constant | Tables | `TenantId = @TenantId` | `IsDeleted = 0` |
|---|---|---|---|
| `CurrentOrgAssignment` (fragment) | 1 | 1 | 1 |
| `DepartmentAncestor` (fragment) | 4 | 4 | 4 |
| `Level2Ancestor` (fragment) | 3 | 3 | 3 |
| `LatestIdentityDocuments` (fragment) | 4 | 4 | 4 |
| `OpenEndedBank` (fragment) | 2 | 2 | 2 |
| `LastIncrement` (fragment) | 4 | 4 | 4 |
| `EmployeeProfileQuery` | 6 | 6 | 6 |
| `EmployeeWageDetailQuery` | 8 | 8 | 8 |
| `BusinessTripQuery` | 6 | 6 | 6 |
| `EmpWorklocationQuery` | 5 | 5 | 5 |
| `LocationWiseEmpQuery` | 6 | 6 | 6 |
| `WorkLocationLookupQuery` | 1 | 1 | 1 |
| `HolidayCalendarQuery` | 3 | 3 | 3 |
| `HolidayCalendarLookupQuery` | 1 | 1 | 1 |
| `PassportInOutQuery` | 2 | 2 | 2 |
| `PassportRequestFormQuery` | 2 | 2 | 2 |

(The per-report totals include only that constant's own body; the shared fragments it concatenates
are audited on their own rows.)

**Every table reference is covered. There are no exemptions** — no lookup table used by this batch
lacks a `TenantId` column. `AbpOrganizationUnits` was the one to watch and it does have both
`TenantId` and `IsDeleted`.

Verified at run time as well as by inspection: all eight reports were constructed and probed, and
every `CustomSqlQuery` in every one both mentions `@TenantId` and has the matching `TenantId`
`QueryParameter` wired by `EnsureHcmParameters()`. `ApplyTenant(7)` propagated to every report and
nested subreport. See "Verification" below.

Two look-up queries deserve a specific call-out, because getting them wrong leaks quietly through
the **parameter panel** rather than through the report body:

* `WorkLocationLookupQuery` — feeds the `PrmWorkLocation` drop-down on both work-location reports.
* `HolidayCalendarLookupQuery` — feeds the `Calendar` drop-down on `rptHolidayCalendar`. This one
  was a designer `SelectQuery` (a table picker) in the legacy report. A `SelectQuery` cannot carry a
  `WHERE` clause, so it **could not be tenant-filtered at all** and would have listed every tenant's
  calendars. It is re-expressed as a `CustomSqlQuery`, keeping the legacy column aliases
  (`OID`/`Code`/`NameEn`/`NameAr`) so the parameter's `DynamicListLookUpSettings` needed no edit.

## BLOCKED

### `rptTravelDeskReport` — no `TravelDeskRequest` equivalent in HCMv3

The report is driven by `TravelDeskRequest`, a legacy **self-service workflow request** type
(`HCM10.Module.BusinessObjects.Workflow.General.TravelDeskRequest`, an XPO subclass of
`WorkflowRequest`). Its driving query is
`FROM TravelDeskRequest LEFT JOIN WorkflowRequest ON TravelDeskRequest.OID = WorkflowRequest.OID`,
filtered `WHERE WorkflowRequest.RequestNumber = @RequestNumber`.

HCMv3 has **no** `TravelDeskRequests` table and no entity for it. Concretely:

* `HCMv3.TravelManagement.Travel` is a port of the *other* legacy entity,
  `HCM10.Module.BusinessObjects.TravelManagement.Travel` — the admin-entered travel record, not the
  employee's request. Its own XML doc says so.
* `HCMv3.Workflow.WorkflowRequest` exists and carries `RequestNumber`, but **nothing links it to any
  travel row** — there is no `TravelId`/`WorkflowRequestId` FK in either direction, and the legacy
  shared-primary-key inheritance that made the join work does not exist in this port.
* `TravelPurpose` (a bound label on the form) exists on no HCMv3 entity at all.

This is the same class of gap the brief already records for `LeaveRequest` / `AbsenceRequest` /
`ReturnFromLeaveRequest`.

**Possible unblock, needs a product decision — not taken unilaterally.** The report could be
re-pointed at `Travels`, which does carry `EmployeeId`, `StartDate`, `EndDate`, `TotalTravelCost`
and a `Code`. That would mean:

1. keying on `Travels.Code` instead of `WorkflowRequest.RequestNumber` (defensible — `Code` is the
   travel record's business key), and
2. either leaving the `TravelPurpose` label permanently blank, or mapping it to `Travels.Comments`,
   which is a *different* field with a different meaning.

Both changes alter what the printed document means, so they are a product call, not a port call. Say
the word and it is roughly an hour's work; everything else it needs (employee, job title,
department, company, manager via `EmployeeManagers`) is already available and already solved by the
fragments in `OthersAdminSql.cs`.

Nothing was written for this report — no stub, no half-file.

## Defects carried over

Legacy bugs reproduced faithfully rather than fixed.

1. **`rptPassportInOut` resolves the job title as of a date the user cannot set.** The query filters
   the org assignment on `@PrmDate`, but the layout only ever exposes `PrmFromDate` and `PrmToDate`.
   `PrmDate` therefore sits at its designer default forever. *Correct behaviour:* it should almost
   certainly use `PrmFromDate`, or today. Not changed — it would silently alter which job title
   prints on an existing form.

2. **Arbitrary document-type matching.** Passport and civil-ID documents are located by
   `DocumentTypes.NameEn LIKE '%passport%'` / `'%civil%'` — a *name* match. A tenant with two
   document types whose English name contains "passport" gets an arbitrary one. *Correct behaviour:*
   match on a stable `DocumentTypes.Code`. Not changed: it needs a product decision about which code
   is canonical, and changing it would move data on existing forms.

3. **`rptPassportInOut` uses `NameEn IN ('PASSPORT')` where the rest of the batch uses
   `LIKE '%passport%'`.** Two different matching rules for the same concept in the same batch. Left
   as-is on purpose — aligning them would change which document this report picks for tenants whose
   type name is not exactly "PASSPORT".

4. **`rptLocationWiseEmp` has one customer's Arabic company and officer names hard-coded in the
   layout** as `StaticListLookUp` values on `PrmCompany` / `PrmSignature`, and the footer chooses a
   job title by string-comparing against one of those names. In a multi-tenant product every tenant
   now sees that customer's names in the parameter drop-down. This is a *layout* fact, not a data
   leak. *Correct behaviour:* the signatory list should come from `Signatories` / `Sponsors` per
   tenant. Reproduced verbatim; flagged for follow-up.

5. **`MaxDate` takes `MAX(EndDate)` and `MAX(StartDate)` independently** in the last-increment
   arithmetic, so on data where the newest basic-pay header is not also the latest-starting one, the
   two values come from different rows. Reproduced. On real data (one open-ended header per
   employee) they always agree.

6. **`PreviousAmount` is repeated on every wage-type detail row** in the wage-detail subreport
   rather than appearing once. That is how the legacy report printed it and the layout depends on
   it.

### Defects deliberately NOT carried over

Each of these would have failed outright or produced a visibly wrong document on HCMv3, so
reproducing them was not an option. All are called out in the XML docs at the point of change.

1. **Row multiplication in the last-increment lookup.** The legacy derived table grouped by currency
   and joined back on employee *code*, so an employee with basic-pay lines in two currencies
   produced two rows — and this is a one-employee-per-page report, so the whole profile page printed
   twice. Now `TOP 1` with a deterministic `ORDER BY`. The same reasoning was applied to the
   open-ended bank record and to `rptBusinessTrip`'s residency record, both of which the legacy SQL
   joined with no `TOP 1`.

2. **`rptBusinessTrip`'s passport join compared an FK to a business code**
   (`EmployeeDocument.DocumentType = DocumentType.Code`, then `WHERE DocumentType.Code = 1`). In
   HCMv3 `DocumentTypes.Code` is `nvarchar`, so reproducing it literally would make SQL Server
   implicitly convert the whole column to `int` and **throw** on the first non-numeric code.
   Rewritten to the correct FK join plus the same name-based type match used elsewhere, keeping the
   "currently valid passport" predicate (`GETDATE() BETWEEN IssueDate AND ExpiryDate`).

3. **Dead payslip `CalculatedField`s.** `rptEmpWorklocation` and `rptLocationWiseEmp` were
   copy-pasted from the payslip and carry calculated fields with `DataMember = "PayslipQuery"` — a
   query that does not exist in their data source. `CalcHeaders` additionally referenced
   `[Parameters].[PrmPayrollRunType]` and `[Parameters].[PrmPeriodMonth]`, **parameters these
   reports do not have**, and its expression lived in the `.resx` that is no longer generated.
   `CalcHeaders` was removed from both reports; nothing bound it. The other dead payslip calculated
   fields were left in place — they are inert, they were inert in the legacy report too, and
   removing them would be gratuitous layout churn.

4. **Unreferenced `QueryParameter`.** `srptEmpProfileWageDetail` declared a `PrmDate`
   `QueryParameter` its SQL never mentioned. Dropped. `PrmDate` survives as a *report* parameter,
   because the parent binds it.

5. **Client-side `FilterString` over a raw `SqlDataSource`** was pushed into SQL where it could be
   (`emp.Code = @PrmEmpCode` on the profile/business-trip/passport-in-out reports). It was
   **deliberately left client-side** on two reports, for reasons rather than oversight:
   * `rptPassportRequestForm` — `PrmEmpCode` is `MultiValue = true`, and a multi-value list cannot
     bind to a single SQL parameter without dynamic SQL, which `CustomSqlQuery` rejects.
   * `rptHolidayCalendar` — the filter is over three user-adjustable parameters and the result set
     is tens of rows per tenant.
   In both cases the SQL still carries the tenant predicate, so this is a performance
   characteristic, not a cross-tenant exposure.

## Layout deviations

1. **The org-unit rewrite (affects `rptEmployeeProfile`, `rptBusinessTrip`).**
   Legacy resolved "Department" by joining `OrganizationUnit` seven times on the flat
   `OrgLevel1..OrgLevel7` FK columns, each filtered on
   `OrganizationUnitType.NameEn = 'Department'`, then picking the first non-null with a seven-branch
   `CASE`. HCMv3 has a single `EmployeeOrgAssignments.OrganizationUnitId` into
   `AbpOrganizationUnits`, a materialised-path tree, with the unit type carried by
   `OrgUnitProfiles.OrganizationUnitTypeId`. The seven joins become one ancestor walk: a
   materialised path is a prefix by construction, so `ownUnit.Code LIKE ancestor.Code + '%'` finds
   ancestors, and `ORDER BY LEN(ancestor.Code)` **ascending** takes the shallowest one of that type.

   Implemented per `Bank/BankSql.cs`, which is the reference implementation. The fragments are
   **duplicated** in `OthersAdminSql.cs` rather than referenced, so this batch stays self-contained
   and does not depend on another batch's folder. If the orchestrator would rather they were shared,
   `DepartmentAncestor`, `Level2Ancestor` and `CurrentOrgAssignment` are easy to consolidate later.

   `AbpOrganizationUnits` has only `DisplayName`, so English org names come from `DisplayName` and
   Arabic from `OrgUnitProfiles.NameAr`.

   **Two corrections applied after the coordinator's mid-flight note — worth reading if you are
   reviewing another batch.** My first draft had copied `EmployeeInfoSqlFragments.cs`, and with it
   two faults:

   * **`ORDER BY LEN(Code) DESC` → ASC.** DESC takes the *nearest* ancestor; the legacy CASE tested
     `OrgLevel1` first and fell through to `OrgLevel7`, i.e. it took the *shallowest*. On any tree
     with two `Department`-typed units on one path, DESC silently prints a different unit. This is
     a real behaviour bug, not a stylistic difference, and it is present in
     `EmployeeInfoSqlFragments.cs` today (`DepartmentAncestor`, `CompanyAncestor`,
     `DivisionAncestor` all order DESC) — **worth checking when that batch is reviewed.**
   * **The own-unit `COALESCE` fallback is gone.** It only ever existed as a workaround for the
     mistaken belief that the unit type was not expressible in HCMv3. Where the legacy CASE yielded
     NULL, this port now yields NULL too. No fallback of any kind remains in this batch's SQL.

2. **`rptPassportRequestForm`'s department is a *level-wise* lookup, and it also translates
   exactly.** Legacy took `OrgLevel2` positionally, with no unit-type filter — "whatever sits at
   level 2". Because ABP's `Code` is a materialised path of five-digit segments joined by dots
   (`"00001.00002.00003"`), a unit's **depth is a function of its code length**: level N has
   `LEN(Code) = 6N - 1`, so level 2 is `LEN(Code) = 11`. `Level2Ancestor` uses exactly that.

   So this is **no longer a deviation** — an ancestor whose code is 11 characters long *is* the
   OrgLevel2 unit. (My first draft did fall back to the employee's own unit here and flagged it as
   needing a product decision; that is withdrawn — no decision is needed.) An employee sitting at
   level 1 has no level-2 ancestor and gets NULL, which is what the legacy `LEFT JOIN` produced in
   the same situation.

3. **`rptLocationWiseEmp`'s sponsor — semantic deviation.** Legacy read the sponsor from
   `WorkLocation.Sponsor`. HCMv3's `WorkLocations` has no sponsor column and no HCMv3 entity links a
   work location to a sponsor, so "the sponsor of this work location" is not expressible. Only one
   of the three legacy sponsor columns is actually bound by the layout — `TradeLicenseNum`, printed
   in the page header — and it is now sourced from the **employee's** sponsor
   (`EmployeeSponsors` → `Sponsors` → `SponsorLicenseInfos`). On this report every row is one work
   location's staff, who in practice share a sponsor, so the printed value will usually agree; it is
   not guaranteed to. Also note `TradeLicenseNum` lives on `SponsorLicenseInfos` in HCMv3, not on
   `Sponsors`, so the newest licence by `LicenseExpiryDate` is taken. **Needs a product decision.**

4. **`rptEmpWorklocation`'s Arabic-culture switch is not ported.** The legacy code-behind hooked
   `ParametersRequestBeforeShow` and, when
   `WebApplication.Instance.CurrentAspectProvider.CurrentAspect == "ar-KW"`, flipped the report to
   `RightToLeft`, overwrote six column captions with Arabic literals, and rewrote four
   `ExpressionBindings` to their Arabic columns. That reads the current UI culture from the XAF web
   application singleton, which does not exist here, and hard-codes Arabic strings inside a report.
   The SQL still projects every Arabic column the switch needed (`FullNameAr`, `JobTitleAr`,
   `NationalityAr`, `WL_Ar`), so an Arabic variant can be built without touching the query. **The
   English rendering is unchanged; only the Arabic rendering is lost.**

5. **Two invisible subreports are not ported.** `rptEmployeeProfile` and `rptBusinessTrip` each
   carry `xrSubreport1` → `srptEmployeeProfileNetPay` and `xrSubreport2` → `srptEmployeeProfile`,
   both `Visible = false` in the legacy layout. Porting a report that is never drawn is dead code,
   so those two classes were not ported; the placeholders keep their original position and size (so
   band geometry is byte-equivalent) and simply have no `ReportSource`. The visible third subreport,
   `srptEmpProfileWageDetail`, **is** ported.

6. **`rptPassportRequestForm` changed namespace.** The legacy class declared
   `namespace HCM10.Module.Reports.GovtForms` even though the file lives in the `Reports\Others`
   folder. The port follows the folder: `HCMv3.Reports.Others`. It is not a GovtForms report — it
   prints over a company form, not an official government one.

7. **Company logo / letterhead images moved out of the `.resx`** into embedded PNG/JPEG files loaded
   by `OthersAdminImageStore`, byte-identically. Side effect: the Visual Studio report designer
   shows an empty picture box, because the image is no longer assigned inside
   `InitializeComponent`. The rendered document is unaffected. Same pattern as
   `GovtFormsImageStore` and `SalaryCertificateBackground`.

## New v23.2 API break found — not on the brief's list

Worth adding to the shared brief for the batches still running:

```
System.Drawing.StringTrimming  ->  DevExpress.Drawing.DXStringTrimming
```

`XRControl.TextTrimming` changed type in v23.2, so `this.xrLabel9.TextTrimming =
System.Drawing.StringTrimming.None;` is now **CS0266**, not a silent failure. Hit in
`rptEmployeeProfile.Designer.cs` and `rptBusinessTrip.Designer.cs`.

All the breaks the brief *did* list were hit and handled: `PaperKind` → `DXPaperKind`,
`PrintEventHandler(PrintEventArgs)` → `BeforePrintEventHandler(CancelEventArgs)` (report level on
`rptPassportInOut`, band level on `rptLocationWiseEmp`), `Scripts.OnBeforePrint` dropped,
`ResultSchemaSerializable` dropped, `.resx` bitmaps moved to an image store.

## Verification — what was actually run, and what was not

**Compiles clean.** `dotnet build aspnet-core/src/HCMv3.Reports/HCMv3.Reports.csproj` currently
fails with ~944 errors, **all of them in `Others/OthersCertificatesSql.cs`**, another batch's
in-progress file. To get a trustworthy signal for my own work I compiled `HcmReportBase.cs` plus
this batch's 18 files in an isolated scratch project against the same
`DevExpress.Reporting.Core 23.2.13` package: **0 errors**, 1 warning (`CS0169`, the unused
`SalaryCertificate_LNDReportDS` field that the legacy `rptPassportInOut` designer also carried —
left alone as faithful dead weight).

**Constructed and probed at run time.** All eight reports were instantiated and inspected. Every one:
constructs without throwing (so `Margins` / `PageHeight` / `PaperKind` are all accepted by v23.2);
carries the hidden `PrmTenantId` parameter; has every query as a `CustomSqlQuery`; has `@TenantId`
present in every query's SQL text; and has the matching `TenantId` `QueryParameter` wired by
`EnsureHcmParameters()`. `ApplyTenant(7)` propagated to each report and to the nested subreport.

```
rptEmployeeProfile           tenantParam=True queries=2 wired=2 applied=7
rptEmpWorklocation           tenantParam=True queries=2 wired=2 applied=7
rptLocationWiseEmp           tenantParam=True queries=2 wired=2 applied=7
rptHolidayCalendar           tenantParam=True queries=2 wired=2 applied=7
rptPassportInOut             tenantParam=True queries=1 wired=1 applied=7
rptPassportRequestForm       tenantParam=True queries=1 wired=1 applied=7
rptBusinessTrip              tenantParam=True queries=2 wired=2 applied=7
srptEmpProfileWageDetail     tenantParam=True queries=1 wired=1 applied=7
ALL OK
```

**Silent-failure greps: clean.** No `.Text = "["` mail-merge on a data field, no `!format` suffix,
and no `XRBinding(` at all in this batch's designers — every data-bound control uses
`ExpressionBindings`, which is the form that works.

### NOT done — please do not read the above as more than it is

**No report was rendered against a database.** I did not run `CreateDocument()` against SQL Server,
so nothing here proves the SQL *executes*, that every column name resolves, or that any cell is
non-blank. Column names were derived from the EF Core entities under `HCMv3.Core` and from
`HCMv3DbContextModelSnapshot.cs` and none were guessed, but that is inspection, not execution. The
things a render test would catch and the checks above would not: a column-name typo inside a
`SELECT` list, an `OUTER APPLY` that returns nothing on real data, and a bound expression pointing
at a column the query does not actually emit.

**A render test is the single highest-value next step for this batch**, particularly for
`rptEmployeeProfile` (28 projected columns) and `rptBusinessTrip`.

Also worth knowing when interpreting a first render, from the `EmployeeInfo` batch's findings on the
current dev database: tenant 1 holds exactly one organisation unit and the verification employee's
org assignment has `OrganizationUnitId = NULL`, so **every Department column will render blank
today**. That is missing data, not a broken rewrite.
