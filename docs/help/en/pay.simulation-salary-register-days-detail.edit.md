---
helpId: pay.simulation-salary-register-days-detail.edit
lang: en
title: Simulation Salary Register Days Details — Create / Edit
entity: HCMv3.PayrollManagement.SimulationSalaryRegisterDaysDetail
surfaces:
  - angular/src/app/admin/payroll-management/simulation-salary-register-days-details/create-or-edit-simulation-salary-register-days-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SimulationSalaryRegisterDaysDetails.Create]
---

## Overview

The breakdown of a simulation payslip line by date.

## SimulationSalaryRegisterDetailId {#SimulationSalaryRegisterDaysDetail.SimulationSalaryRegisterDetailId}
<!-- alias: loc:SimulationSalaryRegisterDetailId, dom:SimulationSalaryRegisterDaysDetail_SimulationSalaryRegisterDetailId -->

**What it is.** The simulation payslip line this breakdown belongs to.

**Rules.** Required. Chosen from Simulation Salary Register Detail master data.

## Period {#SimulationSalaryRegisterDaysDetail.Period}
<!-- alias: loc:Period, dom:SimulationSalaryRegisterDaysDetail_Period -->

**What it is.** The date this part of the line is for.

**Example.** `2026-03-14`

**Rules.** Required. Date value.

## Rate {#SimulationSalaryRegisterDaysDetail.Rate}
<!-- alias: loc:Rate, dom:SimulationSalaryRegisterDaysDetail_Rate -->

**What it is.** The rate for that date.

**Example.** `800`

**Rules.** Required. Number.

## Number {#SimulationSalaryRegisterDaysDetail.Number}
<!-- alias: loc:Number, dom:SimulationSalaryRegisterDaysDetail_Number -->

**What it is.** The days, hours or units for that date.

**Example.** `2`

**Rules.** Required. Number.

## Amount {#SimulationSalaryRegisterDaysDetail.Amount}
<!-- alias: loc:Amount, dom:SimulationSalaryRegisterDaysDetail_Amount -->

**What it is.** The amount for that date.

**Example.** `12.500`

**Rules.** Required. Number.
