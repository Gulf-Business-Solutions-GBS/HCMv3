---
helpId: pay.payroll-engine.run
lang: en
title: Payroll Engine — Run
entity: HCMv3.PayrollManagement.Dto.RunPayrollEngineInput
surfaces:
  - angular/src/app/admin/payroll-management/payroll-engine/payroll-engine.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollEngine]
---

## Overview

_TODO_

## PayrollAreaId {#RunPayrollEngineInput.PayrollAreaName}
<!-- alias: loc:PayrollAreaId, dom:PayrollEngine_PayrollArea -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Required. Maximum 256 characters.

## PayrollRunType {#RunPayrollEngineInput.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:PayrollEngine_RunType -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Required. One of: Regular, Off Cycle.

## EmployeeCode {#RunPayrollEngineInput.EmployeeCodes}
<!-- alias: loc:EmployeeCode, dom:PayrollEngine_EmployeeCodes -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Maximum 4000 characters.

## PayInDate {#RunPayrollEngineInput.PayInDate}
<!-- alias: loc:PayInDate, dom:PayrollEngine_PayInDate -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Required. Date value.

## OffCycleReason {#RunPayrollEngineInput.OffCycleReason}
<!-- alias: loc:OffCycleReason, dom:PayrollEngine_OffCycleReason -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Required. Maximum 256 characters.

## OffCyclePaymentDate {#RunPayrollEngineInput.OffCyclePaymentDate}
<!-- alias: loc:OffCyclePaymentDate, dom:PayrollEngine_OffCyclePaymentDate -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Date value.
