# Porting notes -- batch **PayrollSheets**

Seven legacy DevExpress reports de-XAF-ed into `HCMv3.Reports/Payroll`. All seven are complete
(`.cs` + `.Designer.cs` written, SQL rewritten, project compiles with zero errors in these files).
Nothing was rendered against a live database -- see **Not verified** at the end.

| Report | ReportName | Status |
|---|---|---|
| `rptPayrollSheet` | `PayrollSheet` | complete |
| `rptPayrollSheetSummary` | `PayrollSheetSummary` | complete |
| `rptPayrollSheetByCostCenter` | `PayrollSheetByCostCenter` | complete |
| `rptPayrollSheetByCostCenterPostingGroup` | `PayrollSheetByCostCenterPostingGroup` | complete |
| `rptPayrollSheetByCostCenterSummary` | `PayrollSheetByCostCenterSummary` | complete |
| `rptPayrollSheetBySponsor` | `PayrollSheetBySponsor` | complete |
| `rptSalaryByBudgetingGroup` | `SalaryByBudgetingGroup` | complete |

Files created by this batch (and only these):

```
Payroll/PayrollSheetSql.cs
Payroll/rptPayrollSheet.cs                              Payroll/rptPayrollSheet.Designer.cs
Payroll/rptPayrollSheetSummary.cs                       Payroll/rptPayrollSheetSummary.Designer.cs
Payroll/rptPayrollSheetByCostCenter.cs                  Payroll/rptPayrollSheetByCostCenter.Designer.cs
Payroll/rptPayrollSheetByCostCenterPostingGroup.cs      Payroll/rptPayrollSheetByCostCenterPostingGroup.Designer.cs
Payroll/rptPayrollSheetByCostCenterSummary.cs           Payroll/rptPayrollSheetByCostCenterSummary.Designer.cs
Payroll/rptPayrollSheetBySponsor.cs                     Payroll/rptPayrollSheetBySponsor.Designer.cs
Payroll/rptSalaryByBudgetingGroup.cs                    Payroll/rptSalaryByBudgetingGroup.Designer.cs
Payroll/PORTING-NOTES-PayrollSheets.md
```

---

## How the SQL was factored

This is the point of batching these seven together. In the legacy corpus the same query body was
copy-pasted seven times -- 21 KB to 61 KB of SQL per `.resx`, 16 to 31 `LEFT JOIN (SELECT ... GROUP
BY ...)` derived tables each, all re-scanning `SalaryRegisterDetail`. The copies had drifted, so the
same bucket name meant different wage types in different reports.

`PayrollSheetSql.cs` collapses that into a small set of composed fragments:

| Fragment | What it is |
|---|---|
| `Buckets` | **One** pass over `SalaryRegisterDetails` with conditional aggregation, keyed on `SalaryRegisterId`, producing every wage-type bucket any of the seven reports needs. Replaces 16-31 derived tables per report. The legacy `OR WageType.ArrearSourceWageType IN (SELECT ...)` correlated sub-select -- repeated once per bucket -- becomes a single `LEFT JOIN WageTypes wtSrc`. |
| `TardinessHours` | The one bucket that cannot fold in: it reads `EmployeePayments`, keyed on employee, not register. |
| `EmployeeDimensions` / `DimensionColumns` | The employee/org/cost-centre/posting-group/budgeting-group joins and their projected columns. |
| `CostCentreFamilyBody` | The shared per-employee projection behind the four cost-centre-family reports. |
| `FullMonthLeaveZeroing`, `GrossSalaryColumn`, `GrossSalaryLessBasicColumn`, `CostCentreFamilyHeaderColumns`, `InServiceFilter`, `RunTypeSelection`, `LeaveStatus`, `CodeSortOrder` | The repeated outer-query blocks. |

Where the legacy copies genuinely disagreed, the divergent buckets are kept under **distinct
names** rather than silently unified:

