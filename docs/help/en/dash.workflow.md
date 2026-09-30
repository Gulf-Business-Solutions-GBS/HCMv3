---
helpId: dash.workflow
lang: en
title: Workflow Requests Dashboard
surfaces:
  - angular/src/app/admin/dashboards/workflow-dashboard/workflow-dashboard.component.html
audience: [end-user]
permissions: [Pages.Dashboards.Workflow]
---

## Overview

How the approval workflow is running: the requests raised over a period, how they were decided and how long that took, which open ones are overdue, and whom they are waiting with — by month, request type and department. It opens on this year to date.

Every figure counts the **requests raised** in the period by the employees you may see, from the first day to the end of the last day. A draft was never submitted, so it is never counted. Each request is counted by where it stands **now**: a request raised in January and approved in March counts as approved in January's bar.

**Withdrawn** is a request the employee took back. **Overdue** uses the **due in days** set on the request's approval chain: an open request older than that is overdue. A chain with no due days never makes a request overdue.

Click a request type to show every tile for that type only, a month, a status, a waiting band or an approver to list the requests, and a department to see the units inside it. Each choice appears as a chip in the toolbar; remove the chip to go back. A period longer than five years is shortened to the last five.

## Requests raised {#dash.workflow.raised}
<!-- alias: loc:WorkflowDashboard_Raised -->

**What it is.** How many requests were raised in the period. The line below gives how many were withdrawn or transferred. Click it for the list.

**How it is calculated.** `Requests raised in the period, drafts left out`

- Every status is counted, so awaiting approval + approved + rejected + withdrawn + transferred always equals this figure.

**Example.** 412 · 9 withdrawn · 2 transferred.

**Why it matters.** It is the workload the approvers carry.

## Awaiting approval {#dash.workflow.open}
<!-- alias: loc:WorkflowDashboard_OpenRequests -->

**What it is.** The requests raised in the period that are still waiting for a decision. The line below gives the longest wait. Click it for the list.

**How it is calculated.** `Requests with status Open`; **longest waiting** = `the most days any of them has waited since its last action`

- The last action is the latest step that moved the request on — raised, approved at a step, sent back, reassigned, escalated. Being read or remarked on does not count.

**Example.** 37 · longest waiting 19.5 days.

**Why it matters.** Employees are waiting on these; the longest wait shows whether something is stuck.

## Overdue {#dash.workflow.overdue}
<!-- alias: loc:WorkflowDashboard_Overdue -->

**What it is.** Open requests that are past the time their approval chain allows. Click it for the list.

**How it is calculated.** `Open requests where raised date + due in days < now`, with **due in days** from the approval chain the request follows.

- A chain with no due days (0 or blank) never makes a request overdue.

**Example.** 6 overdue.

**Why it matters.** These are the promises already broken; they are the first ones to chase.

## Approved {#dash.workflow.approved}
<!-- alias: loc:WorkflowDashboard_ApprovedRequests -->

**What it is.** Requests fully approved. The line below gives the approval rate. Click it for the list.

**How it is calculated.** `Requests with status Approved`; **approval rate** = `approved ÷ (approved + rejected) × 100`

- Only decided requests are in the rate, so requests still open do not lower it. Shown as "—" when nothing was decided.

**Example.** 340 · 94.4% of decided requests.

**Why it matters.** A falling approval rate usually means unclear rules or incomplete requests.

## Rejected {#dash.workflow.rejected}
<!-- alias: loc:WorkflowDashboard_RejectedRequests -->

**What it is.** Requests rejected at any step. The line below gives the number decided. Click it for the list.

**How it is calculated.** `Requests with status Rejected`; **decided** = `approved + rejected`

**Example.** 20 · of 360 decided.

**Why it matters.** Look at the rejected list by type to see which requests people most often get wrong.

## Days to decide {#dash.workflow.days-to-decide}
<!-- alias: loc:WorkflowDashboard_DaysToDecide -->

