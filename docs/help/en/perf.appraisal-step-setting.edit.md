---
helpId: perf.appraisal-step-setting.edit
lang: en
title: Appraisal WF Setup — Create / Edit
entity: HCMv3.PerformanceManagement.Review.AppraisalStepSetting
surfaces:
  - angular/src/app/admin/performance-management/appraisal-step-settings/create-or-edit-appraisal-step-setting-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.AppraisalStepSettings.Create]
---

## Overview

Decides who reviews a 360 degree appraisal and what each person may see and do, one step of its approval chain at a time. Each row is one step of one chain under **Request Masters**. Only the 360 appraisal reads this setup; leave, loans and the other requests are not affected.

The row that applies is the one for the step the appraisal is waiting at. An appraisal not yet sent into the workflow uses step 1 of the chain it would take.

**Nothing is set up for you.** Every box starts unticked and no rows are created automatically. A step with no row adds no reviewers and offers no review buttons on **360 Degree Appraisal**; a reviewer can still read their own rating lines there. Set up each step before appraisals reach it.

**When reviewers are added.** The boxes under **Reviewers added automatically** act at the moment approvers are assigned to the step, that is when the appraisal arrives at it, not when the step is approved. Each new reviewer gets an empty rating line for every KPI. Nobody is added twice, and a reviewer once added stays on the list, even if the step is later sent back. Every addition is checked against the form type's **Max Reviewer Limit**; if the step would go over it, the action that moves the appraisal to this step is refused with "Maximum reviewer limit of N has been reached".

**Plain reviewers.** Someone on the reviewer list who is not an approver on the current step sees only their own rating lines, whatever the boxes under **What is visible** say: no KPI list, no reviewer list and no averages. An employee who rates themselves is a plain reviewer too. The visibility boxes govern everyone else: the approvers on the step, the employee when not rating themselves, and managers looking from **Team Appraisals**.

Once the appraisal's request is approved, rejected or closed, nothing can be changed any more.

## RequestMaster {#AppraisalStepSetting.RequestMasterId}
<!-- alias: loc:RequestMaster, dom:AppraisalStepSetting_RequestMasterId -->

**What it is.** The approval chain this row belongs to. Choose the chain your appraisals are routed through; the list shows every request master, by code and name.

**Example.** PA01 - Annual Appraisal

**Why it matters.** Changing it empties the step if the new chain has no such step. The list screen can be filtered by it.

**Rules.** Required. Chosen from Request Master master data.

## Step {#AppraisalStepSetting.Step}
<!-- alias: loc:Step, dom:AppraisalStepSetting_Step -->

**What it is.** The step of the chain this row is for. Only the steps the chain defines are offered.

