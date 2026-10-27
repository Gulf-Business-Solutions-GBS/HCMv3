---
helpId: hs.action-register.edit
lang: en
title: Action Registers — Create / Edit
entity: HCMv3.HealthAndSafetyManagement.ActionRegister
surfaces:
  - angular/src/app/admin/health-and-safety-management/action-registers/create-or-edit-action-register-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.ActionRegisters.Create]
---

## Overview

A corrective or preventive action. As in the old system, both an incident and a risk must be chosen.

## ActionDate {#ActionRegister.ActionDate}
<!-- alias: loc:ActionDate, dom:ActionRegister_ActionDate -->

**What it is.** When the action was raised.

**Rules.** Required. Date value.

## Status {#ActionRegister.StatusId}
<!-- alias: loc:Status, dom:ActionRegister_StatusId -->

**What it is.** Where the action stands.

**Example.** In progress

**Rules.** Optional. Chosen from Action Status master data.

## Incident {#ActionRegister.IncidentId}
<!-- alias: loc:Incident, dom:ActionRegister_IncidentId -->

**What it is.** The incident this belongs to.

**Rules.** Required. Chosen from Incident Register master data.

## Risk {#ActionRegister.RiskId}
<!-- alias: loc:Risk, dom:ActionRegister_RiskId -->

**What it is.** The risk this belongs to.

**Rules.** Required. Chosen from Risk Register master data.

## AssignedTo {#ActionRegister.AssignedToId}
<!-- alias: loc:AssignedTo, dom:ActionRegister_AssignedToId -->

**What it is.** Who must carry it out.

**Rules.** Required. Chosen from the employee list.

## Description {#ActionRegister.Description}
<!-- alias: loc:Description, dom:ActionRegister_Description -->

**What it is.** What is to be done.

**Rules.** Optional.