**What it is.** How long a request takes on average from being raised to its final decision.

**How it is calculated.** `Average of (time of the final approval or rejection in the request log − time raised)`, in days, over approved and rejected requests.

- Measured from the request log, to one decimal place: a request decided in 36 hours counts as 1.5 days.
- Shown as "—" when nothing was decided.

**Example.** 2.3 days.

**Why it matters.** It is the service level employees actually get.

## Requests by month {#dash.workflow.trend}
<!-- alias: loc:WorkflowDashboard_ByMonth -->

**What it is.** Requests raised each month, stacked by where they stand now. Click a month to list its requests.

**How it is calculated.** `Requests raised per month, split by current status`

- Every month of the period is shown, including months with no requests.

**Example.** March: 48 approved, 6 awaiting approval, 2 rejected.

**Why it matters.** It shows busy months, and old months that still have open requests.

## Requests by type {#dash.workflow.by-type}
<!-- alias: loc:WorkflowDashboard_ByType -->

**What it is.** Requests per request type, with how many are open, approved, rejected and overdue, and the average days to decide. Click a type to show every tile for that type only.

**How it is calculated.** The rules above, per request type, most requests first.

**Example.** Leave request: 220 raised · 2.1 days to decide.

**Why it matters.** It shows which kinds of request are slow, so that approval chain can be looked at.

## Waiting with {#dash.workflow.waiting-with}
<!-- alias: loc:WorkflowDashboard_WaitingWith -->

**What it is.** The ten approvers holding the most open requests at their step, with how many are overdue and the oldest wait. Click a name to list the requests.

**How it is calculated.** For each approver: `open requests whose current step is assigned to them and not yet approved by them`; **oldest** = `most days any of them has waited since its last action`.

- An approver is named even when they work outside the part of the organisation you see: it is about who holds the request.
- A request assigned to several people at the same step is counted under each of them.

**Example.** Head of Finance: 9 waiting · 3 overdue · oldest 12 days.

**Why it matters.** It turns "requests are slow" into a name to follow up with.

## How long open requests have waited {#dash.workflow.waiting-bands}
<!-- alias: loc:WorkflowDashboard_WaitingBands -->

**What it is.** Open requests grouped by how long they have waited since their last action. Click a bar to list them.

**How it is calculated.** `Whole days since the last action`, in bands: 0–2, 3–7, 8–14, 15–30, 31 or more days.

- Each band starts where the one before stops, so every open request falls in exactly one.

**Example.** 0–2 days: 22 · 31+ days: 3.

**Why it matters.** Requests in the last bands have probably been forgotten.

## Requests by department {#dash.workflow.by-department}
<!-- alias: loc:WorkflowDashboard_ByDepartment -->

**What it is.** Requests raised, open and overdue, and the average days to decide, for each department one level below the unit you are looking at. Click a department to see the units inside it.

**How it is calculated.** The rules above, over the requests of employees in that department or any unit below it.

- A request is placed by the department of the employee who raised it, as on the last day of the period.
- People attached directly to the unit you are looking at are shown on their own line.

**Example.** Operations: 180 raised · 14 open · 4 overdue · 2.8 days.

**Why it matters.** It shows where requests pile up and where decisions are slowest.

## Requests {#dash.workflow.requests}
<!-- alias: loc:WorkflowDashboard_Requests -->

**What it is.** The list behind a figure: each request with its number, type, employee, department, date raised, status, whom it is waiting with, its days and its due date. Click a row to open the employee.

**How it is calculated.** The same rule as the figure you clicked. **Days** is, for an open request, the days since its last action; for a decided one, the days it took. The due date is shown for open requests whose chain has due days, in red once passed.

**Example.** Click **Overdue**: 6 rows, each with its due date in red.

**Why it matters.** It is the evidence under the numbers, and it downloads as XLSX with the same filters.
