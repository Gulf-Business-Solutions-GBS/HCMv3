---
helpId: gp.gate-pass.edit
lang: en
title: Gate Passes — Create / Edit
entity: HCMv3.GatePassManagement.GatePass
surfaces:
  - angular/src/app/admin/gate-pass-management/gate-passes/create-or-edit-gate-pass-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.GatePasses.Create]
---

## Overview

Permission for people, vehicles or materials to enter or leave a site. The people, vehicles and materials are listed as details. Valid Upto cannot be before the issue date.

## GatePassNumber {#GatePass.GatePassNumber}
<!-- alias: loc:GatePassNumber, dom:GatePass_GatePassNumber -->

**What it is.** The pass number. It is typed in, not given by the system.

**Example.** 1024

**Rules.** Required. Whole number.

## GatePassType {#GatePass.GatePassTypeId}
<!-- alias: loc:GatePassType, dom:GatePass_GatePassTypeId -->

**What it is.** The kind of pass.

**Example.** Visitor

**Rules.** Required. Chosen from Gate Pass Type master data.

## GatePassMovement {#GatePass.GatePassMovementId}
<!-- alias: loc:GatePassMovement, dom:GatePass_GatePassMovementId -->

**What it is.** The direction of movement through the gate.

**Example.** Out

**Rules.** Required. Chosen from a list.

## Location {#GatePass.LocationId}
<!-- alias: loc:Location, dom:GatePass_LocationId -->

**What it is.** The site.

**Rules.** Optional. Chosen from Work Location master data.

## RequestedByEmployee {#GatePass.RequestedByEmployeeId}
<!-- alias: loc:RequestedByEmployee, dom:GatePass_RequestedByEmployeeId -->

**What it is.** Who asked for the pass.

**Rules.** Optional. Chosen from a list.

## GatePassStatus {#GatePass.GatePassStatusId}
<!-- alias: loc:GatePassStatus, dom:GatePass_GatePassStatusId -->

**What it is.** Where the pass stands.

**Example.** Issued

**Rules.** Optional. Chosen from a list.

## IssueDate {#GatePass.IssueDate}
<!-- alias: loc:IssueDate, dom:GatePass_IssueDate -->

**What it is.** When the pass was issued.

**Rules.** Required. Date value.

## ValidUpto {#GatePass.ValidUpto}
<!-- alias: loc:ValidUpto, dom:GatePass_ValidUpto -->

**What it is.** The last day the pass can be used.

**Why it matters.** A record only: nothing warns when it passes.

**Rules.** Required. Date value.

## StartTime {#GatePass.StartTime}
<!-- alias: loc:StartTime, dom:GatePass_StartTime -->

**What it is.** The time the pass starts.

**Example.** 08:00

**Rules.** Required. Date value.

## EndTime {#GatePass.EndTime}
<!-- alias: loc:EndTime, dom:GatePass_EndTime -->

**What it is.** The time the pass ends.

**Example.** 17:00

**Rules.** Required. Date value.
