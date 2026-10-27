---
helpId: sys.appraisal-settings.edit
lang: en
title: Appraisal settings
entity: HCMv3.SystemSettings.ManageAppraisalSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-appraisal-settings/create-or-edit-manage-appraisal-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageAppraisalSettings]
---

## Overview

How performance appraisals are run: the cycle, how marks are given and peer review.

None of these settings is read at present: the features they control were not carried over from the legacy system, so changing them has no effect.

There is one settings record per company; it cannot be deleted.

## AppraisalCycle {#ManageAppraisalSettings.AppraisalCycle}
<!-- alias: loc:AppraisalCycle, dom:AppraisalSettings_AppraisalCycle -->

**What it is.** How often appraisals are held.

**Example.** Annually

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Required. One of: Quarterly, Half Yearly, Annually.

## MarksOn {#ManageAppraisalSettings.MarksOn}
<!-- alias: loc:MarksOn, dom:AppraisalSettings_MarksOn -->

**What it is.** Whether appraisal marks are based on ratings or weights.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Required. One of: Ratings, Weight.

## EnablePAPeerReview {#ManageAppraisalSettings.EnablePAPeerReview}
<!-- alias: loc:EnablePAPeerReview, dom:AppraisalSettings_EnablePAPeerReview -->

**What it is.** Whether peers can review each other.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableMarksInPAReview {#ManageAppraisalSettings.EnableMarksInPAReview}
<!-- alias: loc:EnableMarksInPAReview, dom:AppraisalSettings_EnableMarksInPAReview -->

**What it is.** Whether marks are entered in the review.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## SetPeerMinLimit {#ManageAppraisalSettings.SetPeerMinLimit}
<!-- alias: loc:SetPeerMinLimit, dom:AppraisalSettings_SetPeerMinLimit -->

**What it is.** The fewest peers to nominate.

**Example.** `2`

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## SetPeerMaxLimit {#ManageAppraisalSettings.SetPeerMaxLimit}
<!-- alias: loc:SetPeerMaxLimit, dom:AppraisalSettings_SetPeerMaxLimit -->

**What it is.** The most peers to nominate.

**Example.** `5`

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.
