---
helpId: perf.patimeline-exception.edit
lang: en
title: PA Timeline Exceptions — Create / Edit
entity: HCMv3.PerformanceManagement.PATimelineException
surfaces:
  - angular/src/app/admin/performance-management/pa-timeline-exceptions/create-or-edit-pa-timeline-exception-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PATimelineExceptions.Create]
---

## Overview

An extension of one stage's deadline for one employee.

Not read at present: timelines do not block late submissions, so an extension changes nothing.

## PAPeriod {#PATimelineException.PAPeriodId}
<!-- alias: loc:PAPeriod, dom:PATimelineException_PAPeriodId -->

**What it is.** The appraisal period.

**Rules.** Required. Chosen from PA Period master data.

## Employee {#PATimelineException.EmployeeId}
<!-- alias: loc:Employee, dom:PATimelineException_EmployeeId -->

**What it is.** The employee given the extension.

**Rules.** Required. Chosen from the employee list.

## ActivityType {#PATimelineException.ActivityType}
<!-- alias: loc:ActivityType, dom:PATimelineException_ActivityType -->

**What it is.** The stage extended.

**Rules.** Required. One of: Plan, MidYearReview, AnnualReview, Quarter1Review, Quarter3Review.

## ExtensionDate {#PATimelineException.ExtensionDate}
<!-- alias: loc:ExtensionDate, dom:PATimelineException_ExtensionDate -->

**What it is.** The new deadline.

**Example.** `2026-07-15`

**Rules.** Required. Date value.
