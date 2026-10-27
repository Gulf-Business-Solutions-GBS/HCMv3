---
helpId: pay.social-security.edit
lang: en
title: Social Securities — Create / Edit
entity: HCMv3.PayrollManagement.SocialSecurity
surfaces:
  - angular/src/app/admin/payroll-management/social-securities/create-or-edit-social-security-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurities.Create]
---

## Overview

The social security setup for one nationality, from a given date: the contribution slabs and the allowances that are added to the pay it is worked out on.

Social security applies to an employee only when they have a social security record with *Social Security Contribution* ticked for the month, and there is a social security setup for their nationality in force on the pay date.

The employee's pay for social security is set by the payroll setting *Social Security Rate Rule*: the monthly total of wagetypes marked *Include In Social Security* (*Wage Salary*), or the contract salary on the employee's sponsor record (*Contract Salary*). The marital, education and child allowances are added to it, the slab it falls in gives the percentages, and the employee's and employer's shares are paid on wagetypes `1` and `3`.

## NationalityId {#SocialSecurity.NationalityId}
<!-- alias: loc:NationalityId, dom:SocialSecurity_NationalityId -->

**What it is.** The nationality this setup is for.

**Example.** Kuwaiti

**Why it matters.** Employees of other nationalities are not covered by it.

**Rules.** Required. Chosen from Nationality master data.

## EarningsCap {#SocialSecurity.EarningsCap}
<!-- alias: loc:EarningsCap, dom:SocialSecurity_EarningsCap -->

**What it is.** The most social security that can be deducted in a month.

**Example.** `450`

**Why it matters.** Despite its name it caps the deduction, not the earnings: a deduction worked out above this amount is reduced to it. 0 means no cap.

**Rules.** Optional. Number.

## ChildAllowance {#SocialSecurity.ChildAllowance}
<!-- alias: loc:ChildAllowance, dom:SocialSecurity_ChildAllowance -->

**What it is.** The amount added to the pay for each child.

**Example.** `50`

**Why it matters.** Counted for male employees only, for each dependant marked *Include In Social Security* who is not a special-needs child.

**Rules.** Optional. Number.

## SpecialNeedChildAllowance {#SocialSecurity.SpecialNeedChildAllowance}
<!-- alias: loc:SpecialNeedChildAllowance, dom:SocialSecurity_SpecialNeedChildAllowance -->

**What it is.** The amount added to the pay for each special-needs child.

**Example.** `75`

**Why it matters.** Counted for male employees only, for each dependant marked *Include In Social Security* and *Special Need*.

**Rules.** Optional. Number.
