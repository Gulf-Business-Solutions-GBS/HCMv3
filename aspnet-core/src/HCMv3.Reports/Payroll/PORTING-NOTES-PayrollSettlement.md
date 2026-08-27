# Porting notes -- batch `PayrollSettlement`

Legacy source: `C:\GBS\HCM\HRMS_Production\HCM10.Module\Reports\Payroll\`
Target: `aspnet-core\src\HCMv3.Reports\Payroll\`

Files created by this batch (and ONLY these):

| File | Kind |
|---|---|
| `LeaveSettlementSql.cs` | shared SQL for the whole family |
| `rptLeaveEncashment.cs` / `.Designer.cs` | parent report |
| `rptLeaveSettlement.cs` / `.Designer.cs` | parent report |
| `rptLeaveSettlementAr.cs` / `.Designer.cs` | parent report |
| `srptLeaveSettlementEarningEn.cs` / `.Designer.cs` | subreport |
| `srptLeaveSettlementEarningAr.cs` / `.Designer.cs` | subreport |
| `srptLeaveSettlementDeductionEn.cs` / `.Designer.cs` | subreport |
| `srptLeaveSettlementDeductionAr.cs` / `.Designer.cs` | subreport |
| `srptLeaveSettlementWorkingDaysSummaryEn.cs` / `.Designer.cs` | subreport |
| `PORTING-NOTES-PayrollSettlement.md` | this file |

Nothing else in `Payroll\` was touched. `rptPayslip*`, `srptPaySlip*`, `PayslipSql.cs`,
`PayslipArSql.cs` were read for reference only.

---

## csproj entries needed

**none.**

No `.resx` was ported. The legacy `.resx` files carried exactly two kinds of entry --
`customSqlQuery1.Sql` and `<datasource>.ResultSchemaSerializable`. The SQL moved into
`LeaveSettlementSql.cs` as string constants, and `ResultSchemaSerializable` was removed
(dropped in v23.2). None of the eight legacy `.resx` files contained a
`System.Drawing.Bitmap` entry, so the image-store workaround was not needed.

---

## Registry entries

Only the three PARENT reports. Register these:

```csharp
[rptLeaveEncashment.ReportName]   = () => new rptLeaveEncashment(),
[rptLeaveSettlement.ReportName]   = () => new rptLeaveSettlement(),
[rptLeaveSettlementAr.ReportName] = () => new rptLeaveSettlementAr(),
```

### DO NOT register the subreports

The five ported `srptLeaveSettlement*` classes **must NOT be added to the factory
registry**. Each is instantiated directly by its parent's designer
(`this.<name>.ReportSource = new HCMv3.Reports.Payroll.<name>();`) and is never resolved
by name through the storage extension. They each carry a `public const string ReportName`
purely so the report-name space stays unique and collision-checkable; the constants are
deliberately not wired anywhere:

* `srptLeaveSettlementEarningEn`             -- `"LeaveSettlementEarningEn"`
* `srptLeaveSettlementEarningAr`             -- `"LeaveSettlementEarningAr"`
* `srptLeaveSettlementDeductionEn`           -- `"LeaveSettlementDeductionEn"`
* `srptLeaveSettlementDeductionAr`           -- `"LeaveSettlementDeductionAr"`
* `srptLeaveSettlementWorkingDaysSummaryEn`  -- `"LeaveSettlementWorkingDaysSummaryEn"`

(The sixth subreport named in the batch brief, `srptLeaveSettlementBookingBalance`, is
BLOCKED and was not ported at all -- see below. There is nothing to register for it
either.)

Note that `srptLeaveSettlementWorkingDaysSummaryEn` is embedded by **both** parents --
the legacy Arabic report reuses the English working-days subreport verbatim. That is
legacy behaviour, reproduced as-is.

---

## Catalog rows

```csharp
("PR???", rptLeaveEncashment.ReportName,   "Leave Encashment",            "صرف بدل الإجازة",        ReportCategory.Default),
("PR???", rptLeaveSettlement.ReportName,   "Leave Settlement",            "تسوية الإجازة",           ReportCategory.Default),
("PR???", rptLeaveSettlementAr.ReportName, "Leave Settlement (Arabic)",   "تسوية الإجازة (عربي)",    ReportCategory.Default),
```

Subreports get no catalog row (they are not independently printable).

---

## Tenant audit

Method: for each SQL constant, every `FROM <Table>` / `JOIN <Table>` occurrence was
counted and compared against the count of `TenantId = @TenantId` predicates. Counts were
produced mechanically from `LeaveSettlementSql.cs` and then hand-checked.

| SQL constant | table refs | `TenantId = @TenantId` predicates | verdict |
|---|---:|---:|---|
| `PayrollArea`                | 1  | 1  | OK |
| `LeaveSettlementMasterEn`    | 46 | 46 | OK |
| `LeaveSettlementMasterAr`    | 46 | 46 | OK |
| `LeaveSettlementEarningEn`   | 8  | 8  | OK |
| `LeaveSettlementEarningAr`   | 8  | 8  | OK |
| `LeaveSettlementDeductionEn` | 8  | 8  | OK |
| `LeaveSettlementDeductionAr` | 8  | 8  | OK |
| `LeaveSettlementWorkingDays` | 4  | 4  | OK |
| `LeaveAccrualQuery`          | 44 | 44 | OK |

**Every table reference in every query carries `TenantId = @TenantId`. There are no
exemptions -- no lookup table in this family lacks a `TenantId` column.** All nine
constants also carry `IsDeleted = 0` on every reference; every table touched is an ABP
`FullAuditedEntity`.

Per-constant table breakdown (reference count per table):

**`LeaveSettlementMasterEn` / `LeaveSettlementMasterAr`** (identical shape, 46 refs each)
`AbsenceTypes` x13, `EmployeeAbsences` x7, `EmployeeAbsAdjustments` x6, `WageTypes` x4,
`SalaryRegisterDetails` x3, and x1 each of `Employees`, `SalaryRegisters`,
`EmployeeOrgAssignments`, `JobTitles`, `PayrollAreas`, `EmployeeGroups`,
`EmployeeSubGroups`, `AbpOrganizationUnits`, `EmployeeBanks`, `Banks`,
`EmployeeBasicPays`, `EmployeeBasicPayDetails`, `Currencies`.

The high `AbsenceTypes` count is the paid/unpaid split: the legacy query filters leave
rows through `AbsenceType IN (SELECT OID FROM AbsenceType WHERE IsPaid = 1|0)` in
thirteen separate places (four derived joins in the inner block, nine correlated
sub-selects in the outer block). Each of those thirteen sub-selects independently carries
its own tenant predicate -- a miss on any one of them would let another tenant's absence
types classify this tenant's leave.

**`LeaveAccrualQuery`** (44 refs)
`Employees` x6, `WageTypes` x5, `AbsenceTypes` x5, `SalaryRegisterDetails` x4,
`TimeTransfers` x2, `EmployeeAbsAdjustments` x2, and x1 each of `SalaryRegisters`,
`EmployeeOrgAssignments`, `JobTitles`, `PayrollAreas`, `WorkLocations`, `CostCenters`,
`AbpOrganizationUnits`, `EmployeeBasicPays`, `EmployeeBasicPayDetails`, `Currencies`,
`EmployeeSponsors`, `Sponsors`, `EmployeeBanks`, `Banks`, `PayrollReasonTypes`,
`EmployeeAbsences`, `EmployeeAbsenceQuotas`, `TimeRegisters`, `EmployeeAttendances`,
`AttendanceTypes`.

Watch item: the two `TimeTransfers` references are the
`(SELECT MIN(tt.TransferDate) FROM TimeTransfers tt ...)` floor used by the accrual and
absence sub-blocks. In the legacy single-tenant SQL that was a global `MIN` over the whole
table. Both are tenant-filtered here, which is the *correct* behaviour but is a real
semantic change: the accrual window now starts at this tenant's earliest transfer, not the
system-wide earliest. Listed again under "Layout / semantic deviations".

**Earning / Deduction subreports** (8 refs each)
`WageTypes` x2 (the billing-type join plus the basic-pay-currency `OUTER APPLY`), and x1
each of `Employees`, `SalaryRegisters`, `SalaryRegisterDetails`, `EmployeeBasicPays`,
`EmployeeBasicPayDetails`, `Currencies`.

**`LeaveSettlementWorkingDays`** (4 refs)
`SalaryRegisters`, `Employees`, `SalaryRegisterDetails`, `WageTypes` -- one each.

### Parameter wiring

Every report constructor calls `EnsureHcmParameters()` (the three parents pass
`withLoggedUserName: true`). That injects the hidden `PrmTenantId` parameter defaulting to
`0` -- a tenant id matching nothing, so a caller who forgets `ApplyTenant` gets an empty
report rather than another tenant's payroll -- and auto-wires the `TenantId`
`QueryParameter` into every query whose SQL text contains the literal `@TenantId`. The
literal spelling `@TenantId` is used everywhere; no variant spellings exist in this batch.

---

## BLOCKED

### `srptLeaveSettlementBookingBalance` -- not ported

The legacy subreport's only reason to exist is `AbsenceRequest.BookingBalance`, reached as:

```
LEFT  JOIN WorkflowRequest ON WorkflowRequest.OID = EmployeeAbsence.WfRequest
RIGHT JOIN AbsenceRequest  ON AbsenceRequest.OID  = WorkflowRequest.OID
                          AND AbsenceRequest.BookingBalance IS NOT NULL
