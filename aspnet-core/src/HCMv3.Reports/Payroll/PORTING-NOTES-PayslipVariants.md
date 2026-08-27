# Porting notes — batch **PayslipVariants**

Target folder: `aspnet-core/src/HCMv3.Reports/Payroll/`
Legacy source: `HCM10.Module/Reports/Payroll/`

| Legacy report | Status | Files delivered |
|---|---|---|
| `rptSubordPayslipEn` | **PORTED** | `rptSubordPayslipEn.cs`, `rptSubordPayslipEn.Designer.cs` |
| `rptSubordPayslipAr` | **PORTED** | `rptSubordPayslipAr.cs`, `rptSubordPayslipAr.Designer.cs` |
| `rptSocialSecurity`  | **PORTED** | `rptSocialSecurity.cs`, `rptSocialSecurity.Designer.cs` |
| `rptPayslip_wosp_En` | **PORTED** | `rptPayslip_wosp_En.cs`, `rptPayslip_wosp_En.Designer.cs` |
| `rptPayslip_wsp_En`  | **BLOCKED** | none — see BLOCKED below |

Shared SQL: `PayslipVariantSql.cs` (new, this batch only).

`*_CustomNNN` variants and `rptPayslipEn_Old` were ignored as instructed.

---

## The judgment call: reuse vs. clone

**Result: ZERO new `srpt*` files were created. Every subreport any ported variant needs is an
already-committed class, INSTANTIATED.**

| Variant | Subreports | Verdict |
|---|---|---|
| `rptSubordPayslipEn` | `srptPaySlipEarningEn`, `srptPaySlipDeductionEn`, `srptPayslipAttendenceSummaryEn`, `srptPayslipWorkingDaysSummaryEn`, `srptPayslipIndemnityDaysSummaryEn` | **REUSE** — all five committed, instantiated unchanged |
| `rptSubordPayslipAr` | `srptPaySlipEarningAr`, `srptPaySlipDeductionAr`, `srptPayslipAttendenceSummaryAr`, `srptPayslipWorkingDaysSummaryAr`, `srptPayslipIndemnityDaysSummaryAr` | **REUSE** — all five committed, instantiated unchanged |
| `rptPayslip_wosp_En` | `srptPaySlipEarningEn`, `srptPaySlipDeductionEn` | **REUSE**, but the ParameterBindings on the *parent* had to be retargeted (see below) |
| `rptPayslip_wsp_En` | would have needed `srptPaySlipAttendanceEn`, `srptPaySlipGratuityEn`, `srptPaySlipWorkingDaysEn` — **three genuinely different classes that were never ported** | **BLOCKED before this question mattered** |

Why the hypothesis held: for the two Subord reports the variation really is only *which* subreports
appear plus the employee-selection predicate. Their legacy ParameterBindings already used the exact
parameter names the committed HCMv3 subreports expose (`EmpCode`/`empcode`, `ForPeriod`, `InPeriod`,
`RunType`), so the bindings transferred verbatim. Cloning nine subreports four times over was
avoided; the delta between the base payslip and these variants lives entirely in the master query.

Two adaptations that stop short of modifying anything committed:

* **`rptPayslip_wosp_En` binding retarget.** The legacy layout bound parameters named
  `"EmployeeCode"` / `"ForPeriod"` — the contract of the *legacy* copies of those two subreports as
  they existed when wosp was authored. The committed HCMv3 `srptPaySlipEarningEn` /
  `srptPaySlipDeductionEn` follow `rptPayslipEn`'s four-parameter contract. The fix is in the
  **parent's** designer file: bindings now supply `empcode`/`EmpCode`, `ForPeriod`, `InPeriod`,
  `RunType`. The subreports were not touched.
* **`rptSubordPayslipAr` supplies no `PayrollRunType`.** The legacy Arabic layout never bound it, so
  the five Arabic subreports fall back to their own default (0 = regular run). Preserved as-is; it is
  a legacy gap, not a port artefact.

---

## csproj entries needed

**none.** No `.resx` and no image assets. Everything that lived in the legacy `.resx` files (four
SQL strings, three Arabic month expressions, four FilterStrings) is now a C# constant.

