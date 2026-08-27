# PORTING NOTES — batch `OthersCertificates`

Legacy source: `C:\GBS\HCM\HRMS_Production\HCM10.Module\Reports\Others\`
Target: `aspnet-core\src\HCMv3.Reports\Others\`

7 assigned reports, all ported and compiling. 3 additional subreports were ported as hard
dependencies (two of the assigned reports embed them and will not compile or render without them).

| Report | ReportName key | Status |
|---|---|---|
| `rptExperienceCertificate` | `ExperienceCertificate` | complete |
| `rptSalaryContinuousCertificate` | `SalaryContinuousCertificate` | complete |
| `rptSalaryContinuousCertificateAr` | `SalaryContinuousCertificateAr` | complete, one feature gap (Arabic amount-in-words) |
| `rptWhomsoeverCertificate` | `WhomsoeverCertificate` | complete |
| `rptEmployeeConfirmationLetter` | `EmployeeConfirmationLetter` | complete |
| `rptSalaryIncrementLetter` | `SalaryIncrementLetter` | complete |
| `rptWarningLetter` | `WarningLetter` | complete |
| `srptWarningLetterCount` | (subreport, no key) | complete — embedded by `rptWarningLetter` |
| `srptWarningLetterTotalCount` | (subreport, no key) | complete — embedded by `rptWarningLetter` |
| `srptEmpProfileWageDetailDatewise` | (subreport, no key) | complete — embedded TWICE by `rptSalaryIncrementLetter` |

## Files created by this batch

Beyond the ten `.cs` / `.Designer.cs` pairs:

* `OthersCertificatesSql.cs` — all 12 SQL constants, plus a second class
  `OthersCertificatesResources` holding the embedded-image resource names, the image loader, and
  two RTF constants. The two classes share a file only to keep this batch's file footprint inside
  the agreed budget; there is no other relationship between them. **If a later pass wants to split
  `OthersCertificatesResources` into its own file, nothing else has to change.**
* Four binary assets extracted from the legacy `.resx` files (byte-identical to the base64 payloads,
  which used the `bytearray.base64` mimetype, i.e. the raw file, not a serialised object):
  * `rptSalaryContinuousCertificateLetterhead.png` (24 535 bytes)
  * `rptSalaryContinuousCertificateSignature.png` (10 381 bytes)
  * `rptWhomsoeverCertificateBackground.jpg` (409 467 bytes)
  * `rptWarningLetterBackground.jpg` (625 209 bytes)
* This file.

No `.resx` files were created — see *csproj entries needed*.

## csproj entries needed

```xml
<ItemGroup>
  <!--
    Letterhead / signature / full-page form scans printed behind four of the OthersCertificates
    documents. Same reasoning as the existing Others, GovtForms and EmployeeInfo blocks: a .resx
    entry typed System.Drawing.Bitmap binds a Windows-only type during resource generation, which
    the plain net8.0 flavour of this project cannot do. Shipped as raw image files, byte-identical
    to what was base64-encoded in each legacy .resx, and attached in each report's constructor;
    see the OthersCertificatesResources class in Others/OthersCertificatesSql.cs.
  -->
  <EmbeddedResource Include="Others\rptSalaryContinuousCertificateLetterhead.png" />
  <EmbeddedResource Include="Others\rptSalaryContinuousCertificateSignature.png" />
  <EmbeddedResource Include="Others\rptWhomsoeverCertificateBackground.jpg" />
  <EmbeddedResource Include="Others\rptWarningLetterBackground.jpg" />
