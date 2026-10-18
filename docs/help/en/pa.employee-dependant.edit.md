---
helpId: pa.employee-dependant.edit
lang: en
title: Employee Dependants — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeDependant
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/dependants/create-or-edit-employee-dependant-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDependants.Create]
---

## Overview

A family member who depends on the employee -- a spouse, child or parent -- and the benefits they count towards. Dependants of the same ticket type for the same employee cannot overlap.

Dependants feed health reimbursement entitlement, dependant air tickets, and the child allowance in social security.

## NameEn {#EmployeeDependant.NameEn}

**What it is.** The dependant's name in English.

**Example.** Sara Ahmed Al-Mutairi

**Rules.** Required. Maximum 256 characters.

## NameAr {#EmployeeDependant.NameAr}

**What it is.** The dependant's name in Arabic.

**Example.** سارة أحمد المطيري

**Rules.** Required. Maximum 256 characters.

## DependantTypeId {#EmployeeDependant.DependantTypeId}
<!-- alias: loc:DependantTypeId, dom:EmployeeDependant_DependantTypeId -->

**What it is.** How the dependant is related to the employee.

**Example.** Daughter

**Why it matters.** Health reimbursement entitlement is set per dependant type on the employee's grade.

**Rules.** Required. Chosen from Dependant Type master data.

:::admin
Maintained under General Master Info → Dependant Types.
:::

## BirthDate {#EmployeeDependant.BirthDate}
<!-- alias: loc:BirthDate, dom:EmployeeDependant_BirthDate -->

**What it is.** The dependant's date of birth.

**Example.** 12 May 2015

**Why it matters.** Health reimbursement applies the dependant type's age limit from it, and air ticket fares can depend on the dependant's age.

**Rules.** Required. Date value.

## TicketTypeId {#EmployeeDependant.TicketTypeId}
<!-- alias: loc:TicketTypeId, dom:EmployeeDependant_TicketTypeId -->

**What it is.** The ticket type the dependant's air tickets use.

**Example.** Dependant 1

**Why it matters.** The time engine accrues dependant tickets for the dependant in force on each day.

**Rules.** Required. Chosen from Ticket Type master data.

## SpecialNeed {#EmployeeDependant.SpecialNeed}
<!-- alias: loc:SpecialNeed, dom:EmployeeDependant_SpecialNeed -->

**What it is.** Whether the dependant has special needs.

**Why it matters.** Social security pays the special-needs child allowance for these dependants instead of the ordinary child allowance.

**Rules.** Yes or no.

## IncludeInSocialSecurity {#EmployeeDependant.IncludeInSocialSecurity}
<!-- alias: loc:IncludeInSocialSecurity, dom:EmployeeDependant_IncludeInSocialSecurity -->

**What it is.** Whether the dependant counts for social security child allowances.

**Why it matters.** Only ticked dependants are counted, and only for male employees.

**Rules.** Yes or no.

## IncludeInHealthInsurance {#EmployeeDependant.IncludeInHealthInsurance}
<!-- alias: loc:IncludeInHealthInsurance, dom:EmployeeDependant_IncludeInHealthInsurance -->

**What it is.** Whether the dependant is covered by health insurance.

**Why it matters.** Recorded for reference; no calculation or report reads it at present.

**Rules.** Yes or no.
