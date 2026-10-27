---
helpId: hs.risk-register.edit
lang: en
title: Risk Registers — Create / Edit
entity: HCMv3.HealthAndSafetyManagement.RiskRegister
surfaces:
  - angular/src/app/admin/health-and-safety-management/risk-registers/create-or-edit-risk-register-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.RiskRegisters.Create]
---

## Overview

A workplace hazard, how likely and how serious it is, and what is being done about it.

## ReportingDate {#RiskRegister.ReportingDate}
<!-- alias: loc:ReportingDate, dom:RiskRegister_ReportingDate -->

**What it is.** When the risk was reported.

**Rules.** Required. Date value.

## ReportingTime {#RiskRegister.ReportingTime}
<!-- alias: loc:ReportingTime, dom:RiskRegister_ReportingTime -->

**What it is.** The time of day, if known.

**Example.** 14:30

**Rules.** Optional. Date value.

## RiskType {#RiskRegister.RiskTypeId}
<!-- alias: loc:RiskType, dom:RiskRegister_RiskTypeId -->

**What it is.** The kind of risk.

**Example.** Electrical

**Rules.** Required. Chosen from Risk Type master data.

## ReassessmentDate {#RiskRegister.ReassessmentDate}
<!-- alias: loc:ReassessmentDate, dom:RiskRegister_ReassessmentDate -->

**What it is.** When the risk should be looked at again.

**Why it matters.** A record only: no reminder is sent when it comes round.

**Rules.** Required. Date value.

## Impact {#RiskRegister.ImpactId}
<!-- alias: loc:Impact, dom:RiskRegister_ImpactId -->

**What it is.** How bad it would be.

**Example.** High

**Rules.** Required. Chosen from Risk Impact master data.

## Possibility {#RiskRegister.PossibilityId}
<!-- alias: loc:Possibility, dom:RiskRegister_PossibilityId -->

**What it is.** How likely it is.

**Example.** Unlikely

**Rules.** Required. Chosen from Risk Possibility master data.

## Location {#RiskRegister.LocationId}
<!-- alias: loc:Location, dom:RiskRegister_LocationId -->

**What it is.** The work location.

**Example.** Main warehouse

**Rules.** Required. Chosen from Work Location master data.

## Status {#RiskRegister.StatusId}
<!-- alias: loc:Status, dom:RiskRegister_StatusId -->

**What it is.** Where the risk stands.

**Example.** Open

**Rules.** Optional. Chosen from Risk Status master data.

## ReportedBy {#RiskRegister.ReportedById}
<!-- alias: loc:ReportedBy, dom:RiskRegister_ReportedById -->

**What it is.** Who reported it.

**Rules.** Required. Chosen from the employee list.

## AssignedTo {#RiskRegister.AssignedToId}
<!-- alias: loc:AssignedTo, dom:RiskRegister_AssignedToId -->

**What it is.** Who is dealing with it.

**Rules.** Optional. Chosen from the employee list.

## Action {#RiskRegister.ActionId}
<!-- alias: loc:Action, dom:RiskRegister_ActionId -->

**What it is.** The kind of response.

**Example.** Reduce

**Rules.** Optional. Chosen from Risk Action master data.

## Description {#RiskRegister.Description}
<!-- alias: loc:Description, dom:RiskRegister_Description -->

**What it is.** The hazard.

**Rules.** Optional.

## RiskMitigation {#RiskRegister.RiskMitigation}
<!-- alias: loc:RiskMitigation, dom:RiskRegister_RiskMitigation -->

**What it is.** What is being done to reduce it.

**Rules.** Optional.