## Registry entries

```csharp
[rptSubordPayslipEn.ReportName]  = () => new rptSubordPayslipEn(),
[rptSubordPayslipAr.ReportName]  = () => new rptSubordPayslipAr(),
[rptSocialSecurity.ReportName]   = () => new rptSocialSecurity(),
[rptPayslip_wosp_En.ReportName]  = () => new rptPayslip_wosp_En(),
```

`ReportName` values: `SubordPayslipEn`, `SubordPayslipAr`, `SocialSecurity`,
`PayslipWithoutSalaryParticularsEn`.

## Catalog rows

```
("PR???", "SubordPayslipEn",                   "Subordinates Payslip",                 "كشف رواتب المرؤوسين",        ReportCategory.Default),
("PR???", "SubordPayslipAr",                   "Subordinates Payslip (Arabic)",        "كشف رواتب المرؤوسين (عربي)", ReportCategory.Default),
("PR???", "SocialSecurity",                    "PIFSS Social Security Report",         "تقرير التأمينات الاجتماعية", ReportCategory.Default),
("PR???", "PayslipWithoutSalaryParticularsEn", "Payslip Without Salary Particulars",   "كشف الراتب بدون تفاصيل",     ReportCategory.Default),
```

---

## Tenant audit

Counted mechanically per SQL constant (table references in `FROM` / `JOIN` / `OUTER APPLY`
positions vs. `TenantId = @TenantId` predicates), then verified by hand.

| Constant | Table refs | `TenantId = @TenantId` | Balanced? |
|---|---|---|---|
| `LogoQuery` | 1 | 1 | yes |
| `SubordPayslipEnQuery` | 23 | 23 | yes |
| `SubordPayslipArQuery` | 19 | 19 | yes |
| `SocialSecurityQuery` | 21 | 21 | yes |
| `SocialSecurityCompanyList` | 3 | 3 | yes |
| `SocialSecurityDepartmentList` | 3 | 3 | yes |
| `PayrollAreaList` | 1 | 1 | yes |
| `PayslipWospQuery` | 17 | 17 | yes |

A naive `grep` over `SocialSecurityQuery` reports 23 and over `SocialSecurityCompanyList` reports 4;
the surplus in each case is the word `JOIN` appearing inside a `--` comment that quotes the legacy
SQL. There are no unqualified table references.

**Lookup tables with no `TenantId` column: none.** Every table touched by this batch —
`AbpOrganizationUnits`, `OrgUnitProfiles`, `OrganizationUnitTypes`, `Banks`, `Currencies`,
`JobTitles`, `GovtJobTitles`, `AbsenceTypes`, `WageTypes`, `PayrollAreas`, `EmployeeGroups`,
`EmployeeSubGroups` included — carries both `TenantId` and `IsDeleted`, and both are filtered.

The `@TenantId` literal is spelled exactly that way everywhere, so `EnsureHcmParameters` wires the
`TenantId` QueryParameter into every query. All four reports call `EnsureHcmParameters` from their
constructor; `rptSubordPayslipEn`, `rptSubordPayslipAr` and `rptSocialSecurity` pass
`withLoggedUserName: true`.

---

## BLOCKED

### `rptPayslip_wsp_En` — payslip WITH salary particulars

**Missing: two stored procedures whose bodies exist only in the legacy database.**

The report's master data source is not SQL in the layout — it is
`StoredProcQuery { StoredProcName = "usp_rpt_payslip" }` taking
`@lang, @prmPayrollArea, @prmEmpCode, @prmForMonth, @prmForYear`.
Its three own subreports (`srptPaySlipAttendanceEn`, `srptPaySlipGratuityEn`,
`srptPaySlipWorkingDaysEn` — none of them ported, and all three genuinely distinct from the committed
`*Summary*` subreports) each bind `StoredProcName = "usp_rpt_payslip_details"` with
`(@prmSalRegId, @prmType)`.

A repository-wide search finds **no CREATE PROCEDURE, no `.sql` file, no migration** for either
procedure — the four `.Designer.cs` files above are the only places the names appear anywhere in
`HRMS_Production`.

What that means concretely:

