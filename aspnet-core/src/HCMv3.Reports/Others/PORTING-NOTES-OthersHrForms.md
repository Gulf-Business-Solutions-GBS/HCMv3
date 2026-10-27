# Porting notes — batch `OthersHrForms`

Nine base reports ported from `HCM10.Module\Reports\Others` into
`HCMv3.Reports\Others`. All nine compile; the reports project builds with **zero errors in
this batch's files** (the only errors left in the project at hand-off are in
`Others/rptSalaryIncrementLetter.Designer.cs`, which is not mine).

| Legacy report | Status |
|---|---|
| `rptClearanceForm` | complete |
| `rptFinalClearance_EOS` | complete |
| `rptDisciplinaryForm` | complete **except** its subreport — see BLOCKED |
| `rptEmployeeWarningNotice` | complete |
| `rptProbationaryReportForm` | complete |
| `rptPayChange` | complete |
| `rptEmploymentContract` | complete |
| `rptEmploymentContractAr023` | complete |
| `rptEmploymentContractEn023` | complete |

**Not render-tested.** Nothing in this batch was executed against a database or rendered to a
document. Everything below is derived from reading the legacy source, the HCMv3 EF Core model
snapshot, and the already-ported reference reports.

Files created by this batch (and nothing else was touched):

```
OthersHrFormsSql.cs                            OthersHrFormsImageStore.cs
OthersHrFormsClearanceFormBackground.jpg       OthersHrFormsDisciplinaryFormBackground.jpg
OthersHrFormsProbationaryFormBackground.jpg    OthersHrFormsDisciplinaryMark.png
OthersHrFormsFinalClearanceEosLogo.png
rpt{ClearanceForm,FinalClearance_EOS,DisciplinaryForm,EmployeeWarningNotice,
     ProbationaryReportForm,PayChange,EmploymentContract,
     EmploymentContractAr023,EmploymentContractEn023}{.cs,.Designer.cs}
PORTING-NOTES-OthersHrForms.md
```

---

## csproj entries needed

Five images were extracted byte-for-byte out of the legacy `.resx` files (they were stored with
the `bytearray.base64` mimetype, i.e. the raw JPEG/PNG file, not a serialised object) and are
loaded at run time by `Others/OthersHrFormsImageStore.cs`. They need to ship as embedded
resources:

```xml
<EmbeddedResource Include="Others\OthersHrFormsClearanceFormBackground.jpg" />
<EmbeddedResource Include="Others\OthersHrFormsProbationaryFormBackground.jpg" />
<EmbeddedResource Include="Others\OthersHrFormsDisciplinaryFormBackground.jpg" />
<EmbeddedResource Include="Others\OthersHrFormsDisciplinaryMark.png" />
<EmbeddedResource Include="Others\OthersHrFormsFinalClearanceEosLogo.png" />
```

