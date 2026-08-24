# rptPayslipEn -- one report, all three layers, measured

The reference port. Chosen because it is directly comparable to the existing FastReport pilot in
`aspnet-core/src/HCMv3.Application/Reports/ReportsAppService.cs`.

**Status: renders against live HCMv3 data and reconciles with the salary register.**
Employee 3400, June 2024, run type 0, tenant 1:
earnings 233.574 − deductions 40.000 = **193.574 KWD**, which is exactly the wage-type 9000 (Net Pay)
row in `SalaryRegisterDetails` for register 223.

---

## 1. Measured cost, by layer

Hours-equivalent of skilled developer time for **this** report. "One-time" is discovery now encoded
in `Build-PayslipMrt.ps1` and reusable across the corpus; "per-report" is what recurs.

| Layer | One-time | Per-report | Total |
|---|---:|---:|---:|
| **Layout** (.mrt renders correctly) | 5.0 | 3.5 | **8.5** |
| **SQL** (11 queries, 377 lines, returns rows) | 1.5 | 6.0 | **7.5** |
| **Code-behind** (193 lines classified + ported) | 0.5 | 1.5 | **2.0** |
| | **7.0** | **11.0** | **18.0** |

Plus **1–2 h of residual defects still open** (section 5).

### Layout, itemised

| Work | h |
|---|---:|
| Automated `.repx` -> `.mrt` conversion | ~0 (CPU seconds) |
| Diagnosing why a flawless-looking, zero-error `.mrt` rendered every value cell blank | 2.0 |
| Building the 10-entry subreport-page -> data-source map | 0.5 |
| Rewriting 44 field expressions; discovering `{DS["Col With Space"]}` is the only accessor that works (the `_x0020_` form used inside the serialized column list resolves to nothing, silently) | 1.5 |
| Re-inlining 3 DevExpress `CalculatedFields` the importer discards | 1.0 |
| Parameter binding: regenerating typed report variables; the `SqlDbType`=0/BigInt defect; non-zero `Size`. Required decompiling `StiDataParameterConverter` to learn the serialization field order is `Name,Expression,Type,Size` -- the opposite of the `StiDataParameter.Order` enum | 2.0 |
| Restoring dropped `TextFormatString` (two mechanisms needed, because the importer renames controls inside subreports) | 1.5 |

### SQL, itemised

| Work | h |
|---|---:|
| Schema archaeology: column inventory of 25 HCMv3 tables against their legacy counterparts | 1.5 |
| Master query `PayslipOrgV2Query` -- the hard one (below) | 3.0 |
| The other 10 queries | 2.0 |
| `Test-PayslipSql.ps1` harness + reconciling totals against the register | 1.0 |

### Code-behind, itemised

`rptPayslipEn.cs` is 193 lines doing four things:

| Legacy behaviour | Disposition | h |
|---|---|---:|
| `LoggedUserNme.Text = SecuritySystem.CurrentUserName` in the **constructor** | **Ported.** Because it ran in the constructor, the exported `.repx` has the *exporting* user's name baked into the label as static text -- every HCMv3 payslip would be footed `RepxExportUser`. Replaced with a `LoggedUserName` report variable supplied by the caller. | 0.5 |
| Constructor RLS logic: hide the employee-code parameter and pin it to the caller's own employee when the user is neither an admin nor covered by a row-level-security list | **Not ported here -- belongs in the app service, not the layout.** HCMv3 enforces this with ABP permissions + `AbpSession`; baking a visibility rule into an `.mrt` would move an authorization decision into a report file. | 0.5 |
| `PanelSignature.Visible = GetPayrollSettings().PayslipSignature` | **Deferred.** Needs a `ManagePayrollSettings.PayslipSignature` lookup passed in as a variable; one line once the app service exists. | 0.25 |
| `AfterPrint`: render to PDF and SMTP-mail it, plus `BodyTemplateHtml` | **Already ported**, by the FastReport pilot -- `ReportsAppService.SendPayslipEmails` / `BuildPayslipEmailBodyHtml`. Reused as-is. | 0.25 |
| `BeforePrint` | Entirely commented out in the legacy source. Nothing to port. | 0 |
| Classification + writing up the deviations | | 0.5 |

