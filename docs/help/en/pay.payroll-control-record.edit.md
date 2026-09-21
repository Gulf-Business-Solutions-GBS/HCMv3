---
helpId: pay.payroll-control-record.edit
lang: en
title: Payroll Control Records — Create / Edit
entity: HCMv3.PayrollManagement.PayrollControlRecord
surfaces:
  - angular/src/app/admin/payroll-management/payroll-control-records/create-or-edit-payroll-control-record-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollControlRecords.Create]
---

## Overview

The current payroll period of a payroll area, and its state. Each payroll area has exactly one control record. When you save, the calendar start and end of the period are worked out from the payroll month, the payroll year and the area's fiscal year.

The state decides what may happen:

- **Open** -- not started. Master data can be changed; a regular payroll run is refused.
- **Released for payroll** -- a regular run is allowed, for the month ending on the run's pay date. Changes to payroll-related records of employees in this area are refused ("Payroll Area : 'X' is locked for payroll processing…").
- **Closed** -- the month is finished. Off-cycle runs are allowed; a regular run is refused.

The state is normally moved from the list with **Release for payroll**, **Release for correction**, **Close period** and **Reopen period**, which also record who moved it and when. The two tick boxes on this form set the same state directly.

## PayrollAreaId {#PayrollControlRecord.PayrollAreaId}
<!-- alias: loc:PayrollAreaId, dom:PayrollControlRecord_PayrollAreaId -->

**What it is.** The payroll area this control record governs.

**Example.** Kuwait Monthly

**Why it matters.** An area can have only one control record; a second one is refused ("Control record is unique for a Payroll Area.").

**Rules.** Required. Chosen from Payroll Area master data.

## PayrollMonth {#PayrollControlRecord.PayrollMonth}
<!-- alias: loc:PayrollMonth, dom:PayrollControlRecord_PayrollMonth -->

**What it is.** The period being processed, as a month of the area's fiscal year, 1 to 12.

**Example.** `1` -- April, for an April–March area.

**Why it matters.** Together with the payroll year and the area's fiscal year it gives the period's calendar start and end. A regular run is allowed only when that end is the run's pay date.

**Rules.** Required. Whole number.

## PayrollYear {#PayrollControlRecord.PayrollYear}
<!-- alias: loc:PayrollYear, dom:PayrollControlRecord_PayrollYear -->

**What it is.** The fiscal year of the period being processed.

**Example.** `2026`

**Why it matters.** For a fiscal year that does not start in January, the later months fall in the next calendar year: April–March month 10 of 2026 is January 2027.

**Rules.** Required. Whole number.

## PeriodLocked {#PayrollControlRecord.PeriodLocked}
<!-- alias: loc:PeriodLocked, dom:PayrollControlRecord_PeriodLocked -->

**What it is.** Ticked, the period is released for payroll.

**Why it matters.** A regular payroll run needs it. While it is ticked, adding, changing or deleting payroll-related records of the employees currently in this area is refused. Only the area's latest period counts; an earlier period left locked does not block anything.

**Rules.** Yes or no.

:::admin
Moving the state with **Release for payroll** also locks that month's off-cycle payslips, when the payroll setting *Lock Salary Register On Payroll Exit* is on. Ticking the box here sets the state without that step.
:::

## PeriodClosed {#PayrollControlRecord.PeriodClosed}
<!-- alias: loc:PeriodClosed, dom:PayrollControlRecord_PeriodClosed -->

**What it is.** Ticked, the period is closed.

**Why it matters.** Off-cycle runs need it: an off-cycle run is refused until the period is closed.

**Rules.** Yes or no.

:::admin
Moving the state with **Close period** also locks that month's regular payslips, when *Lock Salary Register On Payroll Exit* is on; ticking the box here does not. When the payroll setting *Disable Retro Calculation* is on, changes dated before the period are refused: before its first day while it is open or released for payroll, and up to its last day once it is closed.
:::
