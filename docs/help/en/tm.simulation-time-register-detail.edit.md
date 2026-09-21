---
helpId: tm.simulation-time-register-detail.edit
lang: en
title: Simulation Time Register Details — Create / Edit
entity: HCMv3.TimeManagement.SimulationTimeRegisterDetail
surfaces:
  - angular/src/app/admin/time-management/simulation-time-register-details/create-or-edit-simulation-time-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.SimulationTimeRegisterDetails.Create]
---

## Overview

One time type's value for a simulated day.

Not written at present: no process fills these registers and nothing reads them. Payroll and the reports work from the daily time register.

## SimulationTimeRegister {#SimulationTimeRegisterDetail.SimulationTimeRegisterId}
<!-- alias: loc:SimulationTimeRegister, dom:SimulationTimeRegisterDetail_SimulationTimeRegisterId -->

**What it is.** The simulated day this line belongs to.

**Rules.** Required. Chosen from Simulation Time Register master data.

## TimeType {#SimulationTimeRegisterDetail.TimeTypeId}
<!-- alias: loc:TimeType, dom:SimulationTimeRegisterDetail_TimeTypeId -->

**What it is.** The time type the value is for.

**Rules.** Required. Chosen from Time Type master data.

## TimeValue {#SimulationTimeRegisterDetail.TimeValue}
<!-- alias: loc:TimeValue, dom:SimulationTimeRegisterDetail_TimeValue -->

**What it is.** The value.

**Example.** `2.5`

**Rules.** Optional.
