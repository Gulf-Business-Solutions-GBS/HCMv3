---
helpId: pay.social-security-detail.edit
lang: en
title: Social Security Details — Create / Edit
entity: HCMv3.PayrollManagement.SocialSecurityDetail
surfaces:
  - angular/src/app/admin/payroll-management/social-security-details/create-or-edit-social-security-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurityDetails.Create]
---

## Overview

One contribution slab: the range of pay it covers and the percentages paid on it.

The employee's pay for social security (with allowances) picks the slab whose range holds it. Pay above every slab uses the highest slab, with the pay reduced to that slab's maximum.

## SocialSecurityId {#SocialSecurityDetail.SocialSecurityId}
<!-- alias: loc:SocialSecurityId, dom:SocialSecurityDetail_SocialSecurityId -->

**What it is.** The social security setup this slab belongs to.

**Rules.** Required. Chosen from Social Security master data.

## MinEarnings {#SocialSecurityDetail.MinEarnings}
<!-- alias: loc:MinEarnings, dom:SocialSecurityDetail_MinEarnings -->

**What it is.** The lowest pay in this slab.

**Example.** `1501`

**Rules.** Optional. Number.

## MaxEarnings {#SocialSecurityDetail.MaxEarnings}
<!-- alias: loc:MaxEarnings, dom:SocialSecurityDetail_MaxEarnings -->

**What it is.** The highest pay in this slab. It cannot be less than the minimum.

**Example.** `2750`

**Rules.** Optional. Number.

## EmployeeContribution {#SocialSecurityDetail.EmployeeContribution}
<!-- alias: loc:EmployeeContribution, dom:SocialSecurityDetail_EmployeeContribution -->

**What it is.** The employee's share, as a percentage of their pay.

**Example.** `2.5`

**Rules.** Optional. Number.

## EmployerContribution {#SocialSecurityDetail.EmployerContribution}
<!-- alias: loc:EmployerContribution, dom:SocialSecurityDetail_EmployerContribution -->

**What it is.** The employer's share, as a percentage of the employee's pay.

**Example.** `11.5`

**Rules.** Optional. Number.

:::admin
The employer's share is worked out only when the slab's employee contribution is not 0. On a slab where only the employer pays, give the employee contribution a value or the employer share is 0 -- legacy behaviour, kept on purpose.
:::

## MinEarningsEmployeeContribution {#SocialSecurityDetail.MinEarningsEmployeeContribution}
<!-- alias: loc:MinEarningsEmployeeContribution, dom:SocialSecurityDetail_MinEarningsEmployeeContribution -->

**What it is.** An extra employee percentage charged on the pay below this slab.

**Example.** `2.5`

**Why it matters.** Added to the employee's share as this percentage of the slab's minimum less 1.

**Rules.** Optional. Number.