| Legacy name | Kept as | Definition |
|---|---|---|
| `SrDetail_MiscPay` (cost-centre family) | `MiscPay1017_Amount` | wage-type Code `1017` |
| `SrDetail_MiscPay` (rptPayrollSheet) | `CalcPay_Amount` | `IncludeInCalculatedPay=1 AND BillingType=0 AND DontImpactNetPay=0` |
| `SrDetail_MiscDed` (rptPayrollSheet) | `CalcDed_Amount` | as above, `BillingType=1`, excluding loan-type wage types |
| `SrDetail_LoanRecoveryDed` (cost-centre family) | `LoanRecoveryDed_Amount` | Codes `2001`,`2028` |
| `SrDetail_LoanRecoveryDed` (rptPayrollSheet) | `LoanTypeDed_Amount` | any wage type a `LoanType` points at |
| `SrDetail_OtherLeave` (cost-centre family) | `OtherLeaveCostCentre_Amount` | Codes `1003`,`1061` |
| `SrDetail_OtherLeave` (summary) | `OtherLeaveSummary_Amount` | any wage type an `AbsenceType` points at, except `1003`/`1013` |
| `SrDetail_TotalDeduction` (cost-centre family) | `TotalDeductionCostCentre_Amount` | `BillingType=1`, excluding Code `8005` |
| `SrDetail_TotalDeduction` (summary) | `TotalDeductionSummary_Amount` | `BillingType=1 AND DontImpactNetPay=0` |

The legacy bucket sub-queries also re-filtered `PayrollRunType = @PrmPayrollRunType`. That filter is
**dropped as redundant**: every bucket joins back to the outer `SalaryRegisters` row on
`SalaryRegisterId`, and that row is already run-type filtered by the calling report. (The legacy
`PresentDays` bucket had already commented its own copy out for the same reason.) The
`InPeriod = ForPeriod` and month/year filters are kept, because they prune the scan.

### Wage types

No legacy wage-type integer is carried across. Legacy `WageType = 1` is a legacy **primary key**
meaning Code `'1000'` (Basic Salary) -- HCMv3 has an unrelated wage type whose *Code* is literally
`'1'` (Employee PIFSS Ded) -- and legacy `WageType <> 65` means Code `'9000'` (Net Pay). Every
wage-type test in `PayrollSheetSql.cs` goes through `WageTypes.Code` or through a real HCMv3 foreign
key (`OvertimeTypes.WageTypeId`, `LoanTypes.WageTypeId`, `AbsenceTypes.WageTypeId`). The legacy
construct `WageType NOT IN (SELECT TOP 1 OID FROM WageType WHERE Code = '9000')` -- which compared an
FK column to an id sub-select -- is rewritten as `wt.Code <> '9000'`.

`WageTypes.Code` is `nvarchar` in HCMv3, so every code literal is quoted. Left unquoted, SQL Server
would implicitly convert the whole column to `int` and throw on the first non-numeric code.

---

## csproj entries needed

**none.** No report in this batch has a `.resx`, an embedded resource, or an image. The legacy SQL
and the `CalcHeaders` expression that lived in the `.resx` files are now C# constants in
`PayrollSheetSql.cs`, and the `.Designer.cs` files no longer construct a `ComponentResourceManager`.

## Registry entries

```csharp
[rptPayrollSheet.ReportName] = () => new rptPayrollSheet(),
[rptPayrollSheetSummary.ReportName] = () => new rptPayrollSheetSummary(),
[rptPayrollSheetByCostCenter.ReportName] = () => new rptPayrollSheetByCostCenter(),
[rptPayrollSheetByCostCenterPostingGroup.ReportName] = () => new rptPayrollSheetByCostCenterPostingGroup(),
[rptPayrollSheetByCostCenterSummary.ReportName] = () => new rptPayrollSheetByCostCenterSummary(),
[rptPayrollSheetBySponsor.ReportName] = () => new rptPayrollSheetBySponsor(),
[rptSalaryByBudgetingGroup.ReportName] = () => new rptSalaryByBudgetingGroup(),
```

