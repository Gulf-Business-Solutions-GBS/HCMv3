---
helpId: pay.employee-sub-group-benefit.edit
lang: en
title: Employee Sub Group Benefits — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupBenefit
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/benefits/create-or-edit-employee-sub-group-benefit-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupBenefits.Create]
---

## Overview

A benefit type the sub group is entitled to, with a maximum amount.

Not read at present: payroll does not use this list, and it is kept for reference only.

## BenefitType {#EmployeeSubGroupBenefit.BenefitTypeId}
<!-- alias: loc:BenefitType, dom:EmployeeSubGroupBenefit_BenefitTypeId -->

**What it is.** The benefit type.

**Example.** Education Allowance

**Rules.** Optional. Chosen from Benefit Type master data.

## MaxAmount {#EmployeeSubGroupBenefit.MaxAmount}
<!-- alias: loc:MaxAmount, dom:EmployeeSubGroupBenefit_MaxAmount -->

**What it is.** The most the benefit can pay.

**Example.** `2000`

**Rules.** Optional. Number.
