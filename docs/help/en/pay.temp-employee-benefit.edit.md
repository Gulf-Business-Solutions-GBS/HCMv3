---
helpId: pay.temp-employee-benefit.edit
lang: en
title: Temp Employee Benefits — Create / Edit
entity: HCMv3.PayrollManagement.TempEmployeeBenefit
surfaces:
  - angular/src/app/admin/payroll-management/temp-employee-benefits/create-or-edit-temp-employee-benefit-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TempEmployeeBenefits.Create]
---

## Overview

Which benefit grade an employee is on. Each employee can be on one grade only.

Not read at present: it links the employee to the grade's reference sheet, and payroll does not use it.

## EmployeeId {#TempEmployeeBenefit.EmployeeId}
<!-- alias: loc:EmployeeId, dom:TempEmployeeBenefit_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## TempBenefitTypeId {#TempEmployeeBenefit.TempBenefitTypeId}
<!-- alias: loc:TempBenefitTypeId, dom:TempEmployeeBenefit_TempBenefitTypeId -->

**What it is.** The benefit grade.

**Example.** Grade 5

**Rules.** Required. Chosen from Temp Benefit Type master data.
