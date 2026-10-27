---
helpId: ess.punch
lang: en
title: My Punches
surfaces:
  - angular/src/app/main/ess/my-punch/my-punch.component.html
audience: [end-user]
permissions: [Ess.MyPunches]
---

## Overview

Ask for a punch to be added when one is missing — the device did not read your finger, you were out at a client, the reader was down.

This is a request, not a punch. It goes for approval, and only once it is approved does the Time Engine count it and your day stop looking short. Punches made on the Geo Punch screen do not appear here; they are real punches and need no approval.

## Punch type {#ess.punch.punch-type}
<!-- alias: loc:PunchType -->

**What it is.** Which punch was missed: Clock In, Clock Out, or an overtime one.

**Example.** Clock Out — you left straight from a meeting.

**Why it matters.** A day needs its In and its Out to be counted as worked. Asking for the wrong one leaves the day still unbalanced, and you will be asked to raise a second request.

## Punch date {#ess.punch.punch-date}
<!-- alias: loc:PunchDate -->

**What it is.** The day the punch was missed.

**Example.** 8 June 2026.

**Why it matters.** It has to be a day you were on duty. It also decides which payroll period the correction lands in — a request for a period that has already been paid is corrected in the next one, not the one you missed.

## Punch time {#ess.punch.punch-time}
<!-- alias: loc:PunchTime -->

**What it is.** The time of the missing punch. Optional: leave it blank and your duty's own start or end time is used.

**Example.** 17:42 — or blank, which fills in your shift's finishing time.

**Why it matters.** Blank is the honest answer when you genuinely worked your shift and the reader simply failed. Put a time in when the real one matters — a late start you want recorded, or an early finish that was agreed.
