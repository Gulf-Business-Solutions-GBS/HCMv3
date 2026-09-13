---
helpId: pa.employee-social-security.edit
lang: en
title: Employee Social Securities — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeSocialSecurity
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/social-securities/create-or-edit-employee-social-security-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeSocialSecurities.Create]
---

## Overview

The employee's social security (PIFSS) registration for a period.

Payroll calculates social security allowances and deductions for a month only when a registration with Contributes ticked covers that month's end.

## SocialSecurityNum {#EmployeeSocialSecurity.SocialSecurityNum}
<!-- alias: loc:SocialSecurityNum, dom:EmployeeSocialSecurity_SocialSecurityNum -->

**What it is.** The employee's social security registration number.

**Example.** 1234567

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Required. Whole number.

## SocialSecurityContrib {#EmployeeSocialSecurity.SocialSecurityContrib}
<!-- alias: loc:SocialSecurityContrib, dom:EmployeeSocialSecurity_SocialSecurityContrib -->

**What it is.** Whether the employee contributes to social security in this period.

**Why it matters.** Without it, payroll calculates no social security for the months this record covers.

**Rules.** Yes or no.
