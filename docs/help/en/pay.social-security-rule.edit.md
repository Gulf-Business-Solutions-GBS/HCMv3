---
helpId: pay.social-security-rule.edit
lang: en
title: Social Security Rules — Create / Edit
entity: HCMv3.PayrollManagement.SocialSecurityRule
surfaces:
  - angular/src/app/admin/payroll-management/social-security-rules/create-or-edit-social-security-rule-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurityRules.Create]
---

## Overview

The social security allowance -- wagetype `5` -- by qualification, gender, marital status and number of dependants.

Social security applies to an employee only when they have a social security record with *Social Security Contribution* ticked for the month, and there is a social security setup for their nationality in force on the pay date. Payroll then takes the rule in force that matches the employee's qualification, gender and marital status exactly, with the highest **Dependant** not above the employee's number of dependants, and pays its amount.

## SocialSecurityId {#SocialSecurityRule.SocialSecurityId}
<!-- alias: loc:SocialSecurityId, dom:SocialSecurityRule_SocialSecurityId -->

**What it is.** The social security setup this rule belongs to.

**Rules.** Required. Chosen from Social Security master data.

## QualificationId {#SocialSecurityRule.QualificationId}
<!-- alias: loc:QualificationId, dom:SocialSecurityRule_QualificationId -->

**What it is.** The qualification the rule is for.

**Example.** University

**Rules.** Required. Chosen from Qualification master data.

## Gender {#SocialSecurityRule.Gender}
<!-- alias: loc:Gender, dom:SocialSecurityRule_Gender -->

**What it is.** The gender the rule is for.

**Example.** Male

**Rules.** Required. One of: Male, Female.

## MaritalStatusId {#SocialSecurityRule.MaritalStatusId}
<!-- alias: loc:MaritalStatusId, dom:SocialSecurityRule_MaritalStatusId -->

**What it is.** The marital status the rule is for.

**Example.** Married

**Why it matters.** Left blank, the rule matches only employees with no marital status recorded.

**Rules.** Optional. Chosen from Marital Status master data.

## Dependant {#SocialSecurityRule.Dependant}
<!-- alias: loc:Dependant, dom:SocialSecurityRule_Dependant -->

**What it is.** The number of dependants the rule starts from.

**Example.** `2`

**Why it matters.** All of the employee's dependants are counted, whatever their type.

**Rules.** Optional. Whole number.

## Amount {#SocialSecurityRule.Amount}
<!-- alias: loc:Amount, dom:SocialSecurityRule_Amount -->

**What it is.** The allowance paid.

**Example.** `120`

**Rules.** Optional. Number.

## Disability {#SocialSecurityRule.Disability}
<!-- alias: loc:Disability, dom:SocialSecurityRule_Disability -->

**What it is.** Whether the rule is for employees with a disability.

**Why it matters.** Not read at present.

**Rules.** Yes or no.
