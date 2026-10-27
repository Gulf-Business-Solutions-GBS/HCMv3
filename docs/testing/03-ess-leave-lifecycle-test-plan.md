# ESS leave lifecycle test plan — apply, amend, cancel, return (on time / early / late) → payroll

Tests how the legacy ESS leave workflow (HCM10 `AbsenceRequest`: Create / Amendment / ReturnFromLeave /
Cancellation) was ported to HCMv3, and that every outcome lands correctly in a real payroll run.

**One table, not two.** Legacy kept a request table (`AbsenceRequest`) beside the leave table
(`EmployeeAbsence`). HCMv3 has only `EmployeeAbsence`. A leave waiting for approval is a row with status
Draft/Open that payroll, time and reports cannot see (the `ApprovedOnly` filter). A change to an approved leave
(amend, return, cancel) is a request **on the same row**; the row does not move until the change is approved.

Automated coverage (run before and after any change to leave or ESS):

```bash
dotnet test aspnet-core/test/HCMv3.Tests/HCMv3.Tests.csproj --filter "FullyQualifiedName~ReturnFromLeave_Tests|FullyQualifiedName~LeaveCancellation_Tests|FullyQualifiedName~EssLeavePayroll_Tests|FullyQualifiedName~MyLeaveAppService_Tests|FullyQualifiedName~WorkflowAmendment_Tests"
```

- [ReturnFromLeave_Tests.cs](../../aspnet-core/test/HCMv3.Tests/Workflow/ReturnFromLeave_Tests.cs) — the three returns, the split rules, weekend handling
- [LeaveCancellation_Tests.cs](../../aspnet-core/test/HCMv3.Tests/Workflow/LeaveCancellation_Tests.cs) — cancelling an approved leave
- [EssLeavePayroll_Tests.cs](../../aspnet-core/test/HCMv3.Tests/Workflow/EssLeavePayroll_Tests.cs) — ESS actions followed by a **real** September 2026 payroll run (seeded catalogue, real Time and Payroll engines)

---

## 1. Set-up

| Thing | Value | Why |
|---|---|---|
| Employee | Loaded through Go-Live, basic 500.000 KWD, Friday weekly off | The seeded fixed-26 catalogue: a day is 500 ÷ 26 = 19.231 KWD |
| Employee ↔ user | Employee record linked to the tester's login (`Employee.UserId`) | ESS acts as the linked employee; the admin user must stay unlinked |
| Leave workflow | Workflow transaction type **EmployeeAbsence** enabled, a one-step Request Master, an approver holding its workflow role | Otherwise leave is recorded immediately with no approval |
| Annual leave | Seeded code **1**: counts weekends, quota-accrued, **return date required**, leave pay wage **1003** | Late paid days are measured against code 1, as in legacy |
| Unpaid leave | Seeded code **2** | Late days beyond the balance are booked here |
| Quota | An Annual Leave quota row covering September 2026 | Balance checks and deductions use it |
| Settings | Time: **Allow future dated return from leave** (only if testing ahead of time). Payroll: **Enable not-return-from-leave logic** for scenario 3 | Legacy refuses a future rejoining date without the first |

## 2. Scenarios (September 2026, pay date 30/09)

| # | ESS actions | Expected in the payroll register |
|---|---|---|
| 1 | Apply 13–17 Sep, **do not approve** | 4000 Annual Leave Paid Days = **0**, 1003 = 0 — pending leave is invisible |
| 2 | Apply 13–17, approve; report return on 18/09, approve | 4000 = **5**; basic + leave pay = **500.000** |
| 3 | Payroll settings **Enable not-return-from-leave logic** AND **Pay leave as per cut-off** both on (with the second off, legacy ignores an unreturned leave in a Regular run and simply stops paying at the leave end). Apply 13–17, approve, **no return**, run | Employee **fails**: "ReturnDate not found for LeaveType …". Report return 18/09, approve, re-run → 4000 = 5 |
| 4 | **Late, within balance.** Apply 13–17, approve; rejoin **22/09** | Preview says late 4 days, all paid. After approval: a new Annual leave 18–21 (approved, linked to the return request); 4000 = **9**, 4001 = 0; earnings 500.000 |
| 5 | **Late, beyond balance.** Quota 7. Apply 13–17, approve; rejoin 22/09 | Preview: paid 18–19, unpaid 20–21. After approval: Annual 18–19 + Unpaid 20–21. 4000 = **7**, 4001 Absent Unpaid Days = **2**; earnings = 500 − 2 × 500/26 = **461.538** |
| 6 | **Early.** Apply 13–24, approve; rejoin **18/09** | Leave becomes 13–17, original end 24/09 kept as *Initial end date*; 7 days back on the quota; 4000 = **5** |
| 7 | **Cancel.** Apply 13–17, approve; *Request cancellation* | While pending: leave still paid (4000 = 5). After approval: leave removed, quota refunded, 4000 = **0**, earnings 500.000 |

## 3. Rules to check by hand (legacy parity)

**Return type** — legacy `DaysDifference`:
- Rejoining the day after the leave ends → **on time**.
- A gap made **only** of week-offs/holidays the leave type does not count → **on time** (leave Sun–Thu, back Sunday).
- Any countable day in the gap → **late by the whole calendar gap** (Fri, Sat, Sun → 3 days late, not 1).
- Rejoining before the end → **early**; refused for a type with *Allow to return early* = No.
- Rejoining on or before the leave start → refused. A future date → refused unless the time setting allows it.

**Late split** — refused with a message when:
- paid + unpaid calendar days ≠ days late;
- paid does not start the day after the leave, or (with no unpaid days) does not end the day before rejoining;
- unpaid does not end the day before rejoining, or (with no paid days) does not start the day after the leave;
- paid and unpaid dates overlap, or fall outside the gap;
- the paid working days exceed the bookable balance;
- another leave already starts inside the gap ("Leave entry exists").

**Day count** — applying, amending and returning early charge calendar days **less** the week-offs, public
holidays and rest days the leave type excludes (its *Include weekend / holiday / rest day* flags). Sun–next Sun
with a Fri/Sat weekend on a type that excludes weekends = 8 calendar days, **6 charged**.

**Rejected changes** leave everything exactly as it was: dates, return stamp, quota, and no extra rows.

**Payroll lock.** After a payroll run the month stays *Released for payroll*. Approving a return, amendment or
cancellation for that month is then refused ("Payroll Area … is locked for payroll processing"), and nothing
changes. HR releases the month for correction, the approver approves, and the next Run Payroll locks the month
again and runs it (scenario 7 does exactly this).

**Go-Live employee load** does not set the employees' *Earliest master data change* (decided 2026-09-22), so
the first payroll does not recalculate every month back to the joining date.

## 4. One known difference from legacy

When a late return has **both** paid and unpaid days, legacy placed the unpaid leave starting right after the
original leave (`AbsenceRequest.cs` line 1572 resets its cursor), so it overlapped the paid leave and left the end
of the gap uncovered. HCMv3 places the unpaid leave **after** the paid leave. With only paid or only unpaid days —
the usual case — the result is identical to legacy.

## 5. Not covered yet

- *Additional unpaid leave* on the apply form (legacy Ext fields) and the early-return variants that depend on it.
- *Pay in advance* leave (and its payment-date rule) raised from ESS — HR can still enter it.
- HR's own Employee Absence screen does not move the quota on create/edit/delete, and has no back-office return
  logic (legacy `SaveDiffInformation`).
- Leave-policy checks legacy ran on ESS submit (probation, min/max days, apply-before days, separation notice).
- Approving a leave change while the payroll area is locked (the lock guard exists but has no leave test).
