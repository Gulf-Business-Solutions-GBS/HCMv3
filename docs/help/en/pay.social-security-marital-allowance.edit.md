---
helpId: pay.social-security-marital-allowance.edit
lang: en
title: Social Security Marital Allowances — Create / Edit
entity: HCMv3.PayrollManagement.SocialSecurityMaritalAllowance
surfaces:
  - angular/src/app/admin/payroll-management/social-security-marital-allowances/create-or-edit-social-security-marital-allowance-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurityMaritalAllowances.Create]
---

## Overview

An amount added to the pay for social security by marital status, for employees whose education falls in a given branch.

Only rows for a branch of one of the employee's educations count. A male employee gets the row matching his marital status. A female employee who is not single gets every non-single row for her branches added together -- legacy behaviour, kept on purpose.

## SocialSecurityId {#SocialSecurityMaritalAllowance.SocialSecurityId}
<!-- alias: loc:SocialSecurityId, dom:SocialSecurityMaritalAllowance_SocialSecurityId -->

**What it is.** The social security setup this allowance belongs to.

**Rules.** Required. Chosen from Social Security master data.

## EducationBranchId {#SocialSecurityMaritalAllowance.EducationBranchId}
<!-- alias: loc:EducationBranchId, dom:SocialSecurityMaritalAllowance_EducationBranchId -->

**What it is.** The branch of study the allowance applies to.

**Example.** Engineering

**Rules.** Required. Chosen from Education Branch master data.

## MaritalStatusId {#SocialSecurityMaritalAllowance.MaritalStatusId}
<!-- alias: loc:MaritalStatusId, dom:SocialSecurityMaritalAllowance_MaritalStatusId -->

**What it is.** The marital status the amount is for.

**Example.** Married

**Rules.** Required. Chosen from Marital Status master data.

## Amount {#SocialSecurityMaritalAllowance.Amount}
<!-- alias: loc:Amount, dom:SocialSecurityMaritalAllowance_Amount -->

**What it is.** The amount added.

**Example.** `50`

**Rules.** Optional. Number.
