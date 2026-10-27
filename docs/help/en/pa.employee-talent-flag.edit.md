---
helpId: pa.employee-talent-flag.edit
lang: en
title: Employee Talent Flags — Create / Edit
entity: HCMv3.SuccessionManagement.EmployeeTalentFlag
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/talent-flags/create-or-edit-employee-talent-flag-modal.component.html
audience: [admin]
permissions: [Pages.SuccessionManagement.EmployeeTalentFlags.Create]
---

## Overview

Flags the employee as a retention concern for a period: how likely they are to leave, how much it would matter, and why.

It is recorded for reference; no calculation or report reads it at present.

## RiskOfLossId {#EmployeeTalentFlag.RiskOfLossId}
<!-- alias: loc:RiskOfLossId, dom:EmployeeTalentFlag_RiskOfLossId -->

**What it is.** How likely the employee is to leave.

**Example.** High

**Rules.** Required. Chosen from Risk Of Loss master data.

:::admin
Maintained under Succession Management.
:::

## ImpactOfLossId {#EmployeeTalentFlag.ImpactOfLossId}
<!-- alias: loc:ImpactOfLossId, dom:EmployeeTalentFlag_ImpactOfLossId -->

**What it is.** How much losing the employee would matter.

**Example.** Critical

**Rules.** Required. Chosen from Impact Of Loss master data.

:::admin
Maintained under Succession Management.
:::

## ReasonForLeavingId {#EmployeeTalentFlag.ReasonForLeavingId}
<!-- alias: loc:ReasonForLeavingId, dom:EmployeeTalentFlag_ReasonForLeavingId -->

**What it is.** The likely reason for leaving.

**Example.** Compensation

**Rules.** Required. Chosen from Reason For Leaving master data.

:::admin
Maintained under Succession Management.
:::

## TalentPoolId {#EmployeeTalentFlag.TalentPoolId}
<!-- alias: loc:TalentPoolId, dom:EmployeeTalentFlag_TalentPoolId -->

**What it is.** The talent pool the employee belongs to.

**Example.** Future Leaders

**Rules.** Required. Chosen from Talent Pool master data.

:::admin
Maintained under Succession Management.
:::
