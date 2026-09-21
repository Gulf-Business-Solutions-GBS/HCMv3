---
helpId: pay.salary-register-detail.edit
lang: en
title: Salary Register Details — Create / Edit
entity: HCMv3.PayrollManagement.SalaryRegisterDetail
surfaces:
  - angular/src/app/admin/payroll-management/salary-register-details/create-or-edit-salary-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SalaryRegisterDetails.Create]
---

## Overview

One line of a payslip: a wagetype with its rate, number and amount. Lines of a locked payslip cannot be changed.

## SalaryRegisterId {#SalaryRegisterDetail.SalaryRegisterId}
<!-- alias: loc:SalaryRegisterId, dom:SalaryRegisterDetail_SalaryRegisterId -->

**What it is.** The payslip this line belongs to.

**Rules.** Required. Chosen from Salary Register master data.

## WageTypeId {#SalaryRegisterDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:SalaryRegisterDetail_WageTypeId -->

**What it is.** The wagetype of this line.

**Example.** Basic Salary

**Rules.** Required. Chosen from Wagetype master data.

## Rate {#SalaryRegisterDetail.Rate}
<!-- alias: loc:Rate, dom:SalaryRegisterDetail_Rate -->

**What it is.** The monthly rate the line was worked out from.

**Example.** `800`

**Rules.** Required. Number.

## Number {#SalaryRegisterDetail.Number}
<!-- alias: loc:Number, dom:SalaryRegisterDetail_Number -->

**What it is.** The quantity -- days, hours or units -- the line was worked out on.

**Example.** `30`

**Rules.** Required. Number.

## Amount {#SalaryRegisterDetail.Amount}
<!-- alias: loc:Amount, dom:SalaryRegisterDetail_Amount -->

**What it is.** The amount of the line.

**Example.** `800.000`

**Rules.** Required. Number.
