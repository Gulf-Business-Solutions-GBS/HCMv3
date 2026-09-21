---
helpId: pay.indemnity-pay-rule-detail.edit
lang: en
title: Indemnity Pay Rule Details — Create / Edit
entity: HCMv3.PayrollManagement.IndemnityPayRuleDetail
surfaces:
  - angular/src/app/admin/payroll-management/indemnity-pay-rule-details/create-or-edit-indemnity-pay-rule-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.IndemnityPayRuleDetails.Create]
---

## Overview

One step of an indemnity pay rule, by years of service -- for example, a resignation after three to five years paid at half.

Not used at present: every payroll and report calculation passes no indemnity pay rule, so the indemnity cap always comes from the employee's **Employee Group**. These rules are saved and kept for reference.

## IndemnityPayRuleId {#IndemnityPayRuleDetail.IndemnityPayRuleId}
<!-- alias: loc:IndemnityPayRuleId, dom:IndemnityPayRuleDetail_IndemnityPayRuleId -->

**What it is.** The rule this step belongs to.

**Rules.** Required. Chosen from Indemnity Pay Rule master data.

## MinYearOfService {#IndemnityPayRuleDetail.MinYearOfService}
<!-- alias: loc:MinYearOfService, dom:IndemnityPayRuleDetail_MinYearOfService -->

**What it is.** The fewest years of service this step covers.

**Example.** `3`

**Rules.** Optional. Number.

## MaxYearOfService {#IndemnityPayRuleDetail.MaxYearOfService}
<!-- alias: loc:MaxYearOfService, dom:IndemnityPayRuleDetail_MaxYearOfService -->

**What it is.** The most years of service this step covers. It cannot be less than the minimum.

**Example.** `5`

**Rules.** Optional. Number.

## Number {#IndemnityPayRuleDetail.Number}
<!-- alias: loc:Number, dom:IndemnityPayRuleDetail_Number -->

**What it is.** A quantity for the step, in the unit below.

**Example.** `15`

**Rules.** Optional. Number.

## Unit {#IndemnityPayRuleDetail.Unit}
<!-- alias: loc:Unit, dom:IndemnityPayRuleDetail_Unit -->

**What it is.** Whether the number is in days or months.

**Example.** Day

**Rules.** Required. One of: Day, Month.

## PayPercentage {#IndemnityPayRuleDetail.PayPercentage}
<!-- alias: loc:PayPercentage, dom:IndemnityPayRuleDetail_PayPercentage -->

**What it is.** The percentage of the indemnity paid at this step.

**Example.** `50`

**Rules.** Optional. Number.
