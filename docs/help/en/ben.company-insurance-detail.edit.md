---
helpId: ben.company-insurance-detail.edit
lang: en
title: Company Insurance Details — Create / Edit
entity: HCMv3.BenefitsManagement.CompanyInsuranceDetail
surfaces:
  - angular/src/app/admin/benefits-management/company-insurance-details/create-or-edit-company-insurance-detail-modal.component.html
audience: [admin]
permissions: [Pages.BenefitsManagement.CompanyInsuranceDetails.Create]
---

## Overview

One payment towards the company share of an employee insurance. The balance still to recover is the company contribution less all of these lines, whatever their type. Each month payroll replaces that month's Through Payroll line with a new one for the insurance's installment amount, cut to what net pay and the balance allow; add External or Exemption lines by hand for amounts settled another way. The amount cannot exceed the remaining balance, and the date cannot be before the insurance's installment start date.

Opened from an insurance's Insurance Installments dialog, the insurance is already filled in and cannot be changed, and the type starts as External.

## EmployeeInsuranceId {#CompanyInsuranceDetail.EmployeeInsuranceId}
<!-- alias: loc:EmployeeInsuranceId, dom:CompanyInsuranceDetail_EmployeeInsuranceId -->

**What it is.** The employee insurance.

**Rules.** Required. Chosen from a list.

## InstallmentDate {#CompanyInsuranceDetail.InstallmentDate}
<!-- alias: loc:InstallmentDate, dom:CompanyInsuranceDetail_InstallmentDate -->

**What it is.** The date of the payment.

**Rules.** Required. Date value.

## InstallmentAmount {#CompanyInsuranceDetail.InstallmentAmount}
<!-- alias: loc:InstallmentAmount, dom:CompanyInsuranceDetail_InstallmentAmount -->

**What it is.** The amount paid.

**Example.** 15.000

**Rules.** Optional. Number.

## InstallmentType {#CompanyInsuranceDetail.InstallmentType}
<!-- alias: loc:InstallmentType, dom:CompanyInsuranceDetail_InstallmentType -->

**What it is.** How the amount was settled.

**Example.** External

**Why it matters.** Through Payroll lines are written by payroll. End Of Service lines dated in the month are deducted in an end-of-service off-cycle run. External and Exemption only reduce the balance.

**Rules.** Required. One of: Through Payroll, External, End Of Service, Exemption.
