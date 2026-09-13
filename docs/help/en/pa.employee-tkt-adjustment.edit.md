---
helpId: pa.employee-tkt-adjustment.edit
lang: en
title: Employee Ticket Adjustments — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeTktAdjustment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/tkt-adjustments/create-or-edit-employee-tkt-adjustment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTktAdjustments.Create]
---

## Overview

A manual adjustment to the employee's air ticket entitlement.

It is recorded for reference: saving it does not update the employee's ticket quota, and neither payroll nor the time engine reads it at present.

## TicketTypeId {#EmployeeTktAdjustment.TicketTypeId}
<!-- alias: loc:TicketTypeId, dom:EmployeeTktAdjustment_TicketTypeId -->

**What it is.** The ticket type being adjusted.

**Example.** Employee

**Rules.** Required. Chosen from Ticket Type master data.

## EffectiveDate {#EmployeeTktAdjustment.EffectiveDate}
<!-- alias: loc:EffectiveDate, dom:EmployeeTktAdjustment_EffectiveDate -->

**What it is.** The date the adjustment takes effect.

**Example.** 30 Jun 2026

**Rules.** Required. Date value.

## AdjustmentType {#EmployeeTktAdjustment.AdjustmentType}
<!-- alias: loc:AdjustmentType, dom:EmployeeTktAdjustment_AdjustmentType -->

**What it is.** What kind of adjustment this is.

**Example.** Encashment

**Rules.** Required. One of: Encashment, Adjust, Fractional, No Impact On Quota.

## TicketPaymentType {#EmployeeTktAdjustment.TicketPaymentType}
<!-- alias: loc:TicketPaymentType, dom:EmployeeTktAdjustment_TicketPaymentType -->

**What it is.** When an encashment would be paid.

**Example.** Pay With Payroll

**Rules.** Required. One of: Pay With Payroll, Pay In Advance.

## QuotaAccrued {#EmployeeTktAdjustment.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeTktAdjustment_QuotaAccrued -->

**What it is.** Tickets accrued by the adjustment.

**Example.** 0

**Rules.** Optional. Number.

## QuotaAdjusted {#EmployeeTktAdjustment.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeTktAdjustment_QuotaAdjusted -->

**What it is.** Tickets adjusted.

**Example.** 1

**Rules.** Optional. Number.