</ItemGroup>
```

Until these land, `OthersCertificatesResources.LoadImage` returns `null` and the four picture boxes
print empty. It never throws, so the reports still render.

## Registry entries

```csharp
[rptExperienceCertificate.ReportName]          = () => new rptExperienceCertificate(),
[rptSalaryContinuousCertificate.ReportName]    = () => new rptSalaryContinuousCertificate(),
[rptSalaryContinuousCertificateAr.ReportName]  = () => new rptSalaryContinuousCertificateAr(),
[rptWhomsoeverCertificate.ReportName]          = () => new rptWhomsoeverCertificate(),
[rptEmployeeConfirmationLetter.ReportName]     = () => new rptEmployeeConfirmationLetter(),
[rptSalaryIncrementLetter.ReportName]          = () => new rptSalaryIncrementLetter(),
[rptWarningLetter.ReportName]                  = () => new rptWarningLetter(),
```

The three `srpt*` classes are deliberately **not** registered: they are only reachable through
`XRSubreport.ReportSource` and are never launched standalone.

## Catalog rows

```csharp
("PR???", "ExperienceCertificate",         "Experience Certificate",          "شهادة خبرة",              ReportCategory.Default),
("PR???", "SalaryContinuousCertificate",   "Salary Continuous Certificate",   "شهادة استمرارية راتب",      ReportCategory.Default),
("PR???", "SalaryContinuousCertificateAr", "Salary Continuous Certificate (Arabic)", "شهادة استمرارية راتب (عربي)", ReportCategory.Default),
("PR???", "WhomsoeverCertificate",         "To Whom It May Concern Certificate", "شهادة لمن يهمه الأمر",   ReportCategory.Default),
("PR???", "EmployeeConfirmationLetter",    "Employment Confirmation Letter",  "خطاب تثبيت الموظف",        ReportCategory.Default),
("PR???", "SalaryIncrementLetter",         "Salary Increment Letter",         "خطاب زيادة الراتب",         ReportCategory.Default),
("PR???", "WarningLetter",                 "Warning Letter",                  "خطاب إنذار",               ReportCategory.Default),
```

## Tenant audit

Counted with the SQL line comments stripped, so the words FROM/JOIN inside a `--` comment do not
inflate the table count. Every query balances exactly:

| SQL constant | table refs | `TenantId = @TenantId` | |
|---|---|---|---|
| `ExperienceCertificateQuery` | 11 | 11 | OK |
| `SalaryContinuousCertificateEnQuery` | 12 | 12 | OK |
| `SalaryContinuousCertificateArQuery` | 12 | 12 | OK |
| `WhomsoeverCertificateQuery` | 19 | 19 | OK |
| `EmployeeConfirmationLetterQuery` | 7 | 7 | OK |
| `SalaryIncrementLetterQuery` | 16 | 16 | OK |
| `EmployeeWageDetailDatewiseQuery` | 5 | 5 | OK |
| `WarningLetterQuery` | 6 | 6 | OK |
| `DisciplinaryTypeListQuery` | 1 | 1 | OK |
| `DisciplinaryCountQuery` | 2 | 2 | OK |
| `WarningLetterTotalCountQuery` | 2 | 2 | OK |
| `WarningLetterSelectedTypeQuery` | 1 | 1 | OK |

Every one of those table references also carries `AND <alias>.IsDeleted = 0`.

**Lookup tables with no TenantId column: none.** Every table touched by this batch
(`Employees`, `Nationalities`, `EmployeeOrgAssignments`, `JobTitles`, `EmployeeBasicPays`,
`EmployeeBasicPayDetails`, `WageTypes`, `Currencies`, `EmployeeSponsors`, `Sponsors`,
`EmployeeBanks`, `Banks`, `EmployeeSeparations`, `EmployeeDutyPeriods`, `DutyPeriods`, `Duties`,
`EmployeeManagers`, `EmployeeDisciplinaries`, `DisciplinaryTypes`, `WorkLocations`,
`AbpOrganizationUnits`, `OrgUnitProfiles`, `OrganizationUnitTypes`) has both `TenantId` and
`IsDeleted`, confirmed against `HCMv3DbContextModelSnapshot.cs`.

Every report constructor — including all three subreports — calls `EnsureHcmParameters()`.
`ApplyTenant` walks nested subreports, so setting it on `rptWarningLetter` /
`rptSalaryIncrementLetter` reaches their children.

## BLOCKED

Nothing in this batch is blocked. All 7 assigned reports port cleanly. One helper is missing (see
below), but it degrades one label rather than blocking a report.

## Defects carried over

Legacy bugs reproduced faithfully rather than fixed:

1. **`srptWarningLetterTotalCount` counts warnings *per day*, not in total.** The legacy `GROUP BY`
   includes `DisciplinaryDate`, so the label "Count: N | Last Committed on: …" shows how many
   warnings of that type were issued on that one date. The correct form would group by employee and
   type only and take `MAX(DisciplinaryDate)`. Changing it would change what the printed document
   says, so it is left as-is and documented on
   `OthersCertificatesSql.WarningLetterTotalCountQuery`.
2. **`rptWarningLetter.DiscCountQuery` is dead.** Nothing in the layout binds it. Kept (tenant-scoped)
   because it is part of the data source.
3. **`rptWarningLetter.calculatedField1` has no expression.** A designer leftover; kept verbatim.
4. **The two legacy warning-letter queries disagreed with each other** about where the disciplinary
   type lives — `EmpDiscReportQuery` joined through `EmployeeDisciplinaryDetail`, `DiscCountQuery`
   read `EmployeeDisciplinary.DisciplinaryType` directly. HCMv3 has no detail table, so both now
   read the header. This is the schema forcing the fix, not a discretionary change.
5. **`rptEmployeeConfirmationLetter`'s parameter is misspelled `PrmEmpCpde`.** Kept: renaming it
   would break any saved layout or catalog row that already refers to it.
6. **`rptSalaryContinuousCertificate*` turn their LEFT JOIN chain into an INNER one.** The legacy
   `WHERE EBP.StartDate <= @PrmDate AND EBP.EndDate >= @PrmDate` sits in the WHERE clause, not on
   the join, so an employee with no basic pay in force on `PrmDate` gets no certificate at all
   rather than a certificate with a blank salary. Preserved exactly.
7. **`rptSalaryIncrementLetter` refreshes `lblMonthlySalary` from two different label handlers.**
   The legacy report needed two entry points because its XPO query keyed off those labels' values;
   here it is simply redundant. Kept so the wiring matches the legacy layout.
8. **`rptSalaryCertificate*`-style `Nationality` quirk does not recur here** — noted only because
   reviewers of the sibling batch may look for it.

Legacy bugs that were fixed, because leaving them would have been worse than a layout deviation:

* **`rptExperienceCertificate` fanned out one page per separation record.** `EmployeeSeparation` was
  LEFT JOINed on `EmployeeId` with no date narrowing, so a rehired-then-separated employee printed
  the certificate twice. Narrowed to the most recent row with `OUTER APPLY ... TOP 1`.
* **`rptSalaryIncrementLetter`'s `PrmMonth` / `PrmYear` query parameters declared their DevExpress
  `Expression` result type as `short` while the report parameters are `int`.** A type mismatch there
  fails at run time. Corrected to `int`.
* **Client-side `FilterString`s replaced by SQL predicates** on `rptWarningLetter`,
  `srptWarningLetterCount` and `srptWarningLetterTotalCount` — see *Layout deviations* below. Over a
  raw `SqlDataSource` a client-side filter fetches every tenant's rows into the report engine before
  discarding them, which is both a performance problem and an isolation hazard.
* **`rptSalaryIncrementLetter`'s `PayrollArea` join projected nothing** and was dropped, matching the
  call `Bank/BankSql.cs` made for the same dead joins.

## Layout deviations

1. **Company / org-level resolution.** Legacy `EmployeeOrgAssignment` carried seven flat FK columns
   `OrgLevel1..OrgLevel7` and each report picked whichever pointed at an `OrganizationUnit` whose
   `OrganizationUnitType` was named `'Company'`. HCMv3 has a single
   `EmployeeOrgAssignments.OrganizationUnitId` into `AbpOrganizationUnits` (an ABP materialised-path
   tree), with the unit *type* on `OrgUnitProfiles.OrganizationUnitTypeId`. The seven aliased joins
   collapse to one ancestor walk — of the employee's unit and all its ancestors, the shallowest whose
   profile type is named `'Company'`. This follows the pattern `Bank/BankSql.cs` established.
   Affects `rptSalaryContinuousCertificate`, `rptSalaryContinuousCertificateAr`,
   `rptEmployeeConfirmationLetter`, `rptSalaryIncrementLetter`.
   *(Note for the orchestrator: `PayslipSql.cs` and `SalaryCertificateSql.cs` concluded the unit type
   was "not expressible" and fell back to the employee's own unit. It IS expressible. Those two ports
   are worth revisiting.)*
2. **FEATURE LOST — `Companylogo`.** `AbpOrganizationUnits` has no `Logo` column and `OrgUnitProfiles`
   does not carry one. Every `Companylogo` column returns `NULL VARBINARY(MAX)` and the bound
   `XRPictureBox` prints empty. Same conclusion `PayslipSql` and `BankSql` reached. Affects
   `rptSalaryContinuousCertificate`, `rptSalaryContinuousCertificateAr`,
   `rptEmployeeConfirmationLetter`, `rptSalaryIncrementLetter`.
3. **FEATURE GAP — Arabic amount-in-words.** `rptSalaryContinuousCertificateAr` called
   `MyXtraReport.AmountInArabicWords(decimal)`, which `HcmReportBase` deliberately did not port
   (it depends on the legacy `CurrencyInfo` type and on arabicOnes/arabicTens tables living outside
   `MyXtraReport.cs`). `lblMonthlySalary` therefore prints the numeric amount inside the legacy
   Arabic wrapper with the spelled-out segment blank. **This is the one visible content regression in
   the batch.** Porting `AmountInArabicWords` into `HcmReportBase` fixes it with a one-line change
   here; it was not attempted because `HcmReportBase.cs` is a forbidden shared file for this batch.
4. **SCHEMA CHANGE — `EmployeeDisciplinaryDetail` is gone.** Legacy modelled a disciplinary event as
   a header with N type lines; HCMv3 allows exactly one type per event
   (`EmployeeDisciplinaries.DisciplinaryTypeId`). `rptWarningLetter` therefore can no longer print
   several types for one event. That capability disappeared with the data model, not with this port.
5. **`rptWarningLetter.PrmEmpCode` and `srptWarningLetterTotalCount.EmpCode` lose `MultiValue = true`.**
   The employee filter moved from a client-side `FilterString` into the SQL, which takes one code.
   The layout prints one employee's letter either way.
6. **Report-level `BeforePrint` re-wired to the Detail band** on both salary-continuation
   certificates. Report-level `BeforePrint` fires once, before any data row is current, so
   `GetCurrentColumnValue` there returns nothing; the legacy handlers only worked because they ran
   their own XPO query. Same call the `rptSalaryCertificateEn` port made.
7. **Report images are no longer assigned inside `InitializeComponent`.** They are attached in each
   constructor from embedded resources. Side effect: the Visual Studio report designer shows empty
   picture boxes. The rendered document is unaffected.
8. **`rptWhomsoeverCertificate`'s duty times.** `Duties.StartTime` / `EndTime` are `time` in HCMv3
   (they were `datetime` in the legacy schema). They are still projected under the legacy aliases
   `FromDate` / `ToDate`, and the labels bound to them carry no `TextFormatString`, so they render as
   a time-of-day string — which is what the certificate wants.
9. **`rptSalaryIncrementLetter` gains `CurrencyCode` / `CurrencyName` columns.** They replace the
   XPO round-trip `GetSalaryAmountText()` made. Pinned to wage-type `Code = '1000'` (Basic Salary)
   rather than "the first detail row", which is order-undefined — the same call
   `SalaryCertificateSql` made.
10. **`rptExperienceCertificate` keeps 39 empty `BeforePrint` handlers.** Every one was a designer
    leftover no-op in the legacy report. Carried over (with the v23.2 `CancelEventArgs` signature)
    rather than deleted, so the layout file stays a line-for-line match.
11. **`rptSalaryIncrementLetter` is not single-employee.** An empty `PrmEmpCode` means "every
    employee with an increment in that month" — legacy behaviour, preserved via
    `emp.Code = CASE WHEN @PrmEmpCode = '' THEN emp.Code ELSE @PrmEmpCode END`, along with the legacy
    `ORDER BY emp.Code`.
12. **`@DtTemp` eliminated.** v23.2 `CustomSqlQuery` rejects SQL starting with `DECLARE`. The legacy
    three-statement preamble computed the last day of `(@PrmYear, @PrmMonth)`, so every `@DtTemp`
    reference became `EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))` — provably the same value, and a
    plain `SELECT`.

## Verification performed

1. `dotnet build aspnet-core/src/HCMv3.Reports/HCMv3.Reports.csproj` — **zero errors in any file
   belonging to this batch**, on both `net8.0` and `net8.0-windows`. The build as a whole still fails
   on four files owned by other in-flight batches (`Others/rptEmploymentContractEn023.Designer.cs`,
   `Payroll/rptPayslip_wosp_En.Designer.cs`, `Payroll/rptPayslip_wsp_En.Designer.cs`,
   `Payroll/rptSalaryByBudgetingGroup.Designer.cs` — missing partial `.cs` files and a stale
   `DevExpress.Persistent` reference). Not touched.
2. Silent-failure greps over this batch's files only — all clean:
   * `\.Text = "\[` (mail-merge that silently fails for data fields) — 0 hits. Every data-bound label
     in this batch uses `ExpressionBindings`, which is what the legacy layouts already did.
   * `!format` — 0 hits.
   * `XRBinding(` — 0 hits (nothing in this batch uses the older binding API).
   * Leftover `HCM10` / `MyXtraReport` / `resources.` / `ResultSchemaSerializable` /
     `System.Drawing.Printing.PaperKind` / `PrintEventHandler` / `Scripts.` in code — 0 hits
     (matches remain only inside XML doc-comments describing what was replaced).
3. Tenant audit — table refs vs `TenantId = @TenantId` predicates counted per SQL constant, all 12
   balance exactly. See the table above.
4. Margins checked against page size on every report (v23.2 throws instead of clamping when margins
   exceed `PageHeight`): all within bounds, none changed from the legacy values.

**NOT verified: nothing in this batch has been render-tested against a live database.** No document
was created, so the run-time-silent failure modes the brief warns about (a binding that resolves to
an empty cell, a `time` column formatting unexpectedly, the `EOMONTH` rewrite on real increment data)
remain unconfirmed by execution. The static greps above are a proxy, not a substitute.
