---
helpId: pa.leave-plans
lang: en
title: Leave Plans
surfaces:
  - angular/src/app/admin/personnel-administration/leave-plans/leave-plans.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.LeavePlans]
---

## Overview

The leave employees say they intend to take, as they planned it in self-service under **Leave Planner Request**. The same list shows on an employee's record as **Leave Plans**, limited to that employee.

A plan is information, not leave. Approving one books nothing: it charges no balance, creates no leave record and changes nothing else. What approval does is let the plan show as planned, rather than asked for, on the Leave Planner. The leave itself is still applied for on the leave screen. Plans have no overlap rule: two plans of the same employee, or a plan and an actual leave, can cover the same days.

An employee's draft that was never sent is not listed here; it is nobody else's business until it is sent.

## Filter {#pa.leave-plans.filter}
<!-- alias:  -->

**What it is.** Narrows the list to employees whose code, English name or Arabic name contains the text. Press Enter or **Search**. Not shown on the employee record, where the list is already one employee's.

**Example.** `1042`, or `Fatima`

## ShowLeavePlannerUnapproved {#pa.leave-plans.show-unapproved}
<!-- alias:  -->

**What it is.** Also lists plans that are not in effect: still with an approver, sent back, rejected or withdrawn. Off, the list holds only approved plans and plans recorded without approval.

**Example.** Switch it on to see what is still waiting before you plan cover for next month.

## PlannedDays {#pa.leave-plans.planned-days}
<!-- alias:  -->

**What it is.** Calendar days from the start date to the end date, both included. Weekly days off and public holidays are counted, because a plan names no leave type whose rules could leave them out.

**Example.** 1 Aug to 14 Aug 2026 is 14 planned days.

## ProjectedLeaveBalance {#pa.leave-plans.projected-balance}
<!-- alias:  -->

**What it is.** The employee's annual leave balance projected to the plan's start date: what the employee is expected to have on that day, counting what accrues until then and approved leave only. Taken when the plan was saved and again when it was sent, so it shows what the employee saw, not today's figure.

**Example.** 18.5

**Why it matters.** A figure lower than the planned days is an early sign that the leave, when applied for, may not be covered. Blank when no annual leave type is set up.

## Status {#pa.leave-plans.status}
<!-- alias:  -->

**What it is.** Where the plan has got to in approval: Open while with an approver, Approved, Rejected, sent back as Draft. **Recorded** means it was saved without approval, because leave plans are not set up for approval.

**Example.** Open

## Delete {#pa.leave-plans.delete}
<!-- alias:  -->

**What it is.** Removes the plan, from the Actions menu. Needs the Leave Plans delete permission.

**Why it matters.** A plan still with an approver, or sent back to the employee, has its request closed as well, so it does not stay in anybody's inbox pointing at nothing. Nothing else is affected, since a plan booked nothing.
