# Off-cycle (Advance Leave) test plan — fixed 26 days, Friday weekend, September 2026 lock → October advance → October/November recovery

Tests how the legacy advance-leave off-cycle was ported to HCMv3: one employee, 260.000 KWD basic on **fixed
26-day billing**, **Friday weekend**, 30 days' leave straddling two months, paid before he travels and
recovered over the two months it covers.

Two configuration facts drive every figure below:

- **Salary ÷ 26.** Wage types use billing method **Fixed days per month**, **Fixed days = 26**, so a paid day is
  worth **260 ÷ 26 = 10.000 KWD** whatever the length of the month.
- **Week-off Fridays are not leave days.** The absence type has **`IncludeWeekend = No`**, so a Friday inside
  the leave stays a week-off. The 30 **calendar** days of leave are therefore **25 paid leave days**.

Every figure is asserted by
[OffCycleAdvanceLeaveScenario_Tests.cs](../../aspnet-core/test/HCMv3.Tests/PayrollManagement/OffCycleAdvanceLeaveScenario_Tests.cs)
(18 tests) with the **real** `PayrollDayCountService` wired in, and the clearing behaviour by
[PayrollAdvancePaymentService_Clearing_Tests.cs](../../aspnet-core/test/HCMv3.Tests/PayrollManagement/PayrollAdvancePaymentService_Clearing_Tests.cs)
(13 tests). Run both before and after any change to the off-cycle path:

```bash
dotnet test aspnet-core/test/HCMv3.Tests/HCMv3.Tests.csproj --filter "FullyQualifiedName~OffCycleAdvanceLeave|FullyQualifiedName~PayrollAdvancePaymentService_Clearing"
```

---

## 1. Master data to set up

| Thing | Value | Why it matters |
|---|---|---|
| Employee | E1, joined before 01/09/2026, active contract, org assignment with a payroll area | Off-cycle refuses to run without an org assignment and payroll area |
| Duty / roster | **Friday weekly off**, Saturday–Thursday working | Sets `TimeRegister.WeeklyOff`, which drives both the salary basis and the leave-day exclusion |
| Basic pay | Wage code **1000**, 260.000 KWD, PayInterval **Monthly** | The scenario's whole salary |
| Wage type 1000 | **FixedDaysPerMonth**, **FixedDays = 26**, **IncludeInAdvancePay = Yes**, DontImpactNetPay = No | Without `IncludeInAdvancePay` the off-cycle writes the line with **Amount 0** and advances nothing |
| Wage type 1050 | Annual Leave Pay, **FixedDaysPerMonth**, **FixedDays = 26**, **IncludeInAdvancePay = Yes** | Leave is `rate ÷ 26 × leave days`. If its FixedDays differs from 1000's, a full month of leave stops summing to a full month of pay |
| **Wage type 4014** | **Present Days**, **FixedDaysPerMonth**, **FixedDays = 26**, Prorate **WorkingDays** | **The one that actually defines "26 days".** See §3.1 — get this wrong and basic pay comes out 0 |
| Wage type 4008 | Weekly Off Days | The week-off count the fixed-days formula subtracts |
| Wage type 2005 | Advance Payment Deduction, Billing type **Deduction**, DontImpactNetPay = **No** | How the advance comes back. If missing, the engine throws `WageType 2005 not found` |
| Wage type 2011 | Last Month Recovery Deduction | Carries a negative net into the next month; cleared on re-run |
| Wage type 9000 | Net Pay, highest `PayrollOrder` | Must run last |
| Absence type | Code **1** Annual Leave, **IsPaid = Yes**, **IncludeWeekend = No**, Wage type = 1050 | `IsPaid` is required or the off-cycle cannot find the leave. **`IncludeWeekend`** is what excludes the Fridays — see §3.2 |
| Wage type leave | Absence type 1 ↔ wage type 1000 | Sets the leave **rate**: the sum of the rates of the wage types flagged here — 260 |
| Payroll control record | Payroll area's PCR, calendar month end **30/09/2026** | The gate for every step below |
| Time register | September, October and November fully evaluated by the Time Engine | The engine refuses to run past `EmployeePayrollStatus.NextTimeEvaluationFrom` |