```

What is missing in HCMv3:

* There is no `AbsenceRequest` entity and no `AbsenceRequests` table. (`WorkflowRequests`
  *does* exist, but the self-service leave-request subtype does not -- this is the known
  missing-module family called out in the shared brief: `LeaveRequest`, `AbsenceRequest`,
  `ReturnFromLeaveRequest`.)
* `BookingBalance` does not exist as a column **anywhere** in
  `HCMv3DbContextModelSnapshot.cs` -- verified by a whole-schema grep, and by a
  whole-source grep over `aspnet-core/src` which returns no hit outside my own comments.
* `EmployeeAbsences` has no `WfRequest`/`WorkflowRequestId` foreign key, so even the join
  path to a workflow request does not exist.

Every projected column of that subreport except `Employee Code` / `Employee Name` /
`ForPeriod` / `InPeriod` comes from the missing side of the join, so there is no useful
partial port. No table was invented and no column was silently dropped -- the whole
subreport is left out.

**Consequence for `rptLeaveSettlement`:** the `XRSubreport` control that hosted it was
removed from the Detail band (see "Layout deviations"). Nothing renderable is lost *today*:
the legacy code-behind set `srptLeaveSettlementBookingBalance.Visible = false` at the top of
every `BeforePrint` and only flipped it to `true` when it found an `AbsenceRequest` with a
non-zero `BookingBalance` -- which in HCMv3 can never happen.

**To unblock:** port the `AbsenceRequest` workflow entity (with `BookingBalance`) and the
`EmployeeAbsence -> WorkflowRequest` link, then re-port this subreport and re-add the
subreport control at `LocationFloat(10.857, 714.14)` / `SizeF(385.945, 149.195)` in
`rptLeaveSettlement`'s Detail band.

Nothing else in this batch is blocked. The other three reports and five subreports are
complete ports, not stubs.

---

## Defects carried over

Legacy bugs reproduced faithfully rather than fixed:

1. **`@PrmEncashment` query parameter is declared but unused.** Both leave-settlement
   masters declare a `PrmEncashment` `QueryParameter` that the SQL never references (it is
   only meaningful in the payslip's `HeaderText`). Kept for parameter-list parity with the
   legacy layout; harmless.

2. **The English master's `PaidLeaveDays` uses `>=` where every sibling uses `>`.** The
   first correlated adjustment sub-select filters
   `eaa.EffectiveDate >= A.LeaveEndDate` while the four structurally identical sub-selects
   around it use `>`. That inconsistency is in the legacy SQL. Left as-is because changing
   it would silently change a paid-leave figure; flagged for a product decision.

3. **`AdditionalFractionPaidLeave` and the `PaidLeaveDays` addend compute different
   things.** The projected `AdditionalFractionPaidLeave` column uses `>` and the addend
   folded into `PaidLeaveDays` uses `>=` (defect 2), so the "additional fraction" a user
   sees on the report can differ from the amount actually added to the total. Legacy
   behaviour, preserved.

4. **`REPLICATE('', 21)` in the ORDER BY sort key is a no-op** (it replicates the empty
   string). Kept verbatim for byte parity with the legacy sort.

5. **`rptLeaveSettlement` embedded a customer-specific subreport in the base report.** The
   legacy base `rptLeaveSettlement.Designer.cs` sets
   `srptLeaveSettlementEarningEn.ReportSource = new srptLeaveSettlementEarningEn_Custom031()`
   -- a Custom031 variant leaking into the shared parent. **NOT carried over**: the port
   uses the base `srptLeaveSettlementEarningEn`. This is deliberate (the batch brief scopes
   out every `*_CustomNNN` file) and is the one legacy behaviour this batch knowingly
   changed. If a tenant depended on the Custom031 earnings layout, it needs its own ported
   variant.

Legacy defects **fixed** rather than carried:

6. **`rptLeaveEncashment.lblUserlogin` printed the literal string `"xrLabel2"`.** The
   legacy class declared `CurrentUserName` / `UserNameLogin` fields and never assigned
   either to the label, so its design-time placeholder text shipped to production. The port
   binds the label to the `PrmLoggedUserName` parameter, matching every other ported report.

7. **`rptLeaveSettlement`'s BeforePrint handler ran once per document, not once per
   employee.** The legacy XPO handler read `PrmEmpCode` and stamped six labels; on a
   payroll-area run covering many employees, every page therefore showed the *last*
   evaluated employee's leave dates and day counts. Replaced by per-row expression bindings
   (see below), which fixes this as a side effect.

---

## Layout deviations

1. **`rptLeaveSettlement`: the `srptLeaveSettlementBookingBalance` subreport control is
   removed** from the Detail band. Required by the BLOCKED item above (the C# would not
   compile with a `ReportSource` pointing at a class that does not exist). Band height,
   page break and every other control are unchanged, and the removed control sat below the
   earning/deduction blocks, so nothing reflows. Legacy default visibility was `false`.

2. **Six labels are now data-bound instead of code-assigned.** In both settlement parents,
   `lblLeaveStartDate`, `lblLeaveEndDate`, `lblExpReturnDate`, `lblPaidLeave`,
   `lblUnpaidLeave` and `lblPaidLeave2` had no bindings at all in the legacy designer -- an
   XPO `BeforePrint` handler assigned `.Text` directly. The port adds
   `ExpressionBinding("BeforePrint", "Text", "[<Column>]")` onto the master-query columns
   that already carry those exact values (`LeaveStartDate`, `LeaveEndDate`,
   `ExpReturnDate`, `PaidLeaveDays`, `UnpaidLeaveDays`). Position, size, font and borders
   are untouched. The three date labels get `TextFormatString = "{0:dd/MM/yyyy}"`, matching
   the legacy handler's `ToString("dd/MM/yyyy")`; the numeric labels keep the designer's
   existing `"{0:#0.00}"`, which is the format the handler applied by hand.
   *v23.2 note:* an `XRLabel.Text = "[Column]"` mail-merge silently renders blank for data
   fields, which is why these are expression bindings and not mail-merge strings.

3. **Department / Division / Section resolution changed shape.** Legacy resolved these from
   seven flat FK columns `OrgLevel1..7` on `EmployeeOrgAssignment`, picking whichever unit
   had `OrganizationUnitType.NameEn = 'Department'` (or `'Division'` / `'Section'` /
   `'Company'`) -- 14 aliased joins in the settlement master, 28 in the encashment query.
   HCMv3 has a single `EmployeeOrgAssignments.OrganizationUnitId` into
   `AbpOrganizationUnits`, an ABP materialised-path tree (`Code`/`ParentId`) with **no
   unit-type concept at all**. "Which ancestor is the Department" is not expressible.
   * `Department` now returns the employee's own organisation unit's `DisplayName`.
   * `Division` and `Section` (encashment report only) return `NULL`.
   **SEMANTIC DEVIATION -- needs a product decision.** This matches the deviation already
   accepted in the committed `PayslipSql.PayslipOrgV2Query`, so the two report families stay
   consistent.

4. **`CompanyLogo` is always `NULL`.** Legacy sourced `OrganizationUnit.logo` through the
   same OrgLevel1..7 fan-out. `AbpOrganizationUnits` has no `Logo` column and HCMv3 has no
   `Company` entity carrying one. The column is returned as
   `CAST(NULL AS VARBINARY(MAX))` so the `xrPictureBox1` `[CompanyLogo]` binding still
   resolves and the layout is byte-identical -- the box simply prints empty.
   **FEATURE LOST**, same as the committed payslip.

5. **`LeaveAccrualQuery`: the accrual window floor is now per-tenant.** See the tenant-audit
   watch item. `(SELECT MIN(TransferDate) FROM TimeTransfers)` became
   `(SELECT MIN(tt.TransferDate) FROM TimeTransfers tt WHERE tt.TenantId = @TenantId AND tt.IsDeleted = 0)`.
   Tenant isolation is non-negotiable, so this had to change; recorded here because it can
   move a leave balance.

6. **`LeaveAccrualQuery`: a dead legacy derived join was dropped.** The legacy query built a
   `TimeTransfer` derived table computing `SUM(TransferCount) AS OpeningBal` over
   `TimeTransferType.NameEn IN ('Annual Leave Opening Balance')` and then never referenced
   `OpeningBal` in the balance expression. Removing it changes no output. If the opening
   balance was *supposed* to be part of `Act_Balance`, that is a pre-existing legacy bug and
   a product decision, not a porting one.

7. **`EmployeeVisaInfo` / `EmployeeGratuity` / `EmployeeSponsor` / `EmployeeSeparation`
   joins dropped from the settlement master.** No column from any of them is projected and,
   as `LEFT JOIN`s to 1:N tables, they could only fan the master row out. Legacy bug, not
   ported. (`EmployeeSponsors`/`Sponsors` *are* kept in the encashment query, where
   `Sponsor.MOSALID` is genuinely projected -- there they are rewritten as a `TOP 1`
   `OUTER APPLY` so they cannot fan out.)

8. **`PayrollArea` look-up query changed from `SelectQuery` to `CustomSqlQuery`.** Both
   settlement parents used a designer `SelectQuery` over the table `PayrollArea` (columns
   `Code`, `Name`). A `SelectQuery` cannot express `TenantId = @TenantId AND IsDeleted = 0`,
   so it became a `CustomSqlQuery` carrying rewritten SQL. The data-member name is unchanged
   (`"PayrollArea"`), so the `PrmPayrollArea` `DynamicListLookUpSettings` binding still
   resolves. Same treatment as the committed payslip.

9. **`ROW_NUMBER() OVER (PARTITION BY ...) ... WHERE Row_Num = 1` derived tables became
   `TOP 1 ... OUTER APPLY`.** Applies to the employee bank, sponsor and basic-pay-currency
   look-ups. Same "one most-recent row" intent, expressed so the optimiser sees the
   correlation. No output change.

### v23.2 API breaks applied

Mechanical, across all eight designer files:

* `System.Drawing.Printing.PaperKind` -> `DevExpress.Drawing.Printing.DXPaperKind`
* `PrintEventHandler(PrintEventArgs)` -> `BeforePrintEventHandler(CancelEventArgs)`
  (both settlement parents)
* `Scripts.OnBeforePrint = "..."` -- removed (both settlement parents)
* `<datasource>.ResultSchemaSerializable = resources.GetString(...)` -- removed (all eight)
* `customSqlQuery1.Sql = resources.GetString(...)` -> `LeaveSettlementSql.<Const>`
* `ComponentResourceManager resources` declaration removed (it had no remaining consumer)
* `ConnectionName = "HCM10ConnectionString"` ->
  `HCMv3.Reports.HcmReportBase.DesignTimeConnectionName`
* `this.Version = "17.2"` -> `"23.2"`
* No `SqlDataSource` `BeginInit`/`EndInit` pairs existed in these files, so none had to be
  removed. The report-level `ISupportInitialize` pairs are kept.
* No `Margins` exceeds its `PageHeight`/`PageWidth`, so nothing throws. The largest is
  `srptLeaveSettlementWorkingDaysSummaryEn`'s `Margins(0, 596, 0, 0)`, which has no explicit
  page size and therefore sits inside the 850x1100 default.

Runtime SQL restrictions handled:

* `DECLARE @PaySlipDate varchar(10) = ...` at the head of the settlement master and the
  accrual query is **inlined** as `DATEFROMPARTS(@PrmYear, @PrmMonth, 1)` at each use site
  -- `CustomSqlQuery` rejects anything that is not purely `SELECT`. Semantics identical
  (legacy built the string `'yyyy-M-01'`). Note this is *first-of-month*, unlike the
  payslip family's `EOMONTH` -- that difference is in the legacy SQL and is preserved.
* No `WITH`, no `BEGIN...END`, no `REPLACE()` anywhere in this batch, so no further
  restructuring was needed.

### Wage-type translation (the trap)

Every legacy wage-type integer was translated through the **Code**, never carried across:

| Legacy | Meaning | Ported as |
|---|---|---|
| `EmployeeBasicPayDetail.WageType = 1` | raw PK; HRMS_RCF row = Code `'1000'` (Basic Salary) | `WageTypes.Code = '1000'` |
| `SRdetail.WageType <> 65` | raw PK; HRMS_RCF row = Code `'9000'` (Net Pay) | `wt.Code <> '9000'` |
| `EmployeeAbsAdjustment.AbsenceType = 1` | raw PK = the 'Annual Paid Leave' absence type | `AbsenceTypes.NameEn IN ('Annual Paid Leave')` |

Carrying `WageType = 1` across literally would have selected the HCMv3 wage type whose
*Code* is `'1'` -- Employee PIFSS Deduction -- i.e. a deduction where basic pay was meant.

Values that were **already codes** in the legacy SQL carry over unchanged, but are now
quoted because `WageTypes.Code` is `nvarchar` in HCMv3 (unquoted, SQL Server would
implicitly convert the whole column to `int` and throw on the first non-numeric code):
`'1000'`, `'1013'`, `'1052'`, `'4000'`, `'4001'`, `'4007'`, `'4008'`, `'4009'`, `'4011'`,
`'4014'`, `'9000'`.

Enum literals verified against `HCMv3.Core.Shared` rather than assumed -- `AdjustmentType`
(`Encashment = 0`, `Adjust = 1`, `Fractional = 2`, `NoImpactOnQuota = 3`), `QuotaType`
(`AnnualLeave = 1`) and `BillingType` (`Earning = 0`, `Deduction = 1`) all match their
legacy numeric values exactly, so `AdjustmentType = 2`, `AdjustmentType = 0`,
`AdjustmentType <> 3`, `QuotaType = 1`, `BillingType = 0|1` carry over as integers safely.

---

## Verification performed

1. **Build** -- `dotnet build aspnet-core/src/HCMv3.Reports/HCMv3.Reports.csproj`:
   **zero errors and zero warnings attributable to any file in this batch**, on both
   target frameworks (`net8.0` and `net8.0-windows`). The build as a whole does not
   currently succeed, but every reported diagnostic is a `CS0115`
   (`Dispose(bool)` -- no suitable method to override, i.e. a base class not yet swapped to
   `HcmReportBase`) or `CS0234` (`DevExpress.Persistent`) in another batch's in-progress
   files: `rptPayslip_wosp_En`, `rptPayslip_wsp_En`, `rptSocialSecurity`, and a set of
   `Others\*` reports. A filter of the full build log for `Leave` returns nothing.

2. **Silent-failure greps over this batch's designer files** -- all clean:
   * `grep -n '\.Text = "\['` -- no hits (no mail-merge data bindings).
   * `grep -n '!format'` -- no hits.
   * `grep -rn 'XRBinding('` -- 8 hits, in `srptLeaveSettlementDeductionEn/Ar` and
     `srptLeaveSettlementWorkingDaysSummaryEn`. **All eight are query-qualified**
     (`"Query.Wage Type"`, `"Query.Number"`, `"Query.Rate"`, `"Query.calcAmount"`,
     `"Query.Currency"`). Two of them qualify a column name containing a space
     (`Query.Wage Type`); per the shared brief this is confirmed-working and was
     deliberately left alone.
   * No residual `HCM10`, `resources.`, `ResultSchemaSerializable`, `Scripts.` or
     `System.Drawing.Printing.PaperKind` anywhere in this batch.

3. **Tenant audit** -- mechanical count, table refs vs `TenantId = @TenantId` predicates,
   per SQL constant. See the table above: all nine constants balance exactly, with no
   exempt lookup tables.

4. **NOT done: render test.** No report in this batch has been rendered against a live
   database. The layouts compile and the SQL is syntactically consistent, but the
   silent-failure classes that only a render catches -- an expression binding onto a column
   name that does not exist in the result set, `GetCurrentColumnValue` returning
   `DBNull.Value`, a subreport parameter binding that fails to resolve -- have **not** been
   ruled out. The six newly added expression bindings in the two settlement parents
   (deviation 2) are the highest-risk item: they are the only bindings in this batch that
   did not exist in the legacy layout, and they point at master-query columns
   (`LeaveStartDate`, `LeaveEndDate`, `ExpReturnDate`, `PaidLeaveDays`, `UnpaidLeaveDays`)
   whose names should be verified against a real result set on the first render.
