---
helpId: hs.incident-register.edit
lang: en
title: Incident Registers — Create / Edit
entity: HCMv3.HealthAndSafetyManagement.IncidentRegister
surfaces:
  - angular/src/app/admin/health-and-safety-management/incident-registers/create-or-edit-incident-register-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.IncidentRegisters.Create]
---

## Overview

An accident, injury, near miss or other safety incident. Investigations, claims and actions are recorded against it.

## IncidentDate {#IncidentRegister.IncidentDate}
<!-- alias: loc:IncidentDate, dom:IncidentRegister_IncidentDate -->

**What it is.** When it happened.

**Rules.** Required. Date value.

## IncidentTime {#IncidentRegister.IncidentTime}
<!-- alias: loc:IncidentTime, dom:IncidentRegister_IncidentTime -->

**What it is.** The time of day, if known.

**Example.** 14:30

**Rules.** Optional. Date value.

## IncidentType {#IncidentRegister.IncidentTypeId}
<!-- alias: loc:IncidentType, dom:IncidentRegister_IncidentTypeId -->

**What it is.** The kind of incident.

**Example.** Slip or fall

**Rules.** Required. Chosen from Incident Type master data.

## Status {#IncidentRegister.StatusId}
<!-- alias: loc:Status, dom:IncidentRegister_StatusId -->

**What it is.** Where the incident stands.

**Example.** Under investigation

**Rules.** Optional. Chosen from Incident Status master data.

## Nature {#IncidentRegister.NatureId}
<!-- alias: loc:Nature, dom:IncidentRegister_NatureId -->

**What it is.** The nature of the incident.

**Example.** Injury

**Rules.** Required. Chosen from Incident Nature master data.

## Severity {#IncidentRegister.SeverityId}
<!-- alias: loc:Severity, dom:IncidentRegister_SeverityId -->

**What it is.** How serious it was.

**Example.** Minor

**Rules.** Required. Chosen from Incident Severity master data.

## Location {#IncidentRegister.LocationId}
<!-- alias: loc:Location, dom:IncidentRegister_LocationId -->

**What it is.** The work location, if any.

**Example.** Main warehouse

**Rules.** Optional. Chosen from Work Location master data.

## ReportedBy {#IncidentRegister.ReportedById}
<!-- alias: loc:ReportedBy, dom:IncidentRegister_ReportedById -->

**What it is.** Who reported it.

**Rules.** Optional. Chosen from the employee list.

## PersonInvolved {#IncidentRegister.PersonInvolvedId}
<!-- alias: loc:PersonInvolved, dom:IncidentRegister_PersonInvolvedId -->

**What it is.** The employee involved.

**Rules.** Optional. Chosen from the employee list.

## AssignedTo {#IncidentRegister.AssignedToId}
<!-- alias: loc:AssignedTo, dom:IncidentRegister_AssignedToId -->

**What it is.** Who is dealing with it.

**Rules.** Optional. Chosen from the employee list.

## OtherPeopleInvolved {#IncidentRegister.OtherPeopleInvolved}
<!-- alias: loc:OtherPeopleInvolved, dom:IncidentRegister_OtherPeopleInvolved -->

**What it is.** Anyone else involved, such as visitors or contractors.

**Rules.** Optional.

## DisabilityPercentage {#IncidentRegister.DisabilityPercentage}
<!-- alias: loc:DisabilityPercentage, dom:IncidentRegister_DisabilityPercentage -->

**What it is.** The disability resulting from an injury, as a percentage.

**Example.** 10%

**Why it matters.** Kept as text for the record; it is not used in any calculation.

**Rules.** Optional.

## Description {#IncidentRegister.Description}
<!-- alias: loc:Description, dom:IncidentRegister_Description -->

**What it is.** What happened.

**Rules.** Optional.
