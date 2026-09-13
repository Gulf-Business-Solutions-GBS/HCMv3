---
helpId: pa.employee-social-security-claim.edit
lang: en
title: Employee Social Security Claims — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeSocialSecurityClaim
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/social-security-claims/create-or-edit-employee-social-security-claim-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeSocialSecurityClaims.Create]
---

## Overview

A social security claim or reimbursement recorded for the employee. At least one of the two contribution amounts must be entered.

It is recorded for reference; payroll does not read it at present.

## ClaimDate {#EmployeeSocialSecurityClaim.ClaimDate}
<!-- alias: loc:ClaimDate, dom:EmployeeSocialSecurityClaim_ClaimDate -->

**What it is.** The date of the claim.

**Example.** 20 Aug 2026

**Rules.** Required. Date value.

## EmployeeContributionAmount {#EmployeeSocialSecurityClaim.EmployeeContributionAmount}
<!-- alias: loc:EmployeeContributionAmount, dom:EmployeeSocialSecurityClaim_EmployeeContributionAmount -->

**What it is.** The employee contribution amount claimed.

**Example.** 52.500

**Rules.** Optional. Number.

## EmployerContributionAmount {#EmployeeSocialSecurityClaim.EmployerContributionAmount}
<!-- alias: loc:EmployerContributionAmount, dom:EmployeeSocialSecurityClaim_EmployerContributionAmount -->

**What it is.** The employer contribution amount claimed.

**Example.** 112.500

**Rules.** Optional. Number.