* The master column set *is* recoverable from the XRBindings (`EmpCode`, `ForPeriod`, `OrgUnit`,
  `EmpName`, `Grade`, `SalProfile`, `JobTitle`, `JoiningDate`, `GratuityStartDate`,
  `PassportExpiryDate`, `GratuityPaidTill`, `BankName`, `IBANNum`, `NetPay`, `SalRegId`), so the
  master query could be reconstructed.
* The three subreports cannot be. `usp_rpt_payslip_details` returns only `(WageType, Amount)`, and
  the entire meaning of the report is in the `@prmType` discriminator — `0` earning, `1` deduction,
  `2` attendance, `3` gratuity, `4` working days. **Which wage types fall in buckets 2, 3 and 4 is
  defined solely inside the procedure body.** HCMv3's `WageTypes` has no flag that reproduces that
  partition; picking one would be inventing payroll semantics, which the brief forbids.
* "Salary particulars" *are* those three blocks. A wsp port carrying only the header plus earnings
  and deductions is byte-for-byte the report next to it, `rptPayslip_wosp_En`. Shipping that under
  the wsp name would be worse than shipping nothing.

The generated `rptPayslip_wsp_En.Designer.cs` stub was therefore **deleted** rather than left in the
tree — a partial class with no `.cs` partner is a `CS0115` for every other batch.

**To unblock:** script out `usp_rpt_payslip` and `usp_rpt_payslip_details` from the legacy
`HRMS_RCF` database and add them to the migration corpus. With those two bodies the report is a
routine port; without them it is not portable at all.

---

## Defects carried over

Legacy bugs reproduced faithfully:

1. **`rptSubordPayslipEn/Ar` — currency keyed on the wrong person.** The legacy derived table that
   supplies `Currency` joins on `emp.OID`, the **manager**, not the subordinate whose payslip is being
   printed. Preserved (`ebp.EmployeeId = mgr.Id`). Correct behaviour would key it on `sub.Id`; the
   two agree only where manager and subordinate share a pay currency.
2. **`rptSubordPayslipEn` runs its `BeforePrint` handler twice.** The legacy designer wires the same
   handler at both report and Detail-band level. Kept; it only assigns `FilterString`, so it is
   harmless.
3. **`rptPayslip_wosp_En` — `OFFCurrentMonthCal` counts sick leave in its own total.** Despite the
   name, the legacy expression summed *every* absence in the period with no type filter, while
   `SLCal` summed only sick leave. So the two figures overlap. The SQL columns `OffDays` /
   `SickLeaveDays` reproduce exactly that.
4. **`rptSubordPayslipAr` — `srptPayslipIndemnityDaysSummaryAr.EmpCode` bound to the `Subordinates`
   parameter**, which is an `Employees.Id`, not an employee *code*. Preserved verbatim; the
   indemnity block in the Arabic subordinate payslip therefore prints empty in legacy and in the
   port. Correct binding would be `Query.SubordinateCode`.
5. **`rptSubordPayslipAr` — `srptPayslipWorkingDaysSummaryAr` has `ForPeriod` and `InPeriod` swapped**
   in its ParameterBindings (`ForPeriod` ← `Query.InPeriod`, `InPeriod` ← `Query.ForPeriod`).
   Preserved. Harmless for regular runs where the two are equal; wrong for off-cycle runs.
6. **`rptSocialSecurity` — the `@PrmPayrollRunType IS NULL` branch is unreachable.** The report
   parameter is a non-nullable `int` pinned to 0 or 1 by a static lookup, so the `OR ... IS NULL`
   arm can never fire. Kept for byte parity.

Three legacy defects **deliberately NOT reproduced**, each because reproducing it would throw or
would corrupt money:

7. **`rptSubordPayslipAr` union row typed `-1` as an integer.** `[manager Code]` and
   `SubordinateCode` are `nvarchar` in the first `UNION` branch; the legacy Arabic query supplies the
   integer literal `-1` in the second. `UNION` type resolution promotes the whole column to `int`,
   so the legacy query throws *"Conversion failed when converting the nvarchar value … to data type
   int"* for any tenant with a non-numeric employee code. Changed to `N'-1'`. The English sibling
   already had this right.