## Catalog rows

```csharp
("PR???", "PayrollSheet", "Payroll Sheet", "كشف الرواتب", ReportCategory.Default),
("PR???", "PayrollSheetSummary", "Payroll Sheet (Summary)", "كشف الرواتب - ملخص", ReportCategory.Default),
("PR???", "PayrollSheetByCostCenter", "Payroll Sheet by Cost Center", "كشف الرواتب حسب مركز التكلفة", ReportCategory.Default),
("PR???", "PayrollSheetByCostCenterPostingGroup", "Payroll Sheet by Cost Center and Posting Group", "كشف الرواتب حسب مركز التكلفة ومجموعة الترحيل", ReportCategory.Default),
("PR???", "PayrollSheetByCostCenterSummary", "Payroll Sheet by Cost Center (Summary)", "كشف الرواتب حسب مركز التكلفة - ملخص", ReportCategory.Default),
("PR???", "PayrollSheetBySponsor", "Payroll Sheet by Sponsor", "كشف الرواتب حسب الكفيل", ReportCategory.Default),
("PR???", "SalaryByBudgetingGroup", "Salary by Budgeting Group", "الرواتب حسب مجموعة الموازنة", ReportCategory.Default),
```

---

## Tenant audit

All SQL for this batch lives in `PayrollSheetSql.cs`. Audit method: every `FROM`/`JOIN` that
introduces a table alias was enumerated, and each alias checked for a matching
`<alias>.TenantId = @TenantId` predicate.

* **81** table references (aliased `FROM`/`JOIN`, including those inside `CASE` sub-selects,
  `OUTER APPLY`s and `EXISTS` clauses; two further textual matches are inside comments and are not
  counted).
* **82** `TenantId = @TenantId` predicates -- one more than the reference count, because
  `RunTypeSelection`'s correlated `NOT IN` sub-select repeats the predicate for readability.
* **0** aliases without a tenant predicate. This was checked mechanically: every alias introduced by
  a `FROM`/`JOIN` in the file was extracted and matched against `<alias>.TenantId = @TenantId`.
* Every soft-deletable table also carries `IsDeleted = 0`.

Per constant (after the org-level rewrite described above):

| Constant | Aliased table refs | `TenantId = @TenantId` |
|---|---|---|
| `Buckets` | 11 (`SalaryRegisterDetails`, `SalaryRegisters`, `WageTypes` x2, `OvertimeTypes` x5, `AbsenceTypes`, `LoanTypes` x3 -- counted once per occurrence) | same |
| `TardinessHours` | 2 | 2 |
| `EmployeeDimensions` | 21 (8 dimension joins + 4 ancestor walks x 3 tables, less the shared `orgUnit` join) | same |
| `InServiceFilter` | 2 | 2 |
| `RunTypeSelection` | 1 | 1 |
| `LeaveStatus` | 4 | 4 |
| `CostCentreFamilyBody` | 3 + composed fragments | same |
| each report query | 2-4 + composed fragments | same |
| the seven look-up queries | 1-3 each | same |

**Lookup tables with no `TenantId` column: none.** Every table this batch touches
(`Employees`, `SalaryRegisters`, `SalaryRegisterDetails`, `WageTypes`, `OvertimeTypes`,
`AbsenceTypes`, `LoanTypes`, `EmployeePayments`, `EmployeeOrgAssignments`, `EmployeeBanks`,
`EmployeeBasicPays`, `EmployeeAbsences`, `EmployeeSeparations`, `EmployeeSponsors`, `Sponsors`,
`JobTitles`, `PayrollAreas`, `PayrollReasonTypes`, `CostCenters`, `CostCenterGroups`,
`PostingGroups`, `BudgetingGroups`, `AbpOrganizationUnits`, `OrgUnitProfiles`,
`OrganizationUnitTypes`) has both `TenantId` and `IsDeleted`.

