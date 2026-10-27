---
helpId: ess.leave-plan
lang: en
title: Leave Planner Request
surfaces:
  - angular/src/app/main/ess/my-leave-plan/my-leave-plan.component.html
audience: [end-user]
permissions: [Ess.MyLeavePlans]
---

## Overview

Tell your manager when you mean to take leave, well before you apply for it, and see your team's plans and leave for the month.

A plan is information, not leave. Approving it books nothing: it does not reserve or charge your balance and does not create leave. What approval does is show your plan to HR and your manager as agreed rather than asked for. When the time comes, you still apply for the leave on **Leave Requests**, where the usual leave rules apply. Plans have no overlap rule, so a plan can cover days you already have another plan or leave for.

## Start date {#ess.leave-plan.start-date}
<!-- alias: loc:StartDate -->

**What it is.** The first day you plan to be away.

**Example.** 3 August 2026

**Why it matters.** The projected balance is worked out to this date, so moving it changes the figure on the right.

## End date {#ess.leave-plan.end-date}
<!-- alias: loc:EndDate -->

**What it is.** The last day you plan to be away.

**Example.** 20 August 2026

**Why it matters.** It cannot be before the start date. Both dates are needed to submit; a draft can be saved without them.

## Planned days {#ess.leave-plan.planned-days}
<!-- alias: loc:PlannedDays -->

**What it is.** Calendar days from the start date to the end date, both included. Weekends and public holidays count.

**Example.** 3 to 20 August is 18 planned days.

**Why it matters.** A plan names no leave type, so there are no leave rules to leave weekends out. For the days the leave itself would cost you, look at **Leave days** below.

## Projected leave balance {#ess.leave-plan.projected-balance}
<!-- alias: loc:ProjectedLeaveBalance -->

**What it is.** Your annual leave balance as it is expected to stand on the start date: including what you earn until then and counting only leave that is already approved. Shown in red when it is below zero.

**Example.** 21.5

**Why it matters.** It is the quickest way to see whether the leave will be covered when you apply for it. The figure is kept with the plan as it was when you saved and submitted it, so your approver sees what you saw. Blank when your company has no annual leave type set up.

## Leave days {#ess.leave-plan.leave-days}
<!-- alias: loc:LeaveDays -->

**What it is.** How many days of annual leave these dates would actually cost: the planned days less the weekly days off and holidays that annual leave does not count. Shown only while you are filling in the form.

**Example.** 18 planned days, 14 leave days.

**Why it matters.** Compare this, not the planned days, with your projected balance. It is not saved with the plan.

## Save {#ess.leave-plan.save}
<!-- alias: loc:Save -->

**What it is.** Keeps the plan as a draft, without checking it and without sending it to anybody.

**Example.** You know you want two weeks in August but not yet which ones.

**Why it matters.** A draft is yours alone: it shows in your list as **Draft** and nobody else sees it, not even HR. Open it again with **Edit** when you are ready.

## Submit {#ess.leave-plan.submit}
<!-- alias: loc:Submit -->

**What it is.** Checks the dates and sends the plan for approval.

**Why it matters.** Once sent you cannot change it in place; withdraw it and plan again, or wait for it to be sent back. If your company has leave plans set up without approval, it is recorded straight away instead.

## Leave Scheduler {#ess.leave-plan.scheduler}
<!-- alias: loc:LeaveScheduler -->

**What it is.** A month view of your own plans and leave, and those of the people who report directly to you. The arrows move a month back or forward; press the button again to hide it.

**Example.** Before you plan the first half of August, you see that two of your team are already planned for that fortnight.

**Why it matters.** Plans and leave are shown approved or still open; drafts and refused requests are not. Only one level is shown: the people who report to your direct reports are not included.

## Actions {#ess.leave-plan.actions}
<!-- alias: loc:Actions -->

**What it is.** What you can do with a plan in your list: **Edit** a draft or a plan sent back to you, open its **Details**, or take it back — **Withdraw request** while it is with an approver, **Discard draft** for one you never sent.

**Why it matters.** A plan sent back reopens in the same form and goes back under the same request number when you submit it again.
