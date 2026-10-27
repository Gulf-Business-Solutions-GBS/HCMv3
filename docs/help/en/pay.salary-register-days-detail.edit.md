---
helpId: pay.salary-register-days-detail.edit
lang: en
title: Salary Register Days Details — Create / Edit
entity: HCMv3.PayrollManagement.SalaryRegisterDaysDetail
surfaces:
  - angular/src/app/admin/payroll-management/salary-register-days-details/create-or-edit-salary-register-days-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SalaryRegisterDaysDetails.Create]
---

## Overview

The breakdown of a payslip line by date -- overtime day by day, for example, or leave paid across months. Breakdowns of a locked payslip cannot be changed.

## SalaryRegisterDetailId {#SalaryRegisterDaysDetail.SalaryRegisterDetailId}
<!-- alias: loc:SalaryRegisterDetailId, dom:SalaryRegisterDaysDetail_SalaryRegisterDetailId -->

**What it is.** The payslip line this breakdown belongs to.

**Rules.** Required. Chosen from Salary Register Detail master data.

## Period {#SalaryRegisterDaysDetail.Period}
<!-- alias: loc:Period, dom:SalaryRegisterDaysDetail_Period -->

**What it is.** The date this part of the line is for.

**Example.** `2026-03-14`

**Rules.** Required. Date value.

## Rate {#SalaryRegisterDaysDetail.Rate}
<!-- alias: loc:Rate, dom:SalaryRegisterDaysDetail_Rate -->

**What it is.** The rate for that date.

**Example.** `800`

**Rules.** Required. Number.

## Number {#SalaryRegisterDaysDetail.Number}
<!-- alias: loc:Number, dom:SalaryRegisterDaysDetail_Number -->

**What it is.** The days, hours or units for that date.

**Example.** `2`

**Rules.** Required. Number.

## Amount {#SalaryRegisterDaysDetail.Amount}
<!-- alias: loc:Amount, dom:SalaryRegisterDaysDetail_Amount -->

**What it is.** The amount for that date.

**Example.** `12.500`

**Rules.** Required. Number.
