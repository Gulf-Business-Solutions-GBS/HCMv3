---
helpId: ben.employee-insurance-detail.edit
lang: en
title: Employee Insurance Details — Create / Edit
entity: HCMv3.BenefitsManagement.EmployeeInsuranceDetail
surfaces:
  - angular/src/app/admin/benefits-management/employee-insurance-details/create-or-edit-employee-insurance-detail-modal.component.html
audience: [admin]
permissions: [Pages.BenefitsManagement.EmployeeInsuranceDetails.Create]
---

## Overview

One payment towards the employee share of an employee insurance. The balance still to recover is the employee contribution less all of these lines, whatever their type. Each month payroll replaces that month's Through Payroll line with a new one for the insurance's installment amount, cut to what net pay and the balance allow; add External or Exemption lines by hand for amounts settled another way. The amount cannot exceed the remaining balance, and the date cannot be before the insurance's installment start date.

## EmployeeInsuranceId {#EmployeeInsuranceDetail.EmployeeInsuranceId}
<!-- alias: loc:EmployeeInsuranceId, dom:EmployeeInsuranceDetail_EmployeeInsuranceId -->

**What it is.** The employee insurance.

**Rules.** Required. Chosen from a list.

## InstallmentDate {#EmployeeInsuranceDetail.InstallmentDate}
<!-- alias: loc:InstallmentDate, dom:EmployeeInsuranceDetail_InstallmentDate -->

**What it is.** The date of the payment.

**Rules.** Required. Date value.

## InstallmentAmount {#EmployeeInsuranceDetail.InstallmentAmount}
<!-- alias: loc:InstallmentAmount, dom:EmployeeInsuranceDetail_InstallmentAmount -->

**What it is.** The amount paid.

**Example.** 15.000

**Rules.** Optional. Number.

## InstallmentType {#EmployeeInsuranceDetail.InstallmentType}
<!-- alias: loc:InstallmentType, dom:EmployeeInsuranceDetail_InstallmentType -->

**What it is.** How the amount was settled.

**Example.** External

**Why it matters.** Through Payroll lines are written by payroll. End Of Service lines dated in the month are deducted in an end-of-service off-cycle run. External and Exemption only reduce the balance.

**Rules.** Required. One of: Through Payroll, External, End Of Service, Exemption.
