---
helpId: pay.social-security-education-allowance.edit
lang: en
title: Social Security Education Allowances — Create / Edit
entity: HCMv3.PayrollManagement.SocialSecurityEducationAllowance
surfaces:
  - angular/src/app/admin/payroll-management/social-security-education-allowances/create-or-edit-social-security-education-allowance-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurityEducationAllowances.Create]
---

## Overview

An amount added to the pay for social security for an education the employee holds.

Each of the employee's educations in force for the month adds the amount of the row with the same education and education branch.

## SocialSecurityId {#SocialSecurityEducationAllowance.SocialSecurityId}
<!-- alias: loc:SocialSecurityId, dom:SocialSecurityEducationAllowance_SocialSecurityId -->

**What it is.** The social security setup this allowance belongs to.

**Rules.** Required. Chosen from Social Security master data.

## EducationBranchId {#SocialSecurityEducationAllowance.EducationBranchId}
<!-- alias: loc:EducationBranchId, dom:SocialSecurityEducationAllowance_EducationBranchId -->

**What it is.** The branch of study.

**Example.** Engineering

**Rules.** Required. Chosen from Education Branch master data.

## EducationId {#SocialSecurityEducationAllowance.EducationId}
<!-- alias: loc:EducationId, dom:SocialSecurityEducationAllowance_EducationId -->

**What it is.** The education level.

**Example.** Bachelor's Degree

**Rules.** Required. Chosen from Education master data.

## Amount {#SocialSecurityEducationAllowance.Amount}
<!-- alias: loc:Amount, dom:SocialSecurityEducationAllowance_Amount -->

**What it is.** The amount added.

**Example.** `100`

**Rules.** Optional. Number.
