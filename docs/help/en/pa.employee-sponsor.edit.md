---
helpId: pa.employee-sponsor.edit
lang: en
title: Employee Sponsors — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeSponsor
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/sponsors/create-or-edit-employee-sponsor-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeSponsors.Create]
---

## Overview

The sponsor (كفيل) the employee's work visa is under, for a period, with their government job title and contract salary.

Creating an assignment counts it against the sponsor's quota for that government job title, as a citizen or non-citizen according to the employee's nationality; deleting it releases the place. Editing an existing assignment does not move the count. An assignment is refused if the sponsor has no quota for the government job title.

## SponsorId {#EmployeeSponsor.SponsorId}
<!-- alias: loc:SponsorId, dom:EmployeeSponsor_SponsorId -->

**What it is.** The sponsor.

**Example.** Al-Mutairi General Trading Co.

**Why it matters.** Bank files and report distribution can be filtered by sponsor.

**Rules.** Required. Chosen from Sponsor master data.

:::admin
Maintained under Government Relations Management → Sponsors.
:::

## GovtJobTitleId {#EmployeeSponsor.GovtJobTitleId}
<!-- alias: loc:GovtJobTitleId, dom:EmployeeSponsor_GovtJobTitleId -->

**What it is.** The job title registered with the government.

**Example.** Accountant

**Why it matters.** The sponsor must have a quota for this title; the assignment takes one of its places.

**Rules.** Required. Chosen from Govt Job Title master data.

## EmpSponsorRelation {#EmployeeSponsor.EmpSponsorRelation}
<!-- alias: loc:EmpSponsorRelation, dom:EmployeeSponsor_EmpSponsorRelation -->

**What it is.** The employee's relation to the sponsor, as free text.

**Example.** Employee

**Why it matters.** Shown on the sponsor report.

**Rules.** Optional. Maximum 256 characters.

## ContractSalary {#EmployeeSponsor.ContractSalary}
<!-- alias: loc:ContractSalary, dom:EmployeeSponsor_ContractSalary -->

**What it is.** The salary on the government contract.

**Example.** 600.000

**Why it matters.** When social security is set to use the contract salary, payroll takes it from the sponsor assignment covering the month.

**Rules.** Optional. Number.

## SalaryCurrencyId {#EmployeeSponsor.SalaryCurrencyId}
<!-- alias: loc:SalaryCurrencyId, dom:EmployeeSponsor_SalaryCurrencyId -->

**What it is.** The currency of the contract salary.

**Example.** KWD

**Why it matters.** Recorded for reference; no calculation or report reads it at present.

**Rules.** Required. Chosen from Currency master data.

## ResidencyType {#EmployeeSponsor.ResidencyType}
<!-- alias: loc:ResidencyType, dom:EmployeeSponsor_ResidencyType -->

**What it is.** Whether the residency is on the company or not.

**Example.** Company

**Why it matters.** Shown on employee reports.

**Rules.** Required. One of: Company, Non Company.

## WorkVisaTypeId {#EmployeeSponsor.WorkVisaTypeId}
<!-- alias: loc:WorkVisaTypeId, dom:EmployeeSponsor_WorkVisaTypeId -->

**What it is.** The kind of work visa.

**Example.** Article 18

**Why it matters.** Recorded for reference; no calculation or report reads it at present.

**Rules.** Required. Chosen from Work Visa Type master data.