**Settings left at their defaults**, in particular all four "fixed to actual" switches
(`FixedToActualWhenLeaveOverlapOnMonthStart` / `...OnMonthEnd` / `...WhenUnpaidLeaveFound` /
`...WhenAbsentDayFound`) **off**. Each one flips a month out of the fixed-26 formula into actual-days — see §7.

**The Fridays in this window:** September 4, 11, 18, 25 (**4**); October 2, 9, 16, 23, 30 (**5**);
November 6, 13, 20, 27 (**4**). Five fall inside the leave: 16, 23, 30 October and 6, 13 November.

A pleasing consequence: **every month's salary basis is exactly 26** — September 30 − 4, October 31 − 5,
November 30 − 4 — so the fixed 26 and the real calendar agree all the way through.

---

## 2. The steps

### Step 1 — Lock the PCR for September 2026

Payroll Control Records → September row → set state **Released for payroll** (legacy `PeriodLocked = Yes`).

A regular run is refused in any other state: *"Payroll Control Record not locked or is not current."*

### Step 2 — Run September regular payroll

Payroll Engine screen: run type **Regular**, payroll area, employee code `E1`, pay-in date **30/09/2026**.

**Expected — September salary register (ForPeriod = InPeriod = 30/09/2026, run type Regular):**

| Wage code | Rate | Number | Amount |
|---|---|---|---|
| 4014 Present Days | | 26 | |
| 1000 Basic Pay | 260.000000 | **26** | **260.000000** |
| 9000 Net Pay | | | **260.000** |

The number is **26** — the fixed days — not the 30 calendar days and not the 26 attended days. 10.000 × 26.

No 1050 line (no leave), and **no** 2005 row — a regular run never creates an advance recovery.

### Step 3 — Exit payroll

Same PCR row → set state **Closed** (legacy `PeriodClosed = Yes`).

Two things happen:

- Off-cycle runs become allowed for the *following* period.
- If **"Lock salary register on payroll exit"** is on in payroll settings, September's **regular** payslips for
  this payroll area are locked and can no longer be edited or deleted.

### Step 4 — Apply the leave

Employee absence for E1:

| Field | Value |
|---|---|
| Absence type | Annual Leave |
| Start date | **16/10/2026** |
| End date | **14/11/2026** |
| Total leave days / calendar days | 30 |
| Leave payment type | **Pay In Advance** |
| **Payment date** | **15/10/2026** |

The payment date is the join key. The off-cycle finds the leave by
`LeavePaymentType = PayInAdvance AND PaymentDate = <the off-cycle payment date you type>`, and sets its own end
date from the leave's end date. Get it wrong and the run fails with *"Leaves not found for offcycle payment for
Employee: E1"*.

### Step 5 — Run the off-cycle

