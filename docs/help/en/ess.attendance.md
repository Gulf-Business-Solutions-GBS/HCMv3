---
helpId: ess.attendance
lang: en
title: My Attendance
surfaces:
  - angular/src/app/main/ess/my-attendance/my-attendance.component.html
audience: [end-user]
permissions: [Ess.MyAttendance]
---

## Overview

Ask for a day's attendance to be corrected when what the system worked out is not what happened — a day counted absent that you worked off-site, hours short because you were at a training course, time owed for a call-out.

A missing punch is not corrected here. Use **My Punches** for that; this screen is for the shape of the day itself.

## Attendance type {#ess.attendance.attendance-type}
<!-- alias: loc:AttendanceType -->

**What it is.** What kind of correction this is — work off-site, a training day, an official errand, a call-out — from the types your company has set up.

**Example.** Official Assignment.

**Why it matters.** The type decides how the day is then counted and paid: some make the day whole, some pay as overtime, some only record where you were.

## Date {#ess.attendance.date}
<!-- alias: loc:Date -->

**What it is.** The day being corrected. One request covers one day.

**Example.** 11 June 2026.

**Why it matters.** It is what the correction is matched to — the existing record for that day is what gets put right, so a request for the wrong date corrects a day that was never wrong.

## Start time {#ess.attendance.start-time}
<!-- alias: loc:StartTime -->

**What it is.** When the period being corrected began.

**Example.** 08:00

**Why it matters.** With the end time it says how much of the day this covers; part of a day is normal, and you do not have to claim the whole of it.

## End time {#ess.attendance.end-time}
<!-- alias: loc:EndTime -->

**What it is.** When the period being corrected ended.

**Example.** 12:30

**Why it matters.** It must be after the start time, and the two together produce the hours the correction is worth.

## Hours {#ess.attendance.hours}
<!-- alias: loc:Hours -->

**What it is.** The hours the correction comes to, from the start and end times.

**Example.** 4.5

**Why it matters.** It is the figure the approver judges and the Time Engine applies. If it does not look like what you meant, the start and end times are what to fix.

## Reason for the change {#ess.attendance.reason-for-the-change}
<!-- alias: loc:ReasonForTheChange -->

**What it is.** Why the day needs correcting, in your own words.

**Example.** "At the client's office in Shuwaikh all morning — no reader there."

**Why it matters.** A correction changes what the record says about a day that has already passed, so it is the one request where the explanation carries the weight. It stays on the record with the correction.
