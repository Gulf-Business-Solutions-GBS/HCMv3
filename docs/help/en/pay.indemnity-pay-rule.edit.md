---
helpId: pay.indemnity-pay-rule.edit
lang: en
title: Indemnity Pay Rules — Create / Edit
entity: HCMv3.PayrollManagement.IndemnityPayRule
surfaces:
  - angular/src/app/admin/payroll-management/indemnity-pay-rules/create-or-edit-indemnity-pay-rule-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.IndemnityPayRules.Create]
---

## Overview

A cap on end-of-service indemnity by reason for leaving and country.

Not used at present: every payroll and report calculation passes no indemnity pay rule, so the indemnity cap always comes from the employee's **Salary Profile**. These rules are saved and kept for reference.

## SeparationType {#IndemnityPayRule.SeparationType}
<!-- alias: loc:SeparationType, dom:IndemnityPayRule_SeparationType -->

**What it is.** The reason for leaving the rule is for.

**Example.** Resignation

**Rules.** Required. One of: Resignation, Termination.

## CountryId {#IndemnityPayRule.CountryId}
<!-- alias: loc:CountryId, dom:IndemnityPayRule_CountryId -->

**What it is.** The country the rule is for.

**Example.** Kuwait

**Rules.** Optional. Chosen from Country master data.

## MaxNumber {#IndemnityPayRule.MaxNumber}
<!-- alias: loc:MaxNumber, dom:IndemnityPayRule_MaxNumber -->

**What it is.** The most pay the indemnity could reach, in the unit below.

**Example.** `18`

**Rules.** Optional. Number.

## MaxUnit {#IndemnityPayRule.MaxUnit}
<!-- alias: loc:MaxUnit, dom:IndemnityPayRule_MaxUnit -->

**What it is.** Whether the maximum is in months or days.

**Example.** Month

**Rules.** Required. One of: Day, Month.

## MaxPayPercentage {#IndemnityPayRule.MaxPayPercentage}
<!-- alias: loc:MaxPayPercentage, dom:IndemnityPayRule_MaxPayPercentage -->

**What it is.** The percentage of that maximum that could be paid.

**Example.** `100`

**Rules.** Optional. Number.
