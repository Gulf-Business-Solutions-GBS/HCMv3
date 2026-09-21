---
helpId: mp.manpower-planning-detail.edit
lang: en
title: Manpower Planning Details — Create / Edit
entity: HCMv3.ManpowerPlanningManagement.ManpowerPlanningDetail
surfaces:
  - angular/src/app/admin/manpower-planning-management/manpower-planning-detail/create-or-edit-manpower-planning-detail-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.ManpowerPlanningDetails.Create]
---

## Overview

One job title in a manpower plan, with its budget for each month. Once the line has a position, each month's amount (other than 0) must lie between that position's minimum and maximum salary.

## JobTitle {#ManpowerPlanningDetail.JobTitleId}
<!-- alias: loc:JobTitle, dom:ManpowerPlanningDetail_JobTitleId -->

**What it is.** The job title requested.

**Example.** Accountant

**Why it matters.** On approval it names the new position and becomes its job title.

**Rules.** Required. Chosen from Job Title master data.

## Holder {#ManpowerPlanningDetail.HolderId}
<!-- alias: loc:Holder, dom:ManpowerPlanningDetail_HolderId -->

**What it is.** The employee expected to fill the position.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from the employee list.

## January {#ManpowerPlanningDetail.January}
<!-- alias: loc:January, dom:ManpowerPlanningDetail_January -->

**What it is.** The budgeted monthly amount for January.

**Example.** 850.000

**Why it matters.** When the plan is approved, the new position's minimum, mid and maximum salary are all set to this amount.

**Rules.** Optional. Number.

## February {#ManpowerPlanningDetail.February}
<!-- alias: loc:February, dom:ManpowerPlanningDetail_February -->

**What it is.** The budgeted monthly amount for February.

**Rules.** Optional. Number.

## March {#ManpowerPlanningDetail.March}
<!-- alias: loc:March, dom:ManpowerPlanningDetail_March -->

**What it is.** The budgeted monthly amount for March.

**Rules.** Optional. Number.

## April {#ManpowerPlanningDetail.April}
<!-- alias: loc:April, dom:ManpowerPlanningDetail_April -->

**What it is.** The budgeted monthly amount for April.

**Rules.** Optional. Number.

## May {#ManpowerPlanningDetail.May}
<!-- alias: loc:May, dom:ManpowerPlanningDetail_May -->

**What it is.** The budgeted monthly amount for May.

**Rules.** Optional. Number.

## June {#ManpowerPlanningDetail.June}
<!-- alias: loc:June, dom:ManpowerPlanningDetail_June -->

**What it is.** The budgeted monthly amount for June.

**Rules.** Optional. Number.

## July {#ManpowerPlanningDetail.July}
<!-- alias: loc:July, dom:ManpowerPlanningDetail_July -->

**What it is.** The budgeted monthly amount for July.

**Rules.** Optional. Number.

## August {#ManpowerPlanningDetail.August}
<!-- alias: loc:August, dom:ManpowerPlanningDetail_August -->

**What it is.** The budgeted monthly amount for August.

**Rules.** Optional. Number.

## September {#ManpowerPlanningDetail.September}
<!-- alias: loc:September, dom:ManpowerPlanningDetail_September -->

**What it is.** The budgeted monthly amount for September.

**Rules.** Optional. Number.

## October {#ManpowerPlanningDetail.October}
<!-- alias: loc:October, dom:ManpowerPlanningDetail_October -->

**What it is.** The budgeted monthly amount for October.

**Rules.** Optional. Number.

## November {#ManpowerPlanningDetail.November}
<!-- alias: loc:November, dom:ManpowerPlanningDetail_November -->

**What it is.** The budgeted monthly amount for November.

**Rules.** Optional. Number.

## December {#ManpowerPlanningDetail.December}
<!-- alias: loc:December, dom:ManpowerPlanningDetail_December -->

**What it is.** The budgeted monthly amount for December.

**Rules.** Optional. Number.
