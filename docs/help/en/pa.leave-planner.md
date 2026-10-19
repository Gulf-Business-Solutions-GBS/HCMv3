---
helpId: pa.leave-planner
lang: en
title: Leave Planner
surfaces:
  - angular/src/app/admin/personnel-administration/leave-planner/leave-planner.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.LeavePlanner]
---

## Overview

Who plans to be away and who is actually away, day by day for one month, grouped by organization unit. It opens on the current month; the arrows move a month back or forward.

Each unit has a line of its own showing how many of its people are away, or planning to be, on each day; below it is one line per employee, coloured by what falls on the day: **Planned - Approved**, **Planned - Open**, **Leave - Approved** and **Leave - Open**. A day can carry more than one, because a plan sitting on top of approved leave is exactly the thing to spot. Hover over a day for the details. Only employees with something in the month get a line, and each employee sits under the unit they are assigned to today; those in no unit come last, under **No department**.

Plans come from **Leave Planner Request** in self-service and book nothing; leave comes from the employees' leave records. Hourly absences are left out — they are short permissions, not days away. Drafts never show, and neither do rejected, withdrawn or cancelled plans and leave.

## OrganizationUnit {#pa.leave-planner.org-unit}
<!-- alias: loc:OrganizationUnit -->

**What it is.** Narrows the planner to one unit and every unit below it. **All** shows everybody you can see.

**Example.** Finance — which also shows Finance / Payroll and Finance / Accounts.

**Why it matters.** The list offers only the units your employees sit in, so choosing one never empties the list of the others.

## Employee {#pa.leave-planner.employee}
<!-- alias: loc:Employee -->

**What it is.** Narrows the planner to employees whose code, English name or Arabic name contains the text. Press Enter to apply it.

**Example.** `Ahmed`

## ShowActualLeaveApproved {#pa.leave-planner.leave-approved}
<!-- alias: loc:ShowActualLeaveApproved -->

**What it is.** Shows leave that is in effect: approved, or recorded without approval. On when the planner opens.

## ShowActualLeaveUnapproved {#pa.leave-planner.leave-unapproved}
<!-- alias: loc:ShowActualLeaveUnapproved -->

**What it is.** Shows leave still waiting with an approver. Leave that was rejected or sent back is not shown.

## ShowLeavePlannerApproved {#pa.leave-planner.plan-approved}
<!-- alias: loc:ShowLeavePlannerApproved -->

**What it is.** Shows leave plans that are in effect: approved, or recorded without approval. On when the planner opens.

## ShowLeavePlannerUnapproved {#pa.leave-planner.plan-unapproved}
<!-- alias: loc:ShowLeavePlannerUnapproved -->

**What it is.** Shows leave plans still waiting with an approver.

**Why it matters.** Together with approved leave, it shows what managers are about to be asked to agree to while there is still time to plan cover.
