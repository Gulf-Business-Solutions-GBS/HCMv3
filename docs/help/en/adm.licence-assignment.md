---
helpId: adm.licence-assignment
lang: en
title: Licence Assignment
surfaces:
  - angular/src/app/admin/administration/licence-assignment/licence-assignment.component.html
audience: [admin]
permissions: [Pages.Administration.Tools.LicenceAssignment]
---

## Overview

Decides which parts of HCM each user may use. Your subscription comes with a number of licences per module; here you give them to users, one tick per user and module.

There are five modules: **Administration (HRMS)** (the whole administration application), **Employee Self Service**, **Mobile Application** (geo punch and the mobile device list, which sit inside self-service), **Performance** and **Recruitment**. They nest: a recruiter needs an Administration licence and a Recruitment licence, and a mobile user needs a self-service licence and a Mobile licence.

When licences are enforced, a user without a licence for a module simply does not get it: its menu items disappear and its screens refuse to open. Signing in itself is not refused. Every tick takes effect at once.

The number of licences, and which modules your subscription includes, are set by GBS on your subscription; they cannot be changed from this screen. To get more licences, contact your GBS Account Manager.

:::admin
The counts and switches are ASP.NET Zero edition features, set by the host: "HCM: Per-user module licences", one licence count per module (0 = unlimited), the module on/off features, and "HCM: Maximum active employees". A module switched off in the edition is refused to everyone, whatever the ticks say.
:::

## HCM: Per-user module licences {#adm.licence-assignment.enforced}
<!-- alias:  -->

**What it is.** Whether licences are actually checked for your company. When they are not, a blue note at the top says so.

**Example.** "Per-user licences are not switched on for your subscription, so nobody is refused access yet."

**Why it matters.** Until GBS switches them on, nobody is refused anything, but you can already tick users, so that everything is in place on the day enforcement starts.

## Module {#adm.licence-assignment.usage}
<!-- alias:  -->

**What it is.** One card per module at the top, showing how many licences are in use and how many your subscription has.

**Example.** "Employee Self Service: 412 used of 500", or "Recruitment: 3 used / unlimited".

**Why it matters.** A licence counts only while its user is active and is not an administrator. Deactivating a user frees their licence but keeps their tick; reactivating them takes it back, which can push a module over its count (see **Over the limit**).

## Search {#adm.licence-assignment.search}
<!-- alias:  -->

**What it is.** Finds users by user name, name, surname or email. The two lists beside it work together: choose a module, then **Assigned** to list the users who hold it or **Not assigned** for those who do not. A module on its own, with **All**, still lists everybody.

**Example.** Module "Employee Self Service", **Not assigned**: every user still waiting for a self-service licence.

**Why it matters.** Press **Search** or Enter after typing; the lists apply as soon as you change them.

## Users {#adm.licence-assignment.users}
<!-- alias:  -->

**What it is.** Every user of your company, with the employee code where the user is linked to an employee, and one tick box per module.

**Example.** Tick **Performance** for a line manager who rates appraisals in the administration application.

**Why it matters.** Ticking or unticking saves immediately. Unticking is always allowed. A box that cannot be ticked is greyed out; hover over it to see why. When a user is linked to an employee, self-service and mobile licences are given to them automatically if those modules are in your subscription and licences are free; otherwise they are skipped without an error.

## Administrator, no licence needed {#adm.licence-assignment.administrator}
<!-- alias:  -->

**What it is.** Shown beside users who hold the Admin role.

**Example.** The "admin" user.

**Why it matters.** Administrators may use every module without a licence and never use one up, so their boxes cannot be ticked.

## Inactive {#adm.licence-assignment.inactive}
<!-- alias:  -->

**What it is.** Shown beside users who are switched off.

**Example.** A user deactivated after the employee left.

**Why it matters.** Their ticks are kept but do not count against your licences.

## Not in subscription {#adm.licence-assignment.not-in-subscription}
<!-- alias:  -->

**What it is.** The module is not part of your subscription.

**Example.** Recruitment, for a company that did not buy it.

**Why it matters.** Nobody can be given that module, and nobody can use it, until GBS adds it to your subscription.

## No licence left {#adm.licence-assignment.no-licence-left}
<!-- alias:  -->

**What it is.** Every licence for the module is in use, so a further tick is refused.

**Example.** "All 25 Administration (HRMS) licences are in use."

**Why it matters.** Untick a user who no longer needs the module first, or ask your GBS Account Manager for more licences. If another administrator used the last licence while you had the screen open, your tick is put back and the counts are refreshed.

## Over the limit {#adm.licence-assignment.over-limit}
<!-- alias:  -->

**What it is.** A module card in red: more licences are in use than your subscription has.

**Example.** 501 used of 500, after a user who held a licence was reactivated.

**Why it matters.** Nobody is locked out by it, but no new ticks can be given for that module until enough users are unticked to bring it back within the count.

## HCM: Maximum active employees {#adm.licence-assignment.active-employees}
<!-- alias:  -->

**What it is.** A separate limit on your subscription: how many employees may be Active at the same time. It is about employee records, not users, and is not shown on this screen.

**Example.** A subscription for 300 employees.

**Why it matters.** Once the limit is reached, saving a new active employee, making an employee active again, or rejoining a separated employee is refused with "Your subscription allows 300 active employees and that number has been reached." A limit of 0 means unlimited.
