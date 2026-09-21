---
helpId: perf.patimeline.edit
lang: en
title: PA Timelines — Create / Edit
entity: HCMv3.PerformanceManagement.PATimeline
surfaces:
  - angular/src/app/admin/performance-management/pa-timelines/create-or-edit-pa-timeline-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PATimelines.Create]
---

## Overview

The dates of one stage of a period's appraisal -- planning, mid-year review, year-end review. A period can have each stage once.

The performance appraisal and PA summary reports use these dates to find the periods that fall in the report's date range. The dates do not stop anyone submitting outside them.

## PAPeriod {#PATimeline.PAPeriodId}
<!-- alias: loc:PAPeriod, dom:PATimeline_PAPeriodId -->

**What it is.** The appraisal period.

**Example.** FY 2026

**Rules.** Required. Chosen from PA Period master data.

## ActivityType {#PATimeline.ActivityType}
<!-- alias: loc:ActivityType, dom:PATimeline_ActivityType -->

**What it is.** The stage the dates are for.

**Example.** Mid-Year Review

**Rules.** Required. One of: Plan, MidYearReview, AnnualReview, Quarter1Review, Quarter3Review.
