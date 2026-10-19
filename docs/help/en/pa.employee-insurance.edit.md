---
helpId: pa.employee-insurance.edit
lang: en
title: Employee Insurances — Create / Edit
entity: HCMv3.BenefitsManagement.EmployeeInsurance
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/insurances/create-or-edit-employee-insurance-modal.component.html
audience: [admin]
permissions: [Pages.BenefitsManagement.EmployeeInsurances.Create]
---

## Overview

An insurance enrolment for the employee or a dependant, and how its cost is shared and recovered.

Payroll pays the employee and company contributions in the payroll month in which the insurance starts, then recovers them in instalments from the instalment start date until each balance is cleared. The insurance amount must equal the employee contribution plus the company contribution.

**Insurance Installments**, in an insurance's row menu on the employee's Insurances section, opens its instalments in one dialog: the employee share and the company share in two lists, under the insurance amount and each contribution with its remaining balance. Instalments are added and changed there with the same dialogs, and the same rules, as the Employee Insurance Details and Company Insurance Details screens, with the insurance already filled in. Instalments written by the payroll run (Through Payroll) are shown but cannot be edited or deleted there: the next run of that period deletes and rewrites them.

## InsurancePlanId {#EmployeeInsurance.InsurancePlanId}
<!-- alias: loc:InsurancePlanId, dom:EmployeeInsurance_InsurancePlanId -->

**What it is.** The insurance plan.

**Example.** Gold Medical

**Why it matters.** Recorded only; payroll does not read it.

**Rules.** Required. Chosen from Insurance Plan master data.

## InsuranceProviderId {#EmployeeInsurance.InsuranceProviderId}
<!-- alias: loc:InsuranceProviderId, dom:EmployeeInsurance_InsuranceProviderId -->

**What it is.** The insurance company.

**Example.** Gulf Insurance

**Why it matters.** Recorded only; payroll does not read it.

**Rules.** Required. Chosen from Insurance Provider master data.

## DependantId {#EmployeeInsurance.DependantId}
<!-- alias: loc:DependantId, dom:EmployeeInsurance_DependantId -->

**What it is.** The dependant covered, when the insurance is not for the employee.

**Example.** Sara Al-Mutairi (Daughter)

**Why it matters.** Dependant insurance is paid and recovered in the same way as the employee's own.

**Rules.** Optional. Chosen from a list.

## InstallmentStartDate {#EmployeeInsurance.InstallmentStartDate}
<!-- alias: loc:InstallmentStartDate, dom:EmployeeInsurance_InstallmentStartDate -->

**What it is.** The date payroll starts recovering the contributions.

**Example.** 1 Oct 2026

**Why it matters.** Cannot be before the start date.

**Rules.** Required. Date value.

## InsuranceAmount {#EmployeeInsurance.InsuranceAmount}
<!-- alias: loc:InsuranceAmount, dom:EmployeeInsurance_InsuranceAmount -->

**What it is.** The total cost of the insurance.

**Example.** 600.000

**Why it matters.** Cannot be zero, and must equal the employee contribution plus the company contribution.

**Rules.** Optional. Number.

## CompanyInterval {#EmployeeInsurance.CompanyInterval}
<!-- alias: loc:CompanyInterval, dom:EmployeeInsurance_CompanyInterval -->

**What it is.** How often the company contribution is expressed.

**Example.** Yearly

**Why it matters.** Recorded only; payroll does not read it.

**Rules.** Required. One of: Monthly, Yearly.

## CompanyContribution {#EmployeeInsurance.CompanyContribution}
<!-- alias: loc:CompanyContribution, dom:EmployeeInsurance_CompanyContribution -->

**What it is.** The company's share of the cost.

**Example.** 400.000

**Why it matters.** Paid in the month the insurance starts, then recovered in instalments.

**Rules.** Optional. Number.

## InstallmentCompanyDedAmount {#EmployeeInsurance.InstallmentCompanyDedAmount}
<!-- alias: loc:InstallmentCompanyDedAmount, dom:EmployeeInsurance_InstallmentCompanyDedAmount -->

**What it is.** The monthly instalment recovering the company contribution.

**Example.** 33.333

**Why it matters.** Payroll never recovers more than the balance left, or more than the employee's net pay allows.

**Rules.** Optional. Number.

## BalanceCompanyContribution {#EmployeeInsurance.BalanceCompanyContribution}
<!-- alias: loc:BalanceCompanyContribution, dom:EmployeeInsurance_BalanceCompanyContribution -->

**What it is.** How much of the company contribution is still to be recovered. Calculated, not typed in.

**Example.** 360.000 of a 600.000 contribution, after four instalments of 60.000

**Why it matters.** It starts at the full contribution and goes down as instalments are recorded or deducted in payroll. Payroll deducts the insurance only while a balance remains, and the last deduction never goes above it.

**Rules.** Optional. Number.

## EmployeeInterval {#EmployeeInsurance.EmployeeInterval}
<!-- alias: loc:EmployeeInterval, dom:EmployeeInsurance_EmployeeInterval -->

**What it is.** How often the employee contribution is expressed.

**Example.** Yearly

**Why it matters.** Recorded only; payroll does not read it.

**Rules.** Required. One of: Monthly, Yearly.

## EmployeeContribution {#EmployeeInsurance.EmployeeContribution}
<!-- alias: loc:EmployeeContribution, dom:EmployeeInsurance_EmployeeContribution -->

**What it is.** The employee's share of the cost.

**Example.** 200.000

**Why it matters.** Paid in the month the insurance starts, then recovered in instalments.

**Rules.** Optional. Number.

## InstallmentEmployeeDedAmount {#EmployeeInsurance.InstallmentEmployeeDedAmount}
<!-- alias: loc:InstallmentEmployeeDedAmount, dom:EmployeeInsurance_InstallmentEmployeeDedAmount -->

**What it is.** The monthly instalment recovering the employee contribution.

**Example.** 16.667

**Why it matters.** Payroll never recovers more than the balance left, or more than the employee's net pay allows.

**Rules.** Optional. Number.

## BalanceEmployeeContribution {#EmployeeInsurance.BalanceEmployeeContribution}
<!-- alias: loc:BalanceEmployeeContribution, dom:EmployeeInsurance_BalanceEmployeeContribution -->

**What it is.** How much of the employee contribution is still to be recovered. Calculated, not typed in.

**Example.** 120.000

**Why it matters.** Works the same way as Balance Company Contribution, for the employee's share.

**Rules.** Optional. Number.