Payroll Engine: run type **OffCycle**, employee code `E1` (**mandatory** — blank throws *"No Employee Code
found, Cannot Run OffCycle Payroll Run."*), off-cycle reason **Advance Leave**, off-cycle payment date
**15/10/2026**.

**The pay-in date you type is ignored.** The engine forces it to the *month end of the payment date* —
31/10/2026 — and then re-checks per employee that this is exactly the month after the closed period. An
off-cycle dated two months out is refused (*"Mismatch between Payroll Control Record and Payroll run date"*),
which is what stops a month being skipped.

### Steps 6 and 7 — Run October and November regular payroll as normal

Release each month's PCR for payroll, run Regular, pay-in date 31/10/2026 then 30/11/2026.

---

## 3. What he gets paid

### 3.1 First, how "26 days" is actually computed

On fixed-days billing the engine does **not** divide the month. It asks: *how many of the 26 paid days did he
not spend on leave?*

```
presentDays = 26 − (leaveDays + absentDays)        …when leaveDays + absentDays < calendarDays − weeklyOffs
```

That number lives on wage code **4014 Present Days**, and **where basic pay reads it from differs by run
type** — the single most surprising thing in this path:

- **Regular run** — basic pay reads the **4014 line already on the register**, written earlier in the same run
  by the time-type pre-pass. It does not compute anything itself. So if wage type 4014 is missing, or is not set
  to `FixedDaysPerMonth`, present days are **0 and basic pay is 0** — with no error anywhere.
- **Off-cycle run** — basic pay calls the day-count service directly, once per recovery month, because the
  pre-pass does not cover a future recovery month.

So for this scenario:

| Month | Calendar leave days | Week-off Fridays inside | **Paid leave days** | Present days | Basic pay |
|---|---|---|---|---|---|
| September | 0 | — | 0 | 26 − 0 = **26** | 260.000 |
| October | 16 (16–31 Oct) | 3 (16, 23, 30) | **13** | 26 − 13 = **13** | 130.000 |
| November | 14 (01–14 Nov) | 2 (6, 13) | **12** | 26 − 12 = **14** | 140.000 |

### 3.1a The invariant: a fixed-26 month can never pay more than one month

On fixed-days billing this holds **by construction**, not by a cap:

```
presentDays = 26 − leaveDays          …basic pay  = rate/26 × presentDays
                                       leave pay  = rate/26 × leaveDays
                              ⇒ presentDays + leaveDays = 26 always
```

Basic pay and leave pay are valued at the *same* per-day rate, so whatever the split between working and leave
days, the month adds up to exactly 26 days = 260.000. Every regular month in this plan satisfies it, and so does
the off-cycle's October share. Pinned by `No_Month_Should_Ever_Pay_More_Than_One_Months_Salary`.

**There is one shape where legacy does not hold the line**, and it is worth knowing before you compare figures:
the **actual-days** branch counts attended days rather than `26 − leaveDays`, and its ceiling is
`calendarDays − weekOffs`, which is **27** in a 31-day month whose weekday pattern gives only four Fridays (e.g.
March 2027, which starts on a Monday). A fully-attended such month would pay 27 days — 270.000 against a 260.000
salary.

Legacy has **no general cap**. Its only use of the promisingly-named `enableBasicpayMoreThenFixeddays`
(`Program.cs` line 4812, ported faithfully in `PayrollDayCountService`) is the much narrower
`leaveDays >= 26 ⇒ workingDays = 0`, which does not apply. Pinned by
`Actual_Days_Branch_Can_Exceed_The_Fixed_Days_In_A_Month_With_Four_Weekly_Offs` so the behaviour is visible and
any decision to cap it is deliberate. **This scenario never reaches that branch** — see §3.2a.

### 3.2 Why the Fridays drop out, and where

This is decided by the **Time Engine**, not payroll, and it is a single flag on the absence type.

Legacy `GeneralTimeService.GetLeaveDay` (lines 256–270) — ported in `TimeRegisterFieldCalculator.Leave.cs`:

```csharp
if (daycount != 0 && (… || (LeaveType.IncludeWeekend == false && WeeklyOff == 1) || …))
    daycount = 0;                      // IncludeWeekend = No  -> it is a week-off, not a leave day

if (daycount == 1) {
    if (LeaveType.IncludeWeekend == true) WeeklyOff = 0;   // IncludeWeekend = Yes -> it is a leave day,
}                                                          //   and the week-off is CLEARED
```

So a TimeRegister row is **never** both `LeaveDay = 1` and `WeeklyOff = 1`. That mutual exclusion is
load-bearing: payroll's leave-pay side sums `LeaveDay` with **no** week-off filter of its own
(`EmployeeLeavePayService.GetLeaveDaysMonthWise` line 850), while the present-days side filters
`WeeklyOff == 0`. A row that was both would be paid as leave *and* excluded from present days — the same Friday
paid twice.

| `IncludeWeekend` | A Friday inside the leave | 30 calendar days of leave are |
|---|---|---|
| **No** (this plan) | `WeeklyOff = 1`, `LeaveDay = 0` | **25 paid leave days** (13 Oct + 12 Nov) |
| Yes | `LeaveDay = 1`, `WeeklyOff = 0` | 30 paid leave days |

Note this is the **leave pay** count. What the employee's leave *quota* is debited is a separate question,
governed by the same absence type but read by the accrual service, not by payroll.

### 3.2a `FixedToActualWhenLeaveOverlapOnMonthEnd` is a no-op here — correcting an earlier note

An earlier draft of this plan warned that switching
**`FixedToActualWhenLeaveOverlapOnMonthEnd`** on would push October to 310.000. **That was wrong**, and it came
from an earlier fixture that had *no weekly offs at all*.

With a real Friday weekend the two branches agree:

| Branch | October working days |
|---|---|
| Fixed days | 26 − 13 leave days = **13** |
| Actual days | attended days 01–15 Oct less Fridays 2 and 9 = **13** |

So the setting changes nothing in this scenario, and October stays at exactly one month. Pinned by
`FixedToActual_On_MonthEnd_Overlap_Should_Not_Change_October_When_The_Weekend_Is_Real`.

The reason they agree is that the week-off count (5) makes October's salary basis `31 − 5 = 26`, equal to the
fixed days. Where they diverge is a month whose salary basis is *not* 26 — see §3.1a.

### 3.3 October off-cycle — paid 15/10/2026, before he travels

**The off-cycle pays exactly three things**, and nothing else:

| # | Component | Days | × 10.000 | Where it comes from |
|---|---|---|---|---|
| 1 | **October working days** | 13 | 130.000 | `Program.GetPresentDays`, fixed-days branch: `fixeddays − totalLeavedays` = 26 − 13 |
| 2 | **October leave days** | 13 | 130.000 | `EmployeeLeavePayService` line 730: `(totalBasicPay / FixedDays) × leaveDaysInMonth` |
| 3 | **November leave days** | 12 | 120.000 | same line, second recovery period |
| | **Total** | **38** | **380.000** | |

**November's *working* days are not in the list, and that is correct.** The off-cycle window ends on the leave's
own end date (14/11), so 15–30/11 has not happened yet as far as this run is concerned; November's own regular
run pays it. Inside the off-cycle, November's recovery month falls past the window end, which trips legacy's
`isLeaveEnded` and switches that month to actual working days — 01–14/11 is all leave and Fridays, so it
contributes 0.

**Components 1 and 2 add to exactly 26** — the fixed month — which is why October alone comes to 260.000. That
identity is the quickest coherence check on the configuration: if `1000` and `1050` are given different
`FixedDays`, it breaks.

Salary register: **ForPeriod = InPeriod = 31/10/2026**, run type **OffCycle**, off-cycle payment date
15/10/2026, reason Advance Leave.

| Wage code | Rate | Number | Amount |
|---|---|---|---|
| 1000 Basic Pay | 260.000000 | 13 | **130.000000** |
| 1050 Annual Leave Pay | 260.000000 | 25 | **250.000000** |
| **9000 Net Pay** | | | **380.000000** |

The 1050 rate does **not** change between the two months: 13 days in October and 12 in November are both worth
10.000/day. Under calendar-days billing they would differ (260/31 against 260/30); under fixed-26 they do not.

### 3.4 October regular — end of October

| Wage code | Rate | Number | Amount |
|---|---|---|---|
| 4014 Present Days | | 13 | |
| 1000 Basic Pay | 260.000000 | 13 | 130.000000 |
| 1050 Annual Leave Pay | 260.000000 | 13 | 130.000000 |
| 2005 Advance Payment Deduction | | | **260.000000** |
| **9000 Net Pay** | | | **0.000** |

He earns his whole October salary again and the advance takes all of it back. **Net zero is correct** — the
money reached him on 15/10.

### 3.5 November regular — end of November

| Wage code | Rate | Number | Amount |
|---|---|---|---|
| 4014 Present Days | | 14 | |
| 1000 Basic Pay | 260.000000 | 14 | 140.000000 |
| 1050 Annual Leave Pay | 260.000000 | 12 | 120.000000 |
| 2005 Advance Payment Deduction | | | **120.000000** |
| **9000 Net Pay** | | | **140.000** |

Only November's share of the advance is recovered, so he is paid the 14 present days he was back at work.

### 3.6 The books balance

| Run | Paid |
|---|---|
| September regular | 260.000 |
| October off-cycle (15/10) | 380.000 |
| October regular (31/10) | 0.000 |
| November regular (30/11) | 140.000 |
| **October + November total** | **520.000 = 2 × 260** |

If this total is not exactly two months' salary, someone has been paid twice or short-paid.

---

## 4. How the advance recovery is calculated and split

The recovery is **not** a single deduction. During the off-cycle run, every rule that pays money also posts a
matching `EmployeePayment` row for wage code **2005**, dated the **1st of the month that money belongs to**. The
later regular run for that month picks the row up as an ordinary deduction.

The recovery months come from `GetRecoveryPeriods(payDate, offCycleEndDate)` — one entry per calendar month from
the run month's start to the leave's end date. Here: **01/10/2026** and **01/11/2026**.

Then, per month:

| Rule | Recovery month | Posts | Running total on the row |
|---|---|---|---|
| Basic pay (1000) | 01/10/2026 | 130.000000 | 130.000000 |
| Leave pay (1050) | 01/10/2026 | 130.000000 | **260.000000** |
| Basic pay (1000) | 01/11/2026 | 0 → **no posting** (window closes 14/11, all leave and Fridays) | — |
| Leave pay (1050) | 01/11/2026 | 120.000000 | **120.000000** |

**Resulting 2005 `EmployeePayment` rows:**

| Employee | Payment date | Off-cycle payment date | Amount | Recovered by |
|---|---|---|---|---|
| E1 | 01/10/2026 | 15/10/2026 | 260.000000 | October regular run |
| E1 | 01/11/2026 | 15/10/2026 | 120.000000 | November regular run |

Both rows are flagged `IsSystemGenerated`. The upsert key is the **four-part**
`(Employee, PaymentDate, OffCyclePaymentDate, wage code 2005)` — which is why basic pay and leave pay accumulate
onto the same October row instead of creating two. **`OffCyclePaymentDate` is part of the key on purpose**:
without it, recoveries from different off-cycle runs merge and over-deduct.

### 4.1 Re-running is now safe

Because the upsert **accumulates**, a re-run would stack onto the existing rows. Four clearing blocks (ported
from legacy `Program.cs` lines 1201, 1619–1653, 1796–1819 and 1832–1850) prevent that:

| When | What it does |
|---|---|
| Before an off-cycle's period loop | **Deletes** every 2005 row keyed to that off-cycle payment date, whatever its month |
| During an off-cycle, per register | **Zeroes** the register's own 2005 line; zeroes 2005 rows whose payment date *and* off-cycle date are both inside the run window (skipped for End Of Service) |
| During an off-cycle, per register | **Zeroes** the register's own 2011 line and the 2011 carry-forward dated the 1st of the following month |
| During a regular run | **Zeroes** 2005 rows for *future* months raised by an off-cycle paid on this pay date — but only when the advance-paid leave is no longer on file. The current month is deliberately left alone |

So you can re-run the off-cycle as often as you like: October stays 260.000 and November stays 120.000. Two
tests pin it (`Re_Running_The_Same_OffCycle_Should_Replace_The_Recovery_Not_Add_To_It` and the three-times
variant).

**One legacy narrowness worth knowing** while testing the regular-run clear: it matches
`OffCyclePaymentDate == payDate`, and on a regular run `payDate` is the **month end**. An off-cycle paid on the
15th therefore never matches it, so cancelling the leave after the off-cycle does *not* stop the future months
being collected. That is legacy's behaviour, ported deliberately and pinned by
`ZeroFutureAdvanceRecovery_Should_Not_Match_An_OffCycle_Paid_MidMonth`. If the business wants it widened, that
is a decision, not a bug fix.

---

## 5. How salary register, detail and days detail are written

Three tables, one per level.

### SalaryRegister — one row per (employee, ForPeriod, InPeriod, run type)

The off-cycle creates one row: `ForPeriod = InPeriod = 31/10/2026`, `PayrollRunType = OffCycle`,
`PayrollResultType = Current`, `OffCyclePaymentDate = 15/10/2026`, `PayrollReasonTypeId` = the reason type
matching "Advance Leave", `Locked = false`.

Two guards worth testing:

- **An off-cycle cannot follow a regular run in the same period** — *"Cannot run OffCycle Payroll for the month,
  as Regular Payroll already ran."* So run the off-cycle **before** October's regular payroll.
- Re-running the same off-cycle **reuses** the existing register and clears its lines first.

### SalaryRegisterDetail — one row per wage type on the register

`Rate`, `Number`, `Amount`, all rounded to **`DecimalInSalaryRegister`** decimals (default **6**), and stamped
`IsSystemGenerated`. Rounding happens inside the 9000 Net Pay rule, **before** net pay is summed —
deliberately, so net pay cannot end up 0.001 away from the lines it adds up.

A line whose amount is zero is **deleted** for most rules, but basic pay, leave pay and net pay write theirs
even at zero. That is why the "not IncludeInAdvancePay" mistake shows up as an `Amount 0` line rather than a
missing one.

### SalaryRegisterDaysDetail — the month-wise breakdown under a detail line

**This table is only written on an off-cycle register whose `InPeriod = ForPeriod`, and only for a non-zero
amount.** A regular register carries none. `Period` is stamped as that month's **last** day.

Expected rows for the off-cycle register:

| Parent line | Period | Rate | Number | Amount |
|---|---|---|---|---|
| 1000 Basic Pay | 31/10/2026 | 260.000000 | 13 | 130.000000 |
| 1050 Annual Leave Pay | 31/10/2026 | 260.000000 | 13 | 130.000000 |
| 1050 Annual Leave Pay | 30/11/2026 | 260.000000 | 12 | 120.000000 |

No November row under 1000 — its amount is 0.

Each line's day rows sum back to the line (250.000000 and 25 days for 1050). That reconciliation is what makes
the payslip's breakdown defensible, and it is asserted in the test.

---

## 6. Remaining port gaps

The re-run gap is closed. One decision is outstanding, three smaller things remain, and two were fixed while
writing this plan.

### 6.0 No cap enforcing "never more than one month" on the actual-days branch

The fixed-days formula guarantees it; the actual-days branch does not, and legacy has no general cap — see
§3.1a for the concrete month and the test that pins it. **This is a business decision waiting to be made**, not
a port defect: adding a cap would be a deliberate deviation from legacy that changes money.

Related: `PayrollEngineSettings.EnableBasicPayMoreThanFixedDays` is assigned at
[PayrollEngineService.cs:519](../../aspnet-core/src/HCMv3.Core/PayrollManagement/Services/PayrollEngineService.cs)
and never read — a dead duplicate. The live copy is `PayrollDayCountService`'s own `DayCountSettings`, which is
read correctly, so nothing is broken; the engine-level field is just noise.

### 6.1 `PickOffcyclePayment` setting is read but never used

`ManagePayrollSettings.PickOffcyclePayment` ("up to current date" vs "up to current month end") is loaded into
`PayrollEngineSettings` at
[PayrollEngineService.cs:542](../../aspnet-core/src/HCMv3.Core/PayrollManagement/Services/PayrollEngineService.cs)
and then consumed nowhere. Legacy used it at its line 594. Changing the setting has no effect today.

### 6.2 `2011` Last Month Recovery, and negative net pay

Net pay cannot go negative on a regular run: it is zeroed and the shortfall becomes a `2011` `EmployeePayment`
dated the 1st of the following month. This scenario never triggers it (October nets exactly zero, not below),
but a tenant whose deductions exceed the advance will hit it. The off-cycle now clears 2011 rows on a re-run,
but the negative-net path itself has no scenario test — worth a second plan.

### 6.3 A stale remark in the source — fixed

`NetPayWageTypeCalculator` said `EmployeePayment` has no `IsSystemGenerated` column. It does, since wave P8, and
the clearing blocks set it. Corrected.

### 6.4 The `GetPresentDays` window argument — fixed

Legacy hands `GetPresentDays` a **per-month** bound as its `offCycleEndDate`, not the run's:

| Call site | Legacy 7th argument |
|---|---|
| Off-cycle recovery month (`EmployeeBasicPayService.cs` line 921) | `monthEnddate` — this month's clamped end |
| Retro month (`EmployeeBasicPayService.cs` line 584) | `payDate` |

Both port call sites were passing the run-level `OffCycleEndDate`. That bound is not cosmetic: on an Advance
Leave run `GetPresentDays` recomputes its **own** recovery periods from it, so the October call was fanning out
over October *and* November and could add both months' present days into October's basic pay. Corrected in
[EmployeeBasicPayCalculationService.cs](../../aspnet-core/src/HCMv3.Core/PayrollManagement/Services/EmployeeBasicPayCalculationService.cs)
and pinned by `OffCycle_Basic_Pay_Should_Count_One_Recovery_Month_At_A_Time`.

It happened not to change this scenario's figures — November's inner contribution is 0 either way, because the
row window is clamped to the leave end where every day is leave or a Friday — but it would bite any shape where
a recovery month has attended days inside the window.

---

## 7. Variants worth running next

| Variant | What it exercises | Effect on the figures |
|---|---|---|
| Absence type **`IncludeWeekend = Yes`** | The Friday inside leave becomes a leave day and its week-off is cleared | 30 paid leave days instead of 25; October present days 26 − 16 = 10. Still exactly one month |
| `FixedToActualWhenLeaveOverlapOnMonthEnd` **on** | October flips out of fixed-26 into actual working days | **No change** — both branches give 13. See §3.2a |
| A 31-day month with only **four** week-offs (e.g. March 2027) under the actual-days branch | The one shape where legacy exceeds one month | Ceiling is 27 days = 270.000 on a 260.000 salary. See §3.1a |
| Leave inside one month (e.g. 05–20/10) | Single recovery period | One 2005 row, one days-detail row |
| Leave spanning three months (e.g. 20/10–10/12) | Three recovery periods | On fixed-26 the middle month has 0 present days |
| Leave of 26 days or more in one month | `EnableBasicpayMoreThenFixedDays` off forces present days to 0 | Basic pay 0 for that month |
| Two week-offs (Fri + Sat) | Salary basis becomes `calendarDays − 8/9`, which can drop *below* the fixed 26 | The `totalLeavedays >= salaryBasisDays` ceiling starts to bite |
| Public holiday inside the leave | `IncludePublicHoliday` behaves exactly as `IncludeWeekend` does | A PH inside leave is either a leave day or a PH, never both |
| Wage type on `CalendarDaysPerMonth` instead | `rate ÷ days-in-month × days` | October and November get different per-day rates |
| Leave payment type `PayFullWithPayroll` | The *regular* run's spill branch pays the next months and posts the 2005 rows itself | Same money, different code path |
| Off-cycle reason **End Of Service** | No advance recovery is ever created, and the window clear is skipped | No 2005 rows at all |
| Re-run the off-cycle after changing the leave dates | The clearing blocks must replace, not stack | October stays 260.000 |
| Off-cycle before the PCR is closed | The §2 Step 3 gate | Refused |

---

## 8. Billing-method parity matrix

**Standing requirement: regular and off-cycle must match legacy as is, on all three billing methods.** No caps,
no corrections — where legacy looks wrong, it is pinned with a test and raised as a business question, not
changed. The decision in §6.0 is therefore closed: **no cap will be added.**

[PayrollBillingMethodParity_Tests.cs](../../aspnet-core/test/HCMv3.Tests/PayrollManagement/PayrollBillingMethodParity_Tests.cs)
(12 tests) asserts each method's **legacy formula written out**, not a snapshot of what the port produces.
October 2026, same fixture as above:

| Billing method | Regular basic | Regular leave | Off-cycle basic | Off-cycle leave |
|---|---|---|---|---|
| **Calendar days** | 18 d → 150.967742 | 13 d → 109.032258 | 168.301075 | 213.032258 |
| **Calendar minus week-off** | 13 d → 130.000000 | 13 d → 130.000000 | 130.000000 | 250.000000 |
| **Fixed 26 days** | 13 d → 130.000000 | 13 d → 130.000000 | 130.000000 | 250.000000 |

The formulas, straight from legacy:

| Method | Legacy source | Per-day | Basic days |
|---|---|---|---|
| Calendar days | `EmployeeBasicPayService.cs` 124-229 / 671-796 | `rate ÷ DaysInMonth` | attended **+ week-offs added back** + PH + rest − includeInLeavePay |
| Calendar minus week-off | 233-317 / 800-901 | `rate ÷ (DaysInMonth − weekOffs)` | attended + PH (week-offs **not** added back) |
| Fixed 26 days | 321-332 / 905-959 | `rate ÷ FixedDays` | the 4014 Present Days line = `FixedDays − leaveDays` |

**Two legacy asymmetries confirmed present in the port** — both deliberate:

- `WorkingDays_REST` subtracts public holidays falling on a *duty* week-off in the **regular** branch
  (legacy 275-283) but **not** in the off-cycle one (legacy 836-847).
- The off-cycle `FixedAmountPerMonth` branch **assigns** `Number` where every other branch accumulates
  (legacy 992).

**Invariants that hold on all three methods**, each pinned by a `[Theory]`:

- October is worth exactly one month (260.000). The methods disagree on the basic/leave *split* — 18/13 against
  13/13 — but each splits the same month.
- The off-cycle recovers exactly what it advanced, and October's 2005 share is exactly one month.

Note **calendar-minus-week-off and fixed-26 coincide in October 2026**, because its salary basis (31 − 5) is
exactly 26. They are genuinely different formulas, asserted separately; they simply agree on this month. Pick a
month whose basis is not 26 (e.g. March 2027, basis 27) to tell them apart.
