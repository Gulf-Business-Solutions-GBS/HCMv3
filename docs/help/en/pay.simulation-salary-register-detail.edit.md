---
helpId: pay.simulation-salary-register-detail.edit
lang: en
title: Simulation Salary Register Details — Create / Edit
entity: HCMv3.PayrollManagement.SimulationSalaryRegisterDetail
surfaces:
  - angular/src/app/admin/payroll-management/simulation-salary-register-details/create-or-edit-simulation-salary-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SimulationSalaryRegisterDetails.Create]
---

## Overview

One line of a simulation payslip: a wagetype with its rate, number and amount.

## SimulationSalaryRegisterId {#SimulationSalaryRegisterDetail.SimulationSalaryRegisterId}
<!-- alias: loc:SimulationSalaryRegisterId, dom:SimulationSalaryRegisterDetail_SimulationSalaryRegisterId -->

**What it is.** The simulation payslip this line belongs to.

**Rules.** Required. Chosen from Simulation Salary Register master data.

## WageTypeId {#SimulationSalaryRegisterDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:SimulationSalaryRegisterDetail_WageTypeId -->

**What it is.** The wagetype of this line.

**Example.** Basic Salary

**Rules.** Required. Chosen from Wagetype master data.

## Rate {#SimulationSalaryRegisterDetail.Rate}
<!-- alias: loc:Rate, dom:SimulationSalaryRegisterDetail_Rate -->

**What it is.** The monthly rate the line was worked out from.

**Example.** `800`

**Rules.** Required. Number.

## Number {#SimulationSalaryRegisterDetail.Number}
<!-- alias: loc:Number, dom:SimulationSalaryRegisterDetail_Number -->

**What it is.** The quantity -- days, hours or units -- the line was worked out on.

**Example.** `30`

**Rules.** Required. Number.

## Amount {#SimulationSalaryRegisterDetail.Amount}
<!-- alias: loc:Amount, dom:SimulationSalaryRegisterDetail_Amount -->

**What it is.** The amount of the line.

**Example.** `800.000`

**Rules.** Required. Number.