**This layer is cheap here only because the FastReport pilot already did it.** A report whose
code-behind has not already been ported once should be budgeted at 4–6 h, not 2.

---

## 2. What the SQL rewrite actually required

11 queries. Per **query**, the three-way bucket came out:

| Bucket | Queries | Which |
|---|---:|---|
| Mechanical rename | 8 (73%) | `PayrollArea`, `OvertimeQuery`, `BasicPayQuery`, `PayslipIndemnityQuery`, `PayslipWorkingDayQuery`, `PayslipAttendanceQuery`, `LeaveInfoQuery`, `LeaveEncashQuery` |
| Templated rewrite | 2 (18%) | `PayslipEarningQuery`, `PayslipDeductionQuery` -- same shape, but each carries a raw-legacy-PK trap |
| Genuine human rewrite | 1 (9%) | `PayslipOrgV2Query` |

**"Mechanical" is doing a lot of work in that table.** Even the mechanical eight required, on every
single join:

* `Table` -> `Tables` (pluralisation)
* `OID` -> `Id`
* `<EntityName>` -> `<EntityName>Id` (FK columns)
* **`AND TenantId = @TenantId`** -- appears zero times in the legacy corpus
* **`AND IsDeleted = 0`** -- appears zero times in the legacy corpus
* `Code` comparisons quoted, because HCMv3 `WageTypes.Code` is `nvarchar` and an unquoted numeric
  literal makes SQL Server implicitly convert the whole column to `int` and throw on the first
  non-numeric code

None of that is `sed`-able and all of it is silent when wrong. Omit `TenantId` and the report leaks
across tenants; omit `IsDeleted` and it resurrects deleted rows. Neither shows up as an error.

### The two traps that look mechanical and are not

1. `EmployeeBasicPayDetail.WageType = 1` (in five of the queries). This is a **raw legacy primary
   key**, not a business code: `HRMS_RCF.WageType` OID 1 is Code `'1000'`, Basic Salary. HCMv3 has an
   unrelated wage type whose *Code* is literally `'1'` -- **Employee PIFSS Ded**, a deduction.
   Translating the literal mechanically returns a deduction where the query means basic pay, and the
   report still renders, still returns rows, and is simply wrong. Rewritten as `Code = '1000'`.

2. `SRdetail.WageType <> 65` in the deduction query. Also a raw legacy PK: OID 65 = Code `'9000'`,
   Net Pay. It means "do not print the net-pay carrier row as a deduction", exactly mirroring the
   earnings query's `code <> 9000` -- a real rule.
   **The FastReport pilot dropped this filter**, on the stated grounds that 65 is "a raw primary key
   of the legacy database with no stable meaning in HCMv3"
   (`ReportsAppService.cs`, `BuildPayslipDataAsync`). It has a stable meaning; it just needs
   translating. Worth revisiting there.

### The master query

`PayslipOrgV2Query` is the genuine rewrite. What forced it:

| Legacy | HCMv3 | Consequence |
|---|---|---|
| `EmployeeOrgAssignment.OrgLevel1..7` joined 14 times to `OrganizationUnit`, filtered on `OrganizationUnitType` = 'Company' / 'Department' | one `OrganizationUnitId` into `AbpOrganizationUnits`, an ABP materialised-path tree (`Code`/`ParentId`) with **no unit-type concept at all** | 14 joins collapse to 1. "Which ancestor is the Department" is **not expressible**. Uses the employee's own unit. **SEMANTIC DEVIATION -- needs a product decision.** |
| `CompanyLogo` from `OrganizationUnit.logo` | `AbpOrganizationUnits` has no `Logo`; HCMv3 has no Company entity carrying one | **FEATURE LOST.** Returned as `NULL`. |
| `PayrollArea.Code = EmployeeOrgAssignment.PayrollArea` (a **string** join) | `EmployeeOrgAssignments.PayrollAreaId -> PayrollAreas.Id` | Properly modelled. Note this contradicts `ReportsAppService.GuardPayrollAreaFilterNotSupported`, which refuses payroll-area filtering because the column was deferred -- it exists now. |
| `LEFT JOIN EmployeeVisaInfo` / `EmployeeGratuity` | n/a | Dropped: no column from either is projected and, as `LEFT JOIN`s to 1:N tables, they could only fan the master row out. Legacy bug, not ported. |
| `OID IN (SELECT TOP 1 ... ORDER BY ExpiryDate DESC)` correlated subqueries | `OUTER APPLY` | Same intent, correlation visible to the optimiser. |