The resource names the image store looks up are
`HCMv3.Reports.Others.<filename>`, so the files must stay in `Others\` with these exact
names. If you prefer a wildcard, `Others\OthersHrForms*.jpg` + `Others\OthersHrForms*.png`
matches all five and nothing else.

> Note: `rptDisciplinaryForm`'s `.resx` held **eight** stamp images (`pbDED_04`, `pbDED_08`,
> `pbDED_16`, `pbDED_24`, `pbDED_Sep`, `pbDED_Warn`, `pblast`, `pbLast2`). All eight are the same
> 7,921-byte PNG — verified by SHA-256. One copy ships and all eight picture boxes load it.

## Registry entries

```csharp
[rptClearanceForm.ReportName]            = () => new rptClearanceForm(),
[rptFinalClearance_EOS.ReportName]       = () => new rptFinalClearance_EOS(),
[rptDisciplinaryForm.ReportName]         = () => new rptDisciplinaryForm(),
[rptEmployeeWarningNotice.ReportName]    = () => new rptEmployeeWarningNotice(),
[rptProbationaryReportForm.ReportName]   = () => new rptProbationaryReportForm(),
[rptPayChange.ReportName]                = () => new rptPayChange(),
[rptEmploymentContract.ReportName]       = () => new rptEmploymentContract(),
[rptEmploymentContractAr023.ReportName]  = () => new rptEmploymentContractAr023(),
[rptEmploymentContractEn023.ReportName]  = () => new rptEmploymentContractEn023(),
```

The `ReportName` values are `ClearanceForm`, `FinalClearanceEos`, `DisciplinaryForm`,
`EmployeeWarningNotice`, `ProbationaryReportForm`, `PayChange`, `EmploymentContract`,
`EmploymentContractAr023`, `EmploymentContractEn023`. Checked against every `ReportName`
already in the project — no collisions.

`rptEmploymentContractAr023` and `rptEmploymentContractEn023` both call
`EnsureHcmParameters(withLoggedUserName: true)`, so the host must also call
`ApplyLoggedUserName(...)` on those two, not just `ApplyTenant(...)`.

## Catalog rows

```
("PR???", "ClearanceForm",           "Clearance Form",              "نموذج إخلاء طرف",              ReportCategory.Default),
("PR???", "FinalClearanceEos",       "Final Clearance (EOS)",       "المخالصة النهائية لنهاية الخدمة", ReportCategory.Default),
("PR???", "DisciplinaryForm",        "Disciplinary Form",           "نموذج جزاء",                   ReportCategory.Default),
("PR???", "EmployeeWarningNotice",   "Employee Warning Notice",     "إشعار إنذار موظف",             ReportCategory.Default),
("PR???", "ProbationaryReportForm",  "Probationary Report Form",    "نموذج تقرير فترة التجربة",      ReportCategory.Default),
("PR???", "PayChange",               "Pay Change",                  "تغيير الراتب",                  ReportCategory.Default),
("PR???", "EmploymentContract",      "Employment Contract",         "عقد عمل",                      ReportCategory.Default),
("PR???", "EmploymentContractAr023", "Employment Contract (Arabic)","عقد عمل (عربي)",               ReportCategory.Default),
("PR???", "EmploymentContractEn023", "Employment Contract (English)","عقد عمل (إنجليزي)",           ReportCategory.Default),
```

---

## Tenant audit

Counted mechanically over each SQL constant in `OthersHrFormsSql.cs`: every `FROM <table>` /
`JOIN <table>` reference against every `TenantId = @TenantId` predicate.

| SQL constant | table refs | `TenantId = @TenantId` | match |
|---|---:|---:|---|
| `ClearanceFormQuery` | 7 | 7 | ✅ |
| `FinalClearanceEosQuery` | 7 | 7 | ✅ |
| `DisciplinaryFormQuery` | 8 | 8 | ✅ |
| `EmployeeWarningNoticeQuery` | 12 | 12 | ✅ |
| `ProbationaryReportFormQuery` | 4 | 4 | ✅ |
| `PayChangeQuery` | 12 | 12 | ✅ |
| `PayChangeCompanyQuery` | 3 | 3 | ✅ |
| `PayChangeCostCenterQuery` | 1 | 1 | ✅ |
| `PayChangePayrollAreaQuery` | 1 | 1 | ✅ |
| `EmploymentContractQuery` | 10 | 10 | ✅ |
| `EmploymentContractWageQuery` | 5 | 5 | ✅ |
| `EmploymentContractAr023Query` | 14 | 14 | ✅ |
| `EmploymentContractEn023Query` | 13 | 13 | ✅ |

**No lookup table in this batch lacks a `TenantId` column** — there is nothing to exempt.
Every table touched (`Employees`, `EmployeeOrgAssignments`, `JobTitles`, `WorkLocations`,
`Nationalities`, `Titles`, `EmployeeBasicPays`, `EmployeeBasicPayDetails`, `WageTypes`,
`Currencies`, `PayrollAreas`, `CostCenters`, `EmployeeManagers`, `EmployeeSponsors`,
`EmployeeDocuments`, `DocumentTypes`, `EmployeeDisciplinaries`, `DisciplinaryTypes`,
`DisciplinaryDeductionTypes`, `OrgUnitProfiles`, `OrganizationUnitTypes`,
`AbpOrganizationUnits`) carries both `TenantId` and `IsDeleted`, and both are filtered on every
reference. `AbpOrganizationUnits.TenantId` is `int?` rather than `int`; the equality predicate is
still correct for tenant-owned rows and correctly excludes host-level units.

Every report constructor calls `EnsureHcmParameters()`, so the `TenantId` `QueryParameter` is
auto-wired on every query whose text contains the literal `@TenantId` — which is all thirteen.

---

## BLOCKED

**`rptDisciplinaryForm` — subreport not ported (partial).**
`xrSubreport1` pointed at `HCM10.Module.Reports.Others.srptDisciplinaryFormTotalCount`, which is
not in this batch's report list and has not been ported by anyone. The subreport control keeps its
legacy position, size and all four `ParameterBinding`s, but has **no `ReportSource`**, so it
renders as empty space where the "total disciplinary count" block used to print. Assigning the
`ReportSource` is a one-line change in `rptDisciplinaryForm.Designer.cs` once that subreport
exists.

Nothing else in the batch is blocked. In particular, none of these nine reports reads
`LeaveRequest` / `AbsenceRequest` / `ReturnFromLeaveRequest`, so the known missing self-service
modules did not bite.

---

## Defects carried over

Legacy bugs reproduced faithfully rather than fixed, plus the three places I deliberately did
**not** reproduce one.

### Reproduced

1. **`rptFinalClearance_EOS` / all three contracts — dead `CalcTotalDays` calculated field.**
   `DataMember = "LeaveFormQuery"` (a query these reports do not have) and the expression reads
   `[Parameters].[PrmStartDate]` / `[PrmEndDate]` (parameters they do not declare). Nothing binds
   it. Kept because deleting it is a layout change. *Correct behaviour: delete the field.*

2. **`rptPayChange` — ~20 dead calculated fields copied from the payslip report.**
   `OFFCurrentMonthCal`, `SLCal`, `CalNetPay`, `CalEarning`, `CalDeduct`, `CalcForPeriodMonth`,
   `CalcInPeriodMonth`, `CalcDateTime`, `CalcHeaders`, `CalcNetPay`, `CalcForGetMonth`, … all
   carry `DataMember`s (`"PayslipQuery"`, `"SalaryRegisterDetail"`, `"Employee.Absence"`,
   `"Query"`) that name no query on this report's data source, and `CalcHeaders.Expression`
   reads `[Parameters].[PrmPayrollRunType]` / `[PrmPeriodMonth]` / `[PrmPeriodYear]`, none of
   which this report declares. No control binds any of them.
   **Risk flagged:** if v23.2 eagerly compiles calculated-field expressions at
   `CreateDocument()` rather than lazily on first use, these will throw. That is the single most
   likely runtime failure in this batch and is exactly what a render test would catch first.
   *Correct behaviour: delete all of them.*

3. **`rptDisciplinaryForm` — `pblast` and `pbLast2` are unconditionally hidden.**
   The legacy `BeforePrint` set `.Visible = false` on both every time, after the six penalty
   stamps were resolved. Looks like an abandoned feature. Reproduced.

4. **`rptEmploymentContractAr023` — Arabic amount-in-words is missing its currency agreement
   and its fractional part.** `MyXtraReport.AmountInArabicWords` guarded both of those blocks on
   the fields `_intergerValue` and `_decimalValue`. `_intergerValue` was **never assigned
   anywhere in the legacy class**, and the method shadowed `_decimalValue` with a *local* of the
   same name — so both fields were always `0` and both blocks were unreachable. The printed
   sentence has always been `(فقط <words> دينار كويتي).` with no فلس part and no
   دنانير/ديناراً agreement. Reproduced exactly, including the two stray spaces the method emits
   from its never-assigned prefix/suffix properties (the caller's
   `Regex.Replace(@"\s+", " ")` exists precisely to mop those up).
   *Correct behaviour: assign `_intergerValue`/`_decimalValue` and let both blocks run.*
   The whole region now lives in `rptEmploymentContractAr023.cs`; `HcmReportBase` deliberately
   deferred it ("port them with the first Arabic report that needs them"), and this is that
   report. **Promote the region into `HcmReportBase` verbatim when a second Arabic report needs
   it** — I could not, because the base class is off-limits to this batch.

5. **`rptEmploymentContractEn023` — a blank `PrmEmpCode` prints a contract for every employee.**
   The legacy filter is the tri-state `(@PrmEmpCode <> '' AND Code = @PrmEmpCode) OR
   @PrmEmpCode = ''`. Kept verbatim. (`rptPayChange` has the same tri-state on three filters,
   which there is intentional.)

6. **`rptEmploymentContract*` — `EmployeeWageType` groups by currency short form.** An employee
   whose basic-pay lines are in two currencies yields two rows and the contract prints twice.
   Legacy shape, kept.

7. **Dead `LeaveFormReportDS` field.** `rptFinalClearance_EOS` and all three contracts declare a
   second `SqlDataSource` field that `InitializeComponent` never instantiates. It produces
   `warning CS0169: the field ... is never used` (four warnings from this batch). Kept — the same
   legacy artefact already produces the identical warning in `rptSickLeaveForm`,
   `rptPassportInOut` and `GovtForms/rptEndorsmentForVisitVisa`, so it should be swept
   project-wide, not per batch. *Correct behaviour: delete the field.*

8. **Copy-paste query names.** `rptFinalClearance_EOS` and all three contracts name their data
   source `SickLeaveFormDS` and their query `SickLeaveFormQuery`;
   `rptProbationaryReportForm` uses `EmpDiscReportDS` / `EmpDiscReportQuery`. All are legacy
   leftovers from other reports. Kept, because `DataMember` binds them by name.

### Deliberately NOT reproduced

9. **`rptPayChange` — `SELECT TOP 1 OID FROM WageType WHERE Code IN (a,b)`.** The Transport
   (`1002`,`1035`) and Housing (`1001`,`1048`) buckets each matched a `TOP 1` over a two-code
   list, so one of the two wage types was silently dropped — which one depended on storage
   order. The ported query matches the full code list, which is what the surrounding `SUM()` and
   the entire `Prev_*` half of the same legacy query already did. **This changes output** for any
   employee using both codes; it makes the two halves of the comparison agree.

10. **`rptEmploymentContractEn023` — three fan-out joins dropped.** The legacy query
   `LEFT JOIN`ed `Sponsor` → `SponsorSignatory` → `Signatory` and projected **nothing** from any
   of the three. `SponsorSignatory` is one-to-many, so the contract printed once per authorised
   signatory. Dropped. **This changes output**: it removes duplicate copies of the contract and
   nothing else. (Contrast `GovtFormsSqlFirstHalf` rewrite note 6, which kept the same fan-out —
   there at least one column of the fanned-out tables was projected.)

11. **`rptClearanceForm` — client-side `FilterString` moved into SQL.** The legacy report set
    `FilterString = "[Code] In (?PrmEmpCode)"` over a raw `SqlDataSource` *and had its
    `PrmEmpCode` `QueryParameter` commented out*, so it fetched **every employee row in the
    database** and discarded all but one at the report engine. Replaced by
    `emp.Code = @PrmEmpCode` in SQL, with the commented-out `QueryParameter` restored. In a
    multi-tenant system the legacy form is not merely slow, it is a data-exposure shape.

---

## Layout deviations

1. **`rptDisciplinaryForm`** — the `srptDisciplinaryFormTotalCount` subreport renders empty. See
   BLOCKED.

2. **`Companylogo` is always NULL** on `rptFinalClearance_EOS`, `rptEmployeeWarningNotice` and
   all three contracts. `AbpOrganizationUnits` has no `Logo` column and `OrgUnitProfiles` does not
   carry one either, so any picture box bound to `[Companylogo]` renders empty —
   `rptFinalClearance_EOS.xrPictureBox1` is the one that visibly does.
   **FEATURE LOST**; same gap `PayslipSql` and `BankSql` both record. Needs a schema decision.

3. **`rptEmploymentContractEn023.lblBasicSalaryInWords`** — the legacy designer left the literal
   placeholder `Text = "[KD in Words]"` on it. That is not a mail-merge field (no such column
   exists) and v23.2 would print it verbatim if the band handler ever failed to run. Blanked;
   `Detail_BeforePrint` assigns the label unconditionally, as the legacy handler did.

4. **`rptPayChange`** — `Scripts.OnBeforePrint = "rptPayslipEn_BeforePrint"` dropped, per the
   standing rule. The named script did not exist on this report anyway; another payslip leftover.

5. **Report-level `BeforePrint` moved to the Detail band** on `rptDisciplinaryForm`,
   `rptEmploymentContractAr023` and `rptEmploymentContractEn023`. Report-level `BeforePrint`
   fires once, *before any data row is current*, so those handlers could only work at all because
   they ran their own XPO query; now that their inputs come from the printed row, the band-level
   handler is the correct place. No visual change on a single-row document.

6. **Empty handlers deleted** along with their wiring: `rptProbationaryReportForm_BeforePrint`
   (body `{ }`), and `rptPayChange`'s three (`Detail_BeforePrint`, `GroupHeader1_BeforePrint`,
   `rptPayChange_BeforePrint` — all empty or entirely commented out).

7. **`.resx` eliminated.** This project ships no `.resx` (a `System.Drawing.Bitmap` entry breaks
   the multi-targeted build). Images moved to `OthersHrFormsImageStore`; SQL moved to
   `OthersHrFormsSql`; static label text, RTF blocks (`SerializableRtfString`, base64-for-base64)
   and one long `ExpressionBinding` were inlined into the designer files verbatim.
   **Side effect:** the Visual Studio report designer shows empty picture boxes on the four
   reports with background scans, because the images are no longer assigned inside
   `InitializeComponent`. The rendered document is unaffected. Same trade-off `GovtFormsImageStore`
   and `SalaryCertificateBackground` already made.

---

## Org-unit handling (mid-flight correction — read this)

I initially wrote the same fallback `PayslipSql.cs` and `SalaryCertificateSql.cs` use — "ABP org
units have no type, so use the employee's own unit" — and then **corrected it** on instruction.

The unit type *does* exist in HCMv3: it lives on `OrgUnitProfiles.OrganizationUnitTypeId` →
`OrganizationUnitTypes`, not on the ABP table. So the legacy seven `OrgLevel1..7` joins collapse
to a proper **ancestor walk**, following `Bank/BankSql.cs`: of the employee's unit and all its
ancestors (ancestor ⟺ the employee unit's `Code` starts with the ancestor's `Code`), take the
shallowest whose profile type is named `'Company'` / `'Department'` / `'SubDepartment'`.
`ORDER BY LEN(anc.Code)` picks the shallowest.

Applied in six queries:

| Query | ancestor type(s) looked up |
|---|---|
| `ClearanceFormQuery` | `SubDepartment` → `DeptEn` / `DeptAr` |
| `FinalClearanceEosQuery` | `Company` → `CompanyEn` |
| `EmployeeWarningNoticeQuery` | `Company` → `CompanyEn` **and** `Department` → `DepartmentEn` (two independent walks, so the two labels resolve separately as they did in the legacy print) |
| `EmploymentContractQuery` | `Company` → `CompanyEn` |
| `EmploymentContractAr023Query` | `Company` → `CompanyEn` / `CompanyAr` |
| `EmploymentContractEn023Query` | `Company` → `CompanyEn` / `CompanyAr` |

`PayChangeCompanyQuery` (the `dsCompany` look-up list) likewise keeps its
`OrganizationUnitType.NameEn = 'Company'` filter rather than listing every unit.

**Arabic org names are not lost either.** `AbpOrganizationUnits` has only `DisplayName`, but
`OrgUnitProfiles` carries a `NameAr`, so every `*Ar` alias takes `OrgUnitProfiles.NameAr` of the
matched ancestor.

`PayslipSql.cs` and `SalaryCertificateSql.cs` still carry the wrong conclusion. I did **not**
touch them — that correction is the orchestrator's to make centrally.

---

## v23.2 API breaks hit in this batch

Everything already on the shared list, plus one addition:

* `PaperKind` → `DXPaperKind` — all nine reports.
* `PrintEventHandler(PrintEventArgs)` → `BeforePrintEventHandler(CancelEventArgs)` — five reports.
* `ResultSchemaSerializable` dropped — all nine.
* `Scripts.OnBeforePrint` dropped — `rptPayChange`.
* **`CustomSqlQuery` rejects SQL starting with `WITH`.** The contract wage query opened with
  `;With CTE1 As(`; rewritten as a plain `SELECT DISTINCT * FROM (...) CTE1 WHERE ...`.
* **`CustomSqlQuery` rejects `REPLACE()` anywhere.** `rptPayChange`'s Remarks column was
  `REPLACE(parts + ',', ',,', '')`. Rewritten as
  `CASE WHEN parts = '' THEN ',' ELSE LEFT(parts, LEN(parts) - 1) END`, which is byte-identical
  on all inputs — including the legacy quirk that an empty part list yields a bare `,` rather
  than an empty string. (Each part ends in a comma and none contains `,,`, so the only match the
  legacy `REPLACE` could ever make was the doubled comma at the very end.)
* The commented-out `--DECLARE @Prm...` preambles on five legacy queries were dropped; they are
  dead text, and on at least one query the first token would then have been `DECLARE`.
* `System.Drawing.StringTrimming` → `DevExpress.Drawing.DXStringTrimming` — **not hit**; checked
  all nine designer files, no `TextTrimming` assignment in this batch.

---

## Other schema substitutions worth a second pair of eyes

* **`rptDisciplinaryForm` reads a different entity.** Legacy `DisciplinaryRequest` is a
  self-service workflow request joined to `WorkflowRequest` for the employee. HCMv3 has no such
  entity; the admin-side `EmployeeDisciplinaries` is used instead (it carries `EmployeeId`
  directly, so the `WorkflowRequest` join disappears). **`PrmRequestOid` therefore now addresses
  `EmployeeDisciplinaries.Id`, not a workflow request id** — whatever launches this report must
  pass the new id.
* **`DisciplinaryRequest.CurrentDeductionCount` has no HCMv3 column.** It is derived as
  `PriorDeductionCount` = the number of *earlier* disciplinaries of the *same type* for the same
  employee. That matches the intent ("this is the Nth offence") but will not reproduce a legacy
  counter that was ever edited by hand. **Needs a product decision.**
* **`DisciplinaryRequest.DisciplinaryDeductionType` was free text**; HCMv3 normalises it into the
  `DisciplinaryDeductionTypes` lookup. The code-behind now compares that lookup's `NameEn`
  against the same six literals the legacy code used (`"Written warning"`, `"Deduction 4 hours"`,
  `"Deduction 8 Hours"`, `"Deduction 16 Hours"`, `"Deduction 24 Hours"`,
  `"Separation from service"` — inconsistent casing included). **If the seeded lookup rows are
  named anything else, no penalty stamp will print.** Worth checking against real data.
* **`Employees.PassportNum` does not exist**; the passport number comes from `EmployeeDocuments`
  joined to the `DocumentTypes` row named `'Passport'`, as `GovtFormsSqlFirstHalf` established.
* **`rptEmploymentContractAr023` / `En023` gained a `Current_Salary` column** so the
  amount-in-words sentence no longer needs an `XPObjectSpaceProvider`. `Ar023` keys the lookup on
  `@PrmFormDate`, exactly as the legacy code-behind did. `En023` uses the header in force *today*
  rather than the header covering the last regular payroll period — the legacy two-step read the
  period from `SalaryRegister` and the amount from `BasicPay`, which disagree whenever a salary
  changed after the last run, and no other column on that contract needs `SalaryRegisters`.
  Consequently the legacy "only print the sentence if a regular payroll run exists" guard is gone;
  the sentence is omitted when the employee has no basic pay in force instead. **Behavioural
  difference, deliberate.**