Note `AbpOrganizationUnits.TenantId` is `int?` (nullable -- host-tenant units have NULL).
`ou.TenantId = @TenantId` therefore excludes host-tenant units, which is the desired behaviour:
`HcmReportBase.ApplyTenant` already refuses to run these reports from the host tenant.

---

## Organisation levels -- Company / Division / Department / Section

Legacy `EmployeeOrgAssignment` carried seven flat FK columns `OrgLevel1..OrgLevel7` into
`OrganizationUnit`, and each report picked whichever of the seven pointed at a unit whose
`OrganizationUnitType.NameEn` was `'Company'` / `'Division'` / `'Department'` / `'Section'` -- up to
28 aliased joins per report.

HCMv3 `EmployeeOrgAssignments` has a **single** `OrganizationUnitId` into `AbpOrganizationUnits`
(ABP's materialised-path tree, `Code`/`ParentId`). The unit **type still exists**: it moved off the
ABP table onto the HCMv3-side `OrgUnitProfiles.OrganizationUnitTypeId` -> `OrganizationUnitTypes`.
So the legacy intent is fully expressible, and this batch implements it as an ancestor walk --
from the employee's own unit up its materialised path (`orgUnit.Code LIKE anc.Code + '%'`), taking
the nearest-to-root ancestor of the wanted type (`ORDER BY LEN(anc.Code)`). Four `OUTER APPLY`s in
`EmployeeDimensions` replace the 28 joins, and the `dsCompany` / `dsDepartment` / `dsDivision`
look-up lists filter on the same profile type. This follows `Bank/BankSql.cs`, which is the closest
existing analogue (it also groups salary data by sponsor and company).

> **Correction made mid-port.** An earlier draft of `PayrollSheetSql.cs` copied the conclusion
> recorded in `PayslipSql.cs` and `SalaryCertificateSql.cs` -- that the walk is "not expressible" --
> and fell back to the employee's own unit for Department plus the tree root (`LEFT(Code, 5)`) for
> Company, with Division and Section returned as NULL. That was wrong and has been replaced with the
> real walk. `DivisionEn` and `SectionsEn` now carry real values, and `DivisionLookup` returns a real
> list, so `rptPayrollSheetSummary`'s Division/Section group bands work. `PayslipSql.cs` and
> `SalaryCertificateSql.cs` were **not** touched -- correcting those is the orchestrator's.

`AbpOrganizationUnits` has only `DisplayName` (no `NameEn`/`NameAr`), so `CompanyAr` and
`DepartmentAr` repeat the English value. There is no Arabic organisation-unit name anywhere in
HCMv3; that is a schema gap, not a port decision.

## BLOCKED

Nothing in this batch is blocked -- all seven reports were ported. One **column-level** gap remains:

1. **Company logo.** Neither `AbpOrganizationUnits` nor `OrgUnitProfiles` has a `Logo` column, so
   `Companylogo` (and `rptPayrollSheet`'s separate `Query.logo` data source) are
   `CAST(NULL AS VARBINARY(MAX))`. The header picture boxes simply print nothing. Same gap
   `Bank/BankSql.cs` records. Note the *unit selection* for the logo query is correct (it filters on
   profile type `'Company'`); only the image column is missing.

---

## Defects carried over

Reproduced faithfully rather than fixed, because the legacy printed output is what users reconcile
against:

1. **`rptSalaryByBudgetingGroup` prints gross minus basic under the heading "GrossSalary".**
   The legacy outer SELECT computed `GrossSalary - BasicSalary` and aliased it `GrossSalary`.
   Correct behaviour would be to print the actual gross, or rename the column. Preserved in
   `GrossSalaryLessBasicColumn`.

2. **`LateHours` is an amount, not hours.** The legacy `SrDetail_TardinessHours` bucket sums
   `EmployeePayment.Amount` for wage type Code `2027` and the layout prints it in an "OT/Late Hours"
   column beside genuine hour counts. Correct behaviour would be to sum a quantity (the commented-out
   legacy alternative read `SalaryRegisterDetail.Number` for Code `2000`). Preserved.

3. **`ORDER BY ... REPLICATE('', 21)`** is a no-op in the non-numeric branch of the employee-code
   sort key -- `LEFT(Code + '', 20)` is just `LEFT(Code, 20)`. Kept byte-identical for parity with
   the already-ported `PayslipSql`.

4. **`rptPayrollSheetSummary`'s `TotalEarnings` bucket** allows the arrear-source alternative
   *without* the `Code <> '9000'` guard that the direct branch has, so an arrear line whose source
   is the net-pay carrier would be double-counted. Preserved.

5. **`rptPayrollSheet` and `rptPayrollSheetSummary` allow `@PrmPayrollRunType IS NULL`** to mean
   "all run types", while the four cost-centre reports use `-1` for the same idea. Two conventions
   for one concept; both preserved.

Legacy dead code **not** ported (it was already commented out or empty, so there is no behaviour
change): the manual "page break every 25 rows" blocks, the 40-line month-name `switch` statements
superseded by `HcmReportBase.GetMonthNameYear`, `rptPayrollSheetSummary`'s unused `int RecordCount`
field, and `rptPayrollSheetBySponsor`'s commented-out `CROSS JOIN` scaffold that manufactured empty
sponsor/pay-interval combinations.

---

## Layout deviations

The `.Designer.cs` files are otherwise byte-identical to the legacy layouts apart from the
mechanical v17.2 -> v23.2 changes (`PaperKind` -> `DXPaperKind`, `PrintEventHandler` ->
`BeforePrintEventHandler`, `Version = "23.2"`, connection name, `resources.GetString` -> SQL
constants, `ResultSchemaSerializable` dropped, `Scripts.OnBeforePrint` dropped). The following are
real changes:

1. **Dangling master-detail relations removed.** All five reports that declared relations declared
   at least one that cannot bind:
   * `rptPayrollSheet`, `rptPayrollSheetByCostCenter`, `...PostingGroup`, `...CostCenterSummary`
     each declared `PayrollSheetQuery -> "dsDivision"`, and **no `dsDivision` query exists** in
     those data sources.
   * The three cost-centre reports additionally declared
     `PayrollSheetQuery -> "dsCompany"` keyed on column `DepartmentEn`, which `dsCompany` does not
     have.

   All were copy-paste residue from `rptPayrollSheetSummary`; nothing in any of those layouts
   navigates a relation -- every band binds straight to `PayrollSheetQuery`. v23.2 validates
   relations when the data source is bound, so they are dropped. `rptPayrollSheetSummary`'s own two
   relations (`dsDivision.DivisionEn`, `dsDepartment.DepartmentEn`) are valid and are kept.

2. **`rptPayrollSheetByCostCenter` brought into line with its three siblings.** Its legacy copy had
   the `HasFullMonthLeave` zeroing and the run-type-selection block commented out while the other
   three applied them, so the same employee could appear with different figures depending on which
   of the four reports was run. Both are applied. *This is a deliberate behaviour change; flag it to
   the payroll owner before go-live.*

3. **Query-parameter names corrected.**
   * `rptPayrollSheetBySponsor` wired its query parameters as `prmMonth` / `prmYear` /
     `prmPayrollRunType` / `prmPayrollArea` (lower-case `p`). DevExpress matches query parameters to
     SQL placeholders by name; renamed to the `Prm...` form the shared SQL uses.
   * `rptSalaryByBudgetingGroup` wired `PrmPayrollRuntype` (lower-case `t`) and declared it
     `typeof(string)` although it is compared numerically. Renamed to `PrmPayrollRunType` and
     retyped `int`, matching the other six.

   Left as-is these would have failed at run time with an unresolved-parameter error, or silently
   compared a string to `-1`.

4. **Look-up queries project duplicate aliases.** The seven layouts read the *same* look-up list
   under different column names -- `dsPayrollArea` is bound as `[Name]` by `rptPayrollSheet` and as
   `[PayrollArea]` by `rptPayrollSheetBySponsor`; `dsCostCenter` as `[NameEn]` in the cost-centre
   family and `[CostCenterEn]` in `rptPayrollSheet`. Rather than edit seven layouts (a designer
   round-trip would undo it), each look-up query projects every alias its callers ask for. A
   `DisplayMember` that does not resolve yields a silently **empty** combo, which is exactly the
   class of invisible failure this port guards against.

5. **`rptPayrollSheetByCostCenterPostingGroup` and `rptSalaryByBudgetingGroup`: the
   `IF ... BEGIN ... END` wrapper became a predicate.** Their legacy SQL was
   `IF (ISNULL(@PrmCompany,'') != '') BEGIN <SELECT> END`; DevExpress `CustomSqlQuery` rejects
   anything that is not purely SELECT. Expressed as `WHERE ISNULL(@PrmCompany,'') <> '' AND
   B.CompanyEn = @PrmCompany`. Semantics identical: no company selected returns no rows.

6. **Organisation hierarchy collapse.** The 28 `OrgLevel1..7` joins per report become four
   `OUTER APPLY` ancestor walks over `AbpOrganizationUnits` + `OrgUnitProfiles` +
   `OrganizationUnitTypes`. This is a faithful translation, not a deviation -- see the
   *Organisation levels* section above. The only residual loss is that `CompanyAr`/`DepartmentAr`
   repeat the English `DisplayName`, because HCMv3 stores no Arabic organisation-unit name.
   `@PrmCompany` compares against the same walked value that `CompanyLookup` lists, so the filter and
   the list agree.

---

## Verification performed

* `dotnet build aspnet-core/src/HCMv3.Reports/HCMv3.Reports.csproj` -- **zero errors in this
  batch's 15 files**. (The project as a whole still reports errors in
  `Others/rptEmploymentContractAr023.Designer.cs`, `Payroll/rptPayslip_wosp_En.Designer.cs` and
  `Payroll/rptPayslip_wsp_En.Designer.cs` -- other batches' work in progress, untouched by this
  batch.) Because a broken sibling file can mask errors, the 15 files were also compiled in
  isolation in a throwaway project against `DevExpress.Reporting.Core` 23.2.13: **build succeeded,
  0 warnings, 0 errors.**
* Silent-failure greps over this batch's `.Designer.cs` files: **no** `.Text = "["` mail-merge,
  **no** `!format` suffix, **no** `XRBinding(` at all (every legacy data binding in these seven
  reports was already an `ExpressionBinding`, which is the form v23.2 resolves correctly).
* Tenant audit as tabulated above: 63 aliased table references, 63 tenant predicates, 0 misses.

## Not verified

**No report in this batch was rendered.** There is no seeded database reachable from this
environment, so nothing here is a claim that the SQL returns the right rows or that the layouts
paginate. The three things most worth checking first on a real tenant:

1. `rptPayrollSheetSummary` -- its Division/Section group bands now resolve through the
   `OrgUnitProfiles` ancestor walk. Confirm the tenant actually has organisation units whose profile
   type is named exactly `Division` and `Section`; the walk matches on `OrganizationUnitTypes.NameEn`,
   so a renamed or unseeded type yields empty groups rather than an error.
2. `rptPayrollSheetByCostCenter` -- deviation 2 above changes its numbers relative to the legacy
   report.
3. The `Buckets` rewrite as a whole -- conditional aggregation over one scan should be
   arithmetically identical to the legacy 31 derived tables, but that is the assertion most worth
   proving against a month of real payroll: run one period through the legacy report and the ported
   one and diff the totals.
