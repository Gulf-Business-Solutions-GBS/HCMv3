---
helpId: pay.cost-register.edit
lang: en
title: Cost Registers — Create / Edit
entity: HCMv3.PayrollManagement.CostRegister
surfaces:
  - angular/src/app/admin/payroll-management/cost-registers/create-or-edit-cost-register-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.CostRegisters.Create]
---

## Overview

An employee's payroll cost for one month and run, split across ledger accounts and cost elements. The cost engine writes these from the payslip and the employee's wage accounts; the journal voucher is built from them.

## EmployeeId {#CostRegister.EmployeeId}
<!-- alias: loc:EmployeeId, dom:CostRegister_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## Period {#CostRegister.Period}
<!-- alias: loc:Period, dom:CostRegister_Period -->

**What it is.** The month the cost is for.

**Example.** `2026-04-30`

**Rules.** Required. Date value.

## PayrollRunType {#CostRegister.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:CostRegister_PayrollRunType -->

**What it is.** Whether the cost came from a regular or an off-cycle run.

**Example.** Regular

**Rules.** Required. One of: Regular, Off Cycle.
