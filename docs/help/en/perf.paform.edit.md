---
helpId: perf.paform.edit
lang: en
title: PA Forms — Create / Edit
entity: HCMv3.PerformanceManagement.PAForm
surfaces:
  - angular/src/app/admin/performance-management/pa-forms/create-or-edit-pa-form-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PAForms.Create]
---

## Overview

An employee's appraisal form for a period, in the older model: the plan, quarterly reviews, mid-year and year-end reviews, with sign-off and summary comments. Its KPI lines are held on the plan and review lists. Each employee can have one form per period.

The submission boxes record progress; they do not lock the form. The dashboard counts the plan, mid-year and year-end submissions, and the PA summary report shows the moderated score and comments.

## PAPeriod {#PAForm.PAPeriodId}
<!-- alias: loc:PAPeriod, dom:PAForm_PAPeriodId -->

**What it is.** The appraisal period.

**Example.** FY 2026

**Rules.** Required. Chosen from PA Period master data.

## Employee {#PAForm.EmployeeId}
<!-- alias: loc:Employee, dom:PAForm_EmployeeId -->

**What it is.** The employee being appraised.

**Rules.** Required. Chosen from the employee list.

## ModeratedScore {#PAForm.ModeratedScore}
<!-- alias: loc:ModeratedScore, dom:PAForm_ModeratedScore -->

**What it is.** The final score after moderation, shown on the PA summary and appraisal reports.

**Example.** `78`

**Rules.** Optional. Number.

## PlanSubmitByAppraiser {#PAForm.PlanSubmitByAppraiser}
<!-- alias: loc:PlanSubmitByAppraiser, dom:PAForm_PlanSubmitByAppraiser -->

**What it is.** Whether the appraiser has submitted the plan.

**Why it matters.** Counted on the performance appraisal dashboard.

**Rules.** Yes or no.

## PlanSubmitByAppraisee {#PAForm.PlanSubmitByAppraisee}
<!-- alias: loc:PlanSubmitByAppraisee, dom:PAForm_PlanSubmitByAppraisee -->

**What it is.** Whether the appraisee has submitted the plan.

**Why it matters.** Counted on the performance appraisal dashboard.

**Rules.** Yes or no.

## Quarter1SubmitByAppraiser {#PAForm.Quarter1SubmitByAppraiser}
<!-- alias: loc:Quarter1SubmitByAppraiser, dom:PAForm_Quarter1SubmitByAppraiser -->

**What it is.** Whether the appraiser has submitted the first-quarter review.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## Quarter1SubmitByAppraisee {#PAForm.Quarter1SubmitByAppraisee}
<!-- alias: loc:Quarter1SubmitByAppraisee, dom:PAForm_Quarter1SubmitByAppraisee -->

**What it is.** Whether the appraisee has submitted the first-quarter review.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## MRSubmitByAppraiser {#PAForm.MRSubmitByAppraiser}
<!-- alias: loc:MRSubmitByAppraiser, dom:PAForm_MRSubmitByAppraiser -->

**What it is.** Whether the appraiser has submitted the mid-year review.

**Why it matters.** Counted on the performance appraisal dashboard.

**Rules.** Yes or no.

## MRSubmitByAppraisee {#PAForm.MRSubmitByAppraisee}
<!-- alias: loc:MRSubmitByAppraisee, dom:PAForm_MRSubmitByAppraisee -->

**What it is.** Whether the appraisee has submitted the mid-year review.

**Why it matters.** Counted on the performance appraisal dashboard.

**Rules.** Yes or no.

## Quarter3SubmitByAppraiser {#PAForm.Quarter3SubmitByAppraiser}
<!-- alias: loc:Quarter3SubmitByAppraiser, dom:PAForm_Quarter3SubmitByAppraiser -->

**What it is.** Whether the appraiser has submitted the third-quarter review.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## Quarter3SubmitByAppraisee {#PAForm.Quarter3SubmitByAppraisee}
<!-- alias: loc:Quarter3SubmitByAppraisee, dom:PAForm_Quarter3SubmitByAppraisee -->

**What it is.** Whether the appraisee has submitted the third-quarter review.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## ERSubmitByAppraiser {#PAForm.ERSubmitByAppraiser}
<!-- alias: loc:ERSubmitByAppraiser, dom:PAForm_ERSubmitByAppraiser -->

**What it is.** Whether the appraiser has submitted the year-end review.

**Why it matters.** Counted on the performance appraisal dashboard.

**Rules.** Yes or no.

## ERSubmitByAppraisee {#PAForm.ERSubmitByAppraisee}
<!-- alias: loc:ERSubmitByAppraisee, dom:PAForm_ERSubmitByAppraisee -->

**What it is.** Whether the appraisee has submitted the year-end review.

**Why it matters.** Counted on the performance appraisal dashboard.

**Rules.** Yes or no.

## AppraiserSignedOff {#PAForm.AppraiserSignedOff}
<!-- alias: loc:AppraiserSignedOff, dom:PAForm_AppraiserSignedOff -->

**What it is.** Whether the appraiser has signed off the form.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## AppraiseeSignedOff {#PAForm.AppraiseeSignedOff}
<!-- alias: loc:AppraiseeSignedOff, dom:PAForm_AppraiseeSignedOff -->

**What it is.** Whether the appraisee has signed off the form.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## AppraiseeStrength {#PAForm.AppraiseeStrength}
<!-- alias: loc:AppraiseeStrength, dom:PAForm_AppraiseeStrength -->

**What it is.** The appraisee's strengths. Shown on the PA summary report.

**Rules.** Optional. Maximum 4000 characters.

## AreasOfImprovement {#PAForm.AreasOfImprovement}
<!-- alias: loc:AreasOfImprovement, dom:PAForm_AreasOfImprovement -->

**What it is.** Areas to improve. Shown on the PA summary report.

**Rules.** Optional. Maximum 4000 characters.

## ReviewofPreviousGoals {#PAForm.ReviewofPreviousGoals}
<!-- alias: loc:ReviewofPreviousGoals, dom:PAForm_ReviewofPreviousGoals -->

**What it is.** A review of the previous period's goals.

**Rules.** Optional. Maximum 4000 characters.

## NewlySetGoals {#PAForm.NewlySetGoals}
<!-- alias: loc:NewlySetGoals, dom:PAForm_NewlySetGoals -->

**What it is.** Goals set for the next period. Shown on the PA summary report.

**Rules.** Optional. Maximum 4000 characters.

## OverallComments {#PAForm.OverallComments}
<!-- alias: loc:OverallComments, dom:PAForm_OverallComments -->

**What it is.** Overall comments. Shown on the PA summary report.

**Rules.** Optional. Maximum 4000 characters.
