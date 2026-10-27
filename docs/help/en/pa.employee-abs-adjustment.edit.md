---
helpId: pa.employee-abs-adjustment.edit
lang: en
title: Employee Absence Adjustments — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAbsAdjustment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/abs-adjustments/create-or-edit-employee-abs-adjustment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAbsAdjustments.Create]
---

## Overview

A manual change to the employee's leave -- days encashed, adjusted or rounded.

Payroll pays annual-leave encashment and fractional adjustments falling in the pay period as leave encashment. The as-on-date leave quota used by payroll counts every adjustment except those marked No Impact On Quota.

Saving an adjustment does not update the employee's leave quota record, so the self-service balance does not change.

## EffectiveDate {#EmployeeAbsAdjustment.EffectiveDate}
<!-- alias: loc:EffectiveDate, dom:EmployeeAbsAdjustment_EffectiveDate -->

**What it is.** The date the adjustment takes effect.

**Example.** 30 Jun 2026

**Why it matters.** Payroll pays encashment whose effective date falls in the pay period, and the as-on-date quota counts adjustments up to the date asked for.

**Rules.** Required. Date value.

## AbsenceTypeId {#EmployeeAbsAdjustment.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:EmployeeAbsAdjustment_AbsenceTypeId -->

**What it is.** The leave type being adjusted.

**Example.** Annual Leave

**Rules.** Required. Chosen from Leave Type master data.

:::admin
Maintained under Payroll Management → Leave Types.
:::

## AdjustmentType {#EmployeeAbsAdjustment.AdjustmentType}
<!-- alias: loc:AdjustmentType, dom:EmployeeAbsAdjustment_AdjustmentType -->

**What it is.** What kind of adjustment this is.

**Example.** Encashment

**Why it matters.** Encashment and Fractional adjustments of annual leave are paid by payroll. No Impact On Quota is recorded without reducing the as-on-date quota.

**Rules.** Required. One of: Encashment, Adjust, Fractional, No Impact On Quota.

## AdjustmentPaymentType {#EmployeeAbsAdjustment.AdjustmentPaymentType}
<!-- alias: loc:AdjustmentPaymentType, dom:EmployeeAbsAdjustment_AdjustmentPaymentType -->

**What it is.** When an encashment is paid.

**Example.** Pay With Payroll

**Why it matters.** Pay In Advance adjustments are picked up by off-cycle payroll runs.

**Rules.** Required. One of: Pay With Payroll, Pay In Advance.

## QuotaAdjusted {#EmployeeAbsAdjustment.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeAbsAdjustment_QuotaAdjusted -->

**What it is.** The number of days adjusted.

**Example.** 5

**Why it matters.** For encashment, the days payroll pays out.

**Rules.** Optional. Number.
