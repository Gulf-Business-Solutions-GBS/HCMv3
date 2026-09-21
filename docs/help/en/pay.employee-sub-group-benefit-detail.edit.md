---
helpId: pay.employee-sub-group-benefit-detail.edit
lang: en
title: Employee Sub Group Benefit Details — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupBenefitDetail
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/benefits/benefit-details/create-or-edit-employee-sub-group-benefit-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupBenefitDetails.Create]
---

## Overview

The amount of a sub group benefit per kind of dependant, with an age limit.

Not read at present: payroll does not use this list, and it is kept for reference only.

## DependantType {#EmployeeSubGroupBenefitDetail.DependantTypeId}
<!-- alias: loc:DependantType, dom:EmployeeSubGroupBenefitDetail_DependantTypeId -->

**What it is.** The kind of dependant.

**Example.** Child

**Rules.** Optional. Chosen from Dependant Type master data.

## Amount {#EmployeeSubGroupBenefitDetail.Amount}
<!-- alias: loc:Amount, dom:EmployeeSubGroupBenefitDetail_Amount -->

**What it is.** The amount per dependant of that kind.

**Example.** `500`

**Rules.** Optional. Number.

## AgeLimit {#EmployeeSubGroupBenefitDetail.AgeLimit}
<!-- alias: loc:AgeLimit, dom:EmployeeSubGroupBenefitDetail_AgeLimit -->

**What it is.** The oldest age at which a dependant counts.

**Example.** `21`

**Rules.** Optional. Number.