8. **`rptSocialSecurity` — `EmployeeSocialSecurity` join fanned rows out.** Legacy used
   `LEFT JOIN EmployeeSocialSecurity … WHERE …OID IS NOT NULL`. Two overlapping social-security
   records for one employee duplicate the salary-register row and **double that employee's reported
   contribution**. Rewritten as `EXISTS`; identical row set otherwise. Doubling a figure in a
   statutory PIFSS return is not a defect worth preserving.
9. **`rptSocialSecurity` / `rptPayslip_wosp_En` — `EmployeeBasicPay` joined and never projected.**
   1:N, so it can only multiply rows and every money column with them. Dropped, matching the call
   `BankSql` and `PayslipSql` already made.

---

## Layout deviations

* **`rptSubordPayslipAr` — Arabic display fonts lost.** The legacy constructor loaded `BFantezy.ttf`
  and `BNaznnBd.ttf` into static `PrivateFontCollection`s via
  `HttpContext.Current.Server.MapPath("~/Contents/fonts/…")` and assigned them to eleven date labels.
  Neither font file is in the HCMv3 repository, `HttpContext.Current` does not exist on ASP.NET Core,
  and the call NREs outside a request — which is precisely why this report fails to construct in the
  legacy exporter. Not ported. **The eleven date labels now render in the Arial/Tahoma faces their
  layout already declares.** (`rptPayslipAr` met the same dead code, but there every consumer line
  was already commented out, so it lost nothing.)
* **`rptSocialSecurity` — dead `CalcHeaders` CalculatedField removed.** Its `DataMember` was
  `"PayslipQuery"` (a query this report does not have) and its expression referenced
  `[Parameters].[PrmPeriodMonth]` (a parameter this report does not have). Nothing bound it — the
  header text is assigned in `lblHeader_BeforePrint`. In v23.2 an unresolvable parameter reference
  throws at document creation instead of being ignored.
* **`rptSocialSecurity` — two SqlDataSource master/detail `Relations` removed.** One keyed on a
  column named `DivisionEn` that no query in the report has ever produced; the other nested a flat
  combo list under every master row. Both are copy-paste debris from the payroll-sheet report this
  one was cloned from, and nothing consumes either (the combos bind through
  `DynamicListLookUpSettings.DataMember`).
* **`rptPayslip_wosp_En` — data source rebuilt, sixteen bindings re-pointed.** This is the one report
  in the batch whose layout was XAF rather than SQL: it bound a
  `DevExpress.Persistent.Base.ReportsV2.CollectionDataSource` over the XPO `SalaryRegister` class and
  walked XPO association paths (`Employee.Bank.Bank.NameEn`, …). `CollectionDataSource` is an XAF
  type `HCMv3.Reports` must not reference. Replaced by a `SqlDataSource` +
  `PayslipVariantSql.PayslipWospQuery`; all sixteen `XRBinding`s now use query-qualified column
  names. Five XPO-only CalculatedFields went with it: `OFFCurrentMonthCal`, `SLCal` and `CalNetPay`
  became SQL columns with identical arithmetic; `CalEarning` and `CalDeduct` were already unbound.
  Every removal is annotated inline in the designer file.
* **`rptPayslip_wosp_En` — the "Calendar Days" cell prints blank.** Legacy bound
  `Employee.Attendance.CalendarDays`; HCMv3's `EmployeeAttendances` has no `CalendarDays` column at
  all (it carries `AttendanceHours` / `TotalAttendDays` / `AttendanceUnit`). Returned as `NULL`
  rather than silently substituting a different attendance measure into a payslip.
* **`rptPayslip_wosp_En` — government job title simplified.** Legacy path was
  `Employee.Sponsor → Sponsor → GovtJobTitle → GovtJobTitle → NameEn`, i.e. the *sponsor's* job-title
  row. HCMv3 puts `GovtJobTitleId` directly on `EmployeeSponsors`, so the value is now the employee's
  own government job title — which is what the label always claimed to show.

---

## Org-unit resolution — divergence from committed files, flagged deliberately