### Correcting the corpus-wide bucket split

The earlier estimate was ~43% mechanical / ~43% templated / ~14% genuine.

* **Per query, that split is about right** -- this sample came out slightly better (73/18/9), but the
  payslip's subreport queries are unusually simple and the sample is one report.
* **Per report, it materially understates the work.** A report needs a genuine rewrite if *any* of
  its queries does. 274 of 481 reports have 2 or more data sources, so a per-query "genuine" rate of
  even 9% puts most multi-query reports into the genuine bucket. Plan on **~1 genuine rewrite per
  multi-query report**, and treat the 14% figure as applying to queries, not reports.

---

## 3. Corpus complexity -- where this report sits

Measured across all 481 converted reports (`out/corpus-complexity.csv`):

| Metric | Corpus p50 | Corpus p90 | rptPayslipEn | Percentile |
|---|---:|---:|---:|---:|
| SQL lines | 93 | 349 | 377 | **92nd** |
| Data sources | 1 | 4 | 11 | 11 reports match or exceed |
| Pages (subreports) | 1 | 4 | 10 | 11 reports match or exceed |
| Bound expressions | -- | -- | 44 | **93rd** |

Distribution:

| Data sources | Reports | | Pages | Reports |
|---|---:|---|---|---:|
| 1 | 207 | | 1 (no subreports) | 412 |
| 2–3 | 162 | | 2–3 | 14 |
| 4–6 | 75 | | 4–6 | 36 |
| 7+ | 37 | | 7+ | 19 |

**The payslip is not the median report.** 86% of the corpus has no subreports at all, and 43% has a
single data source. The 18 h figure must be scaled down for the bulk of the corpus, not multiplied
across it.

---

## 4. Files

```
reports/PayslipEn/
  queries/                   11 rewritten queries (folder is not named "sql": aspnet-core/.gitignore excludes any directory called sql), one file per data source
  Build-PayslipMrt.ps1       injects SQL, rebinds layout, restores formats, parameterises code-behind
  Test-PayslipSql.ps1        executes every query standalone and reports row counts
  rptPayslipEn.HCMv3.mrt     the deliverable (regenerate; do not hand-edit)
  PORTING-NOTES.md           this file
```

The `.mrt` is **generated**. Every fix belongs in `Build-PayslipMrt.ps1` or in `queries/`, so that
improving the importer and re-running the pipeline does not throw the work away.

---

## 5. Residual defects, still open

| Defect | Severity | Est. |
|---|---|---:|
| `srptPaySlipOvertimeHoursEn` and `srptPaySlipBasicPayEn` render on top of each other. DevExpress grows the band under a subreport; Stimulsoft honours the design-time height. Needs the stacked subreports wrapped in a vertical panel or converted to linked components. | Visible | 1.0 h |
| RATE column too narrow -- `185.000` wraps to two lines. Column widths converted correctly; Stimulsoft's font metrics differ slightly from DevExpress's. | Cosmetic | 0.25 h |
| `xrLabel2` / `xrLabel3` have conflicting `TextFormatString`s across subreports in the `.repx` and the importer's control renaming makes them unresolvable by name. Format not applied. | Cosmetic | 0.25 h |
| `PanelSignature` visibility not wired (see code-behind table). | Functional | 0.25 h |
| `OrgUnit`, `Bank Name`, `Bank Account`, `Passport Exp Date` render blank. **Not a report defect** -- `EmployeeBanks`, `EmployeeDocuments`, `EmployeeSponsors` are empty in HCMv3 and only one `AbpOrganizationUnits` row exists. Blocked on the data migration. | External | -- |
| Stimulsoft **trial watermark**. | Licensing | -- |
