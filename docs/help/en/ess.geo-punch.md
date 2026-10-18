---
helpId: ess.geo-punch
lang: en
title: Geo Punch
surfaces:
  - angular/src/app/main/ess/my-geo-punch/my-geo-punch.component.html
audience: [end-user]
permissions: [Ess.GeoPunch]
---

## Overview

Clock in and out from your phone or your computer instead of from a wall device. The screen reads where you are, checks it against the work location you are punching from, and records the punch straight away — there is nothing to approve afterwards.

You need three things for the punch to go through: a duty for today, one of the four buttons available to you, and a position inside the allowed area. If your company asks for a selfie, that is a fourth.

Allow your browser to use your location when it asks, and your camera too when a selfie is required. Without them the buttons stay unavailable. Both need the site to be opened over `https`, or on the computer it is running on.

## Clock In and Clock Out {#ess.geo-punch.clock}
<!-- alias: loc:GeoPunchClockIn -->

**What it is.** The two buttons that record the start and the end of your working day. Only the one you can actually use is highlighted: after you clock in, **Clock In** turns grey until you have clocked out.

**Example.** You arrive at 07:55, press **Clock In**, and the punch is stored at 07:55. At 16:30 you press **Clock Out**.

**Why it matters.** These are the punches the Time Engine pairs into your hours for the day, and what your overtime, lateness and absence are worked out from. A punch is saved to the minute, so 07:55:40 is recorded as 07:55.

## Overtime In and Overtime Out {#ess.geo-punch.overtime}
<!-- alias: loc:GeoPunchOvertimeIn -->

**What it is.** Two extra buttons for work outside your duty hours, shown only when your grade is paid overtime.

**Example.** Your duty ends at 16:00 and you are asked to stay on. You clock out at 16:00, press **Overtime In**, and press **Overtime Out** when you leave at 18:30.

**Why it matters.** Overtime is counted from these punches and not from the ordinary ones, so time worked after **Clock Out** without an **Overtime In** is not paid as overtime. If you do not see the buttons, your grade is not set up for overtime — ask HR rather than clocking out late instead.

## Selfie {#ess.geo-punch.selfie}
<!-- alias: loc:GeoPunchSelfie -->

**What it is.** A photo of yourself taken at the moment you punch, when your company requires one. The buttons stay unavailable until the photo is taken.

**Example.** Press **Take selfie**, check the picture, and punch. **Retake** replaces it if it came out badly.

**Why it matters.** It is what stops a punch being made on your behalf by somebody else holding your phone. The photo is stored with the punch and HR can open it from the Time Events screen.

:::admin
Required when the tenant's self-service settings allow geolocation selfies AND the employee's time policy asks for a face. An employee with no time policy is treated as requiring one.
:::

## Work location {#ess.geo-punch.work-location}
<!-- alias: loc:WorkLocation -->

**What it is.** Which of your company's sites you are punching from. The list holds only the ones you are allowed to use today — your own, and any site you have been extended to for a period that covers today.

**Example.** Head Office; or the client site you were sent to this week.

**Why it matters.** The distance is measured from the site you pick, so picking the wrong one puts you out of range. If your company does not restrict you to a site, no list is shown and your punch is recorded wherever you are.

## Your location {#ess.geo-punch.your-location}
<!-- alias: loc:GeoPunchYourLocation -->

**What it is.** The position your browser reported, and how accurate it says it is.

**Example.** 29.3388, 48.0838 — accurate to about 20 m.

**Why it matters.** Indoors, and on a computer with no GPS, the position can be out by a hundred metres or more, which is enough to read as out of range. Stepping outside or near a window usually fixes it. The position is stored with the punch.

## Allowed range and the site boundary {#ess.geo-punch.range}
<!-- alias: loc:GeoPunchAllowedRange -->

**What it is.** How far from the work location a punch still counts as being at work. A site is set up either with a **range** — a distance in metres from the site's pin — or with a **site boundary**, a shape drawn around it. When a boundary is used you are told whether you are inside or outside it rather than being shown a number of metres.

**Example.** A range of 100 m around the head office pin; or a boundary drawn around the whole compound, so the car park counts and the street does not.

**Why it matters.** A shape can follow a site that is not round — an L-shaped compound, or one where the road runs right past the wall — which a range cannot do without also covering the street.

:::admin
Set per work location under Organization Management → Work Locations: **Geo Range** in metres, and **Geo Fence Coordinates** for the boundary. The boundary is only applied when **Enable Geo Fence** is on in Time Settings, and it replaces the range for that location. A boundary that does not enclose its own site pin is ignored and the range is used instead, which is what happens when latitude and longitude are entered the wrong way round.
:::

## When you are out of range {#ess.geo-punch.out-of-range}
<!-- alias: loc:PunchingOutOfLocation -->

**What it is.** A warning that where you are standing is not close enough to the work location you chose. Depending on your company's settings the punch is either refused outright, or recorded with a note of how far outside you were.

**Example.** "You are 240 m away. The allowed range is 100 m."

**Why it matters.** When a punch is allowed out of range, the distance is stored with it and shows on attendance reports, so it is visible to HR. If you are genuinely at work and still out of range, check the work location you picked and press **Refresh location** before asking HR to widen it.

## No duty today {#ess.geo-punch.no-duty}
<!-- alias: loc:GeoPunchNoDutyWarning -->

**What it is.** A warning that no duty period covers today, which means there is no shift for a punch to belong to.

**Example.** A day off, or a duty period that ran out and has not been renewed.

**Why it matters.** Nothing can be recorded until it is fixed, and only HR can fix it. If you are at work and see this, contact HR rather than waiting — the day counts as absent until a duty exists.

## Refreshing your location {#ess.geo-punch.refresh}
<!-- alias: loc:GeoPunchRefreshLocation -->

**What it is.** Asks your browser for your position again and re-checks it against the selected site.

**Example.** You open the screen inside the building, it reads as out of range, you walk to the entrance and press **Refresh location**.

**Why it matters.** The position is taken when the screen opens and when you change site, so a reading taken while your phone was still settling can be stale. This is the quickest fix for an out-of-range warning you do not believe.

## Today's punches {#ess.geo-punch.todays-punches}
<!-- alias: loc:GeoPunchTodaysPunches -->

**What it is.** Every punch recorded for you today, whichever way it was made — this screen, a wall device, or one HR entered for you — with its time, its work location and whether it was in range.

**Example.** 07:55 Clock In — Head Office — in range; 16:30 Clock Out — Head Office — in range.

**Why it matters.** It is how you check a punch actually went through before you leave. A punch missing from this list was never recorded, and needs a missing-punch request under **My Punches**, not a second attempt here.