Per the orchestrator's mid-flight correction: the org-unit **type** *is* expressible in HCMv3, on
`OrgUnitProfiles.OrganizationUnitTypeId`, and `Bank/BankSql.cs` already implements the ancestor walk
correctly. Every new query in `PayslipVariantSql.cs` that needs a company or org-level column uses
**the BankSql pattern**, not the PayslipSql fallback:

| Query | Column | Legacy | This batch |
|---|---|---|---|
| `SocialSecurityQuery` | `CompanyEn`, `DepartmentEn` | 14 aliased `OrgLevel1..7` joins filtered on `OrganizationUnitType.NameEn` | two type-based ancestor walks (shallowest ancestor whose `OrgUnitProfiles` type is `'Company'` / `'Department'`) |
| `SubordPayslipEnQuery`, `SubordPayslipArQuery` | `OrgUnit` | two blind `ParentOrgUnit` hops | type-based `'Department'` ancestor walk, `ISNULL`-ing back to the employee's own unit |
| `PayslipWospQuery` | `OrgUnit` | three blind `ParentOrgUnit` hops | same |
| `SocialSecurityCompanyList`, `SocialSecurityDepartmentList` | combo lists | `OrganizationUnit ⋈ OrganizationUnitType` | `AbpOrganizationUnits ⋈ OrgUnitProfiles ⋈ OrganizationUnitTypes` |

**Consequence to be aware of:** `rptSubordPayslipEn` and `rptPayslipEn` will now print *different*
org-unit strings for the same employee — the subordinate payslip resolves the department properly,
the base payslip still falls back to the employee's own unit because `PayslipSql.cs` carries the
older (incorrect) conclusion. That file is committed and was not edited; correcting it centrally
removes the disagreement.

`ISNULL(department.DisplayName, orgUnit.DisplayName)` is used rather than a bare walk so that a
tenant with no `'Department'`-typed units still prints something, as legacy did.

## Changes needed in committed payslip files

None were required to complete this batch — every reuse is an instantiation. Two items are recorded
here for the orchestrator, neither of them blocking:

1. **`PayslipSql.cs`, rewrite note 1** states that "which ancestor is the Department" is *"not
   expressible"* in HCMv3 and falls back to the employee's own unit. It is expressible, via
   `OrgUnitProfiles.OrganizationUnitTypeId` — see the table above. Same wording appears in
   `SalaryCertificateSql.cs`. Correcting them removes the `rptPayslipEn` / `rptSubordPayslipEn`
   disagreement noted above. **Not edited by this batch.**
2. **`srptPaySlipEarningEn` / `srptPaySlipDeductionEn` parameter naming is inconsistent** — the
   earnings subreport spells it `empcode`, the deductions one `EmpCode`. Every parent has to
   remember which is which (this batch got it wrong once during development). Harmonising them would
   be a breaking change across all existing parents, so it is noted, not done.

---

## Verification performed

1. `dotnet build aspnet-core/src/HCMv3.Reports/HCMv3.Reports.csproj` — the four ported reports and
   `PayslipVariantSql.cs` produce **zero errors and zero warnings**. Diagnostics remaining in the
   project at hand-off all live in `Others/` and belong to other batches' in-flight work — at the
   time of writing, three in `Others/rptSalaryIncrementLetter.Designer.cs` (two `CS1061` for missing
   handlers plus the `CS0266` `StringTrimming` → `DXStringTrimming` break), and a scattering of
   `CS0169` unused-field warnings. Nothing under `Payroll/` from this batch.
2. Silent-failure greps over this batch's four `.Designer.cs` files — **all three clean**:
   * `\.Text = "\[` (mail-merge on a data field) — no hits
   * `!format` — no hits
   * every `XRBinding(` is query-qualified (`"PayslipQuery.X"` / `"Query.X"`) — verified by
     inverse grep, no unqualified bindings
3. `System.Drawing.StringTrimming` (the late-breaking v23.2 `DXStringTrimming` break) — no hits in
   any file of this batch.
4. Tenant audit — table refs vs. `TenantId = @TenantId` predicates balanced in all eight SQL
   constants; see the table above.

**Not verified: rendering.** No report in this batch has been rendered against a live database. The
build gate and the static greps pass; `CreateDocument()` was not run, so nothing here should be
described as render-verified.