**Example.** 2 (the line manager's step)

**Why it matters.** Each step of a chain can have one row only: a second row for the same step is refused with "Request ... already has a setup row for step ...".

**Rules.** Required. Whole number.

## CanEmployeeSelfRate {#AppraisalStepSetting.CanEmployeeSelfRate}
<!-- alias: loc:CanEmployeeSelfRate, dom:AppraisalStepSetting_CanEmployeeSelfRate -->

**What it is.** The employee being appraised is added as a reviewer of their own appraisal when approvers are assigned to this step.

**Example.** Ticked on step 1, so the employee rates themselves first.

**Why it matters.** The employee then has their own rating lines and **Submit Review** on the self-service screen, and their score appears as the **Employee Average Score**.

**Rules.** Yes or no.

## CanAddRatings {#AppraisalStepSetting.CanAddRatings}
<!-- alias: loc:CanAddRatings, dom:AppraisalStepSetting_CanAddRatings -->

**What it is.** Each approver on this step is added as a reviewer, not as a peer, and may enter ratings.

**Example.** Ticked on the manager's step, so the manager rates every KPI.

**Why it matters.** It also opens the rating section to the approver. An approver who rates finishes by approving the step as usual, unless **Auto Submit On Last Fill** is also ticked, in which case they get **Submit Review** too.

**Rules.** Yes or no.

## AutoAddManager {#AppraisalStepSetting.AutoAddManager}
<!-- alias: loc:AutoAddManager, dom:AppraisalStepSetting_AutoAddManager -->

**What it is.** The employee's current manager, from **Employee Managers** on the day the step is assigned, is added as a reviewer, marked as a peer.

**Example.** An employee whose manager record names 1020 gets 1020 on the reviewer list.

**Why it matters.** Nobody is added when the employee has no manager record covering that day. As a peer, the manager rates and submits a review like any other peer.

**Rules.** Yes or no.

## AutoAddRelationShip {#AppraisalStepSetting.AutoAddRelationShip}
<!-- alias: loc:AutoAddRelationShip, dom:AppraisalStepSetting_AutoAddRelationShip -->

**What it is.** Everyone linked to the employee through one of the chosen relationship types, on **Employee Relationships** on the day the step is assigned, is added as a reviewer, marked as a peer.

**Example.** Ticked with the type "Dotted-line manager": the dotted-line manager joins the review automatically.

**Why it matters.** Ticking it shows the list of relationship types underneath; tick the types to use. With no type ticked, nobody is added. Only relationships to another employee count.

**Rules.** Yes or no.

## CanAddKpi {#AppraisalStepSetting.CanAddKpi}
<!-- alias: loc:CanAddKpi, dom:AppraisalStepSetting_CanAddKpi -->

**What it is.** The approver on this step may use **Copy KPI**, which fills the appraisal's KPI list from a job title, an organization unit or another employee's appraisal for the same period.

**Example.** Ticked on step 1, so the manager sets the KPIs before anyone rates.

**Why it matters.** It also decides whether the KPI list is shown at all on the self-service screen: with it unticked, the KPI section is hidden from everyone at this step.

**Rules.** Yes or no.

## CanAddPeer {#AppraisalStepSetting.CanAddPeer}
<!-- alias: loc:CanAddPeer, dom:AppraisalStepSetting_CanAddPeer -->

**What it is.** The approver on this step and the employee may add peers as reviewers with **Add Peer**. A peer cannot add further peers.

**Example.** Ticked with an allowed peer count of 3.

**Why it matters.** It switches on **Allowed Peer Count** beside it and shows the reviewer list. Adding peers also needs **Set Peer Max Limit** in Appraisal Settings: with no limit there, every addition is refused.

**Rules.** Yes or no.

## CanPeerAddRatings {#AppraisalStepSetting.CanPeerAddRatings}
<!-- alias: loc:CanPeerAddRatings, dom:AppraisalStepSetting_CanPeerAddRatings -->

**What it is.** Opens the rating section to people on the reviewer list.

**Example.** Ticked on a step where a reviewer is also one of the approvers.

**Why it matters.** Plain reviewers (those who are not approvers on this step) always see and rate their own lines, whatever this box says, so that an automatically added manager can always finish their review. In practice the box matters only for a reviewer who is also an approver on this step.

**Rules.** Yes or no.

## AutoSubmitOnLastFill {#AppraisalStepSetting.AutoSubmitOnLastFill}
<!-- alias: loc:AutoSubmitOnLastFill, dom:AppraisalStepSetting_AutoSubmitOnLastFill -->

**What it is.** When the last reviewer still pending presses **Submit Review**, the step is approved automatically, in the name of the step's first approver who has not yet approved.

**Example.** Step 3 waits for four peers; when the fourth submits, the appraisal moves on to step 4 by itself.

**Why it matters.** The approval carries the remark "Approved automatically when the last review was submitted." and the reviewer sees "All reviews are in, so the step was approved". It only fires while the request is still open at this step, so it cannot approve twice.

**Rules.** Yes or no.

## AllowedPeerCount {#AppraisalStepSetting.AllowedPeerCount}
<!-- alias: loc:AllowedPeerCount, dom:AppraisalStepSetting_AllowedPeerCount -->

**What it is.** How many peers the employee may nominate personally on this step. Only available when **Can Add Peer** is ticked.

**Example.** 3

**Why it matters.** Peers added by an approver or added automatically do not use up the employee's allowance. A fourth nomination is refused with "The employee cannot add more than 3 peers". With 0 the employee cannot nominate anyone, though the approver still can.

**Rules.** Optional. Whole number.

## CanViewRatings {#AppraisalStepSetting.CanViewRatings}
<!-- alias: loc:CanViewRatings, dom:AppraisalStepSetting_CanViewRatings -->

**What it is.** Everyone except plain reviewers (see the overview) sees the rating lines of the reviewers who are not peers, not only their own.

**Example.** Ticked on the HR step, so HR sees the employee's and the manager's ratings.

**Why it matters.** It also shows the reviewer list. Plain reviewers are never shown other people's lines.

**Rules.** Yes or no.

## CanViewPeerRatings {#AppraisalStepSetting.CanViewPeerRatings}
<!-- alias: loc:CanViewPeerRatings, dom:AppraisalStepSetting_CanViewPeerRatings -->

**What it is.** Everyone except plain reviewers (see the overview) sees the peers' rating lines.

**Example.** Unticked on the employee's step, so the employee does not see what peers wrote; ticked on the manager's step.

**Why it matters.** It also shows the reviewer list. Peers' own lines remain visible to the peers themselves.

**Rules.** Yes or no.

## CanViewOverallAverageRating {#AppraisalStepSetting.CanViewOverallAverageRating}
<!-- alias: loc:CanViewOverallAverageRating, dom:AppraisalStepSetting_CanViewOverallAverageRating -->

**What it is.** Everyone except plain reviewers (see the overview) sees the overall, peer and employee average scores beside the review.

**Example.** Ticked on the final HR step.

**Why it matters.** The averages are never shown when Appraisal Settings has **Enable Marks In PA Review** on, because marks replace the rating scale for everybody.

**Rules.** Yes or no.
