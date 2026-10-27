---
helpId: perf.paemployee-reviewer.edit
lang: en
title: PA Employee Reviewers — Create / Edit
entity: HCMv3.PerformanceManagement.PAEmployeeReviewer
surfaces:
  - angular/src/app/admin/performance-management/pa-employee-detail/pa-employee-reviewers/create-or-edit-pa-employee-reviewer-modal.component.html
audience: [admin]
permissions: []
---

## Overview

A person reviewing an appraisal -- the manager, or a peer. The number of reviewers is limited by the form type's maximum, a person can review an appraisal once, and only peer reviewers can be deleted.

## Employee {#PAEmployeeReviewer.EmployeeId}
<!-- alias: loc:Employee, dom:PAEmployeeReviewer_EmployeeId -->

**What it is.** The reviewer.

**Rules.** Optional. Chosen from the employee list.

## PeerAddedBy {#PAEmployeeReviewer.PeerAddedById}
<!-- alias: loc:PeerAddedBy, dom:PAEmployeeReviewer_PeerAddedById -->

**What it is.** Who added the peer reviewer.

**Rules.** Optional. Chosen from the employee list.

## IsPeer {#PAEmployeeReviewer.IsPeer}
<!-- alias: loc:IsPeer, dom:PAEmployeeReviewer_IsPeer -->

**What it is.** Whether the reviewer is a peer rather than the manager.

**Why it matters.** The manager evaluation report uses only non-peer reviewers.

**Rules.** Yes or no.

## CustomTextField1 {#PAEmployeeReviewer.CustomTextField1}
<!-- alias: loc:CustomTextField1, dom:PAEmployeeReviewer_CustomTextField1 -->

**What it is.** The appraisee's strengths, in the reviewer's words.

**Why it matters.** Shown as strengths on the reviewer summary report, from the manager's review.

**Rules.** Optional. Maximum 4000 characters.

## CustomTextField2 {#PAEmployeeReviewer.CustomTextField2}
<!-- alias: loc:CustomTextField2, dom:PAEmployeeReviewer_CustomTextField2 -->

**What it is.** Areas the appraisee should improve.

**Why it matters.** Shown on the reviewer summary report, from the manager's review.

**Rules.** Optional. Maximum 4000 characters.

## CustomTextField3 {#PAEmployeeReviewer.CustomTextField3}
<!-- alias: loc:CustomTextField3, dom:PAEmployeeReviewer_CustomTextField3 -->

**What it is.** The overall evaluation and training needs.

**Why it matters.** Shown on the reviewer summary report, from the manager's review.

**Rules.** Optional. Maximum 4000 characters.
