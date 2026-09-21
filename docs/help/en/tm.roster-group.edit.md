---
helpId: tm.roster-group.edit
lang: en
title: Roster Groups — Create / Edit
entity: HCMv3.TimeManagement.RosterGroup
surfaces:
  - angular/src/app/admin/time-management/roster-groups/create-or-edit-roster-group-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterGroups.Create]
---

## Overview

A group of employees who share a roster -- for example a shift team. Its members are listed in the group's details, and the duty period the group follows in its schedules.

An employee's duty for a day comes first from a substitute duty for that date, then from their own employee duty period, and only then from their roster group. Roster groups are used only when the time setting *EnableRosterDuty* is on.
