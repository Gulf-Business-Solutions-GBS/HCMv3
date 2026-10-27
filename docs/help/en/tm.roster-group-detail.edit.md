---
helpId: tm.roster-group-detail.edit
lang: en
title: Roster Group Details — Create / Edit
entity: HCMv3.TimeManagement.RosterGroupDetail
surfaces:
  - angular/src/app/admin/time-management/roster-group-details/create-or-edit-roster-group-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterGroupDetails.Create]
---

## Overview

An employee's membership of a roster group for a period. The employee follows the group's schedule for the dates covered, unless they have their own duty period or a substitute duty.

## RosterGroup {#RosterGroupDetail.RosterGroupId}
<!-- alias: loc:RosterGroup, dom:RosterGroupDetail_RosterGroupId -->

**What it is.** The roster group.

**Example.** Team A

**Rules.** Required. Chosen from Roster Group master data.

## Employee {#RosterGroupDetail.EmployeeId}
<!-- alias: loc:Employee, dom:RosterGroupDetail_EmployeeId -->

**What it is.** The employee.

**Why it matters.** An employee's periods in the same roster group cannot overlap.

**Rules.** Required. Chosen from the employee list.
