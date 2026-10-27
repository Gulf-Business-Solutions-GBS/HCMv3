---
helpId: pa.emp-talent-matrix-data.edit
lang: en
title: Employee Talent Matrix Data — Create / Edit
entity: HCMv3.SuccessionManagement.EmpTalentMatrixData
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/talent-matrix/create-or-edit-employee-talent-matrix-modal.component.html
audience: [admin]
permissions: [Pages.SuccessionManagement.EmpTalentMatrixDatas.Create]
---

## Overview

The employee's position on the nine-box talent matrix for a period. Periods for the same employee cannot overlap.

It is recorded for reference; no calculation or report reads it at present.

## PerformanceRatingId {#EmpTalentMatrixData.PerformanceRatingId}
<!-- alias: loc:PerformanceRatingId, dom:EmpTalentMatrixData_PerformanceRatingId -->

**What it is.** The performance axis rating.

**Example.** High

**Rules.** Required. Chosen from Nine Box Rating master data.

:::admin
Maintained under Succession Management.
:::

## PotentialRatingId {#EmpTalentMatrixData.PotentialRatingId}
<!-- alias: loc:PotentialRatingId, dom:EmpTalentMatrixData_PotentialRatingId -->

**What it is.** The potential axis rating.

**Example.** Medium

**Rules.** Required. Chosen from Nine Box Rating master data.

:::admin
Maintained under Succession Management.
:::

## OverallRatingId {#EmpTalentMatrixData.OverallRatingId}
<!-- alias: loc:OverallRatingId, dom:EmpTalentMatrixData_OverallRatingId -->

**What it is.** The overall box the employee falls in.

**Example.** High Performer

**Rules.** Required. Chosen from Nine Box Rating master data.

:::admin
Maintained under Succession Management.
:::

## CalibratedRatingId {#EmpTalentMatrixData.CalibratedRatingId}
<!-- alias: loc:CalibratedRatingId, dom:EmpTalentMatrixData_CalibratedRatingId -->

**What it is.** The box after calibration review.

**Example.** Star

**Rules.** Required. Chosen from Nine Box Rating master data.

:::admin
Maintained under Succession Management.
:::
