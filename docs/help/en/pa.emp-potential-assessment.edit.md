---
helpId: pa.emp-potential-assessment.edit
lang: en
title: Employee Potential Assessments — Create / Edit
entity: HCMv3.SuccessionManagement.EmpPotentialAssessment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/potential-assessments/create-or-edit-employee-potential-assessment-modal.component.html
audience: [admin]
permissions: [Pages.SuccessionManagement.EmpPotentialAssessments.Create]
---

## Overview

An assessment of the employee's potential over a period, for succession planning. Periods for the same employee cannot overlap.

It is recorded for reference; no calculation or report reads it at present.

## Description {#EmpPotentialAssessment.Description}
<!-- alias: loc:Description, dom:EmpPotentialAssessment_Description -->

**What it is.** Notes on the assessment.

**Example.** Ready for a team lead role within a year.

**Rules.** Optional. Maximum 2000 characters.

## Score {#EmpPotentialAssessment.Score}
<!-- alias: loc:Score, dom:EmpPotentialAssessment_Score -->

**What it is.** The potential score.

**Example.** 3.8

**Rules.** Required. Number.

## Source {#EmpPotentialAssessment.Source}
<!-- alias: loc:Source, dom:EmpPotentialAssessment_Source -->

**What it is.** Whether the score came from a live assessment or was entered by hand.

**Example.** Manual

**Rules.** Required. One of: Live, Manual.

## RatingId {#EmpPotentialAssessment.RatingId}
<!-- alias: loc:RatingId, dom:EmpPotentialAssessment_RatingId -->

**What it is.** The succession rating.

**Example.** High Potential

**Rules.** Required. Chosen from Succession Rating master data.

:::admin
Maintained under Succession Management.
:::

## NineBoxRatingId {#EmpPotentialAssessment.NineBoxRatingId}
<!-- alias: loc:NineBoxRatingId, dom:EmpPotentialAssessment_NineBoxRatingId -->

**What it is.** The nine-box rating.

**Example.** Future Star

**Rules.** Required. Chosen from Nine Box Rating master data.

:::admin
Maintained under Succession Management.
:::
