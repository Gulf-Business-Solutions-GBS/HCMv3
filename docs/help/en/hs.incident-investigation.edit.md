---
helpId: hs.incident-investigation.edit
lang: en
title: Incident Investigations — Create / Edit
entity: HCMv3.HealthAndSafetyManagement.IncidentInvestigation
surfaces:
  - angular/src/app/admin/health-and-safety-management/incident-investigations/create-or-edit-incident-investigation-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.IncidentInvestigations.Create]
---

## Overview

An investigation into an incident and its root cause.

## InvestigationDate {#IncidentInvestigation.InvestigationDate}
<!-- alias: loc:InvestigationDate, dom:IncidentInvestigation_InvestigationDate -->

**What it is.** When the investigation took place.

**Rules.** Required. Date value.

## InvestigationTime {#IncidentInvestigation.InvestigationTime}
<!-- alias: loc:InvestigationTime, dom:IncidentInvestigation_InvestigationTime -->

**What it is.** The time of day, if known.

**Example.** 14:30

**Rules.** Optional. Date value.

## Incident {#IncidentInvestigation.IncidentId}
<!-- alias: loc:Incident, dom:IncidentInvestigation_IncidentId -->

**What it is.** The incident this belongs to.

**Rules.** Required. Chosen from Incident Register master data.

## RootCause {#IncidentInvestigation.RootCauseId}
<!-- alias: loc:RootCause, dom:IncidentInvestigation_RootCauseId -->

**What it is.** The main cause found.

**Example.** Wet floor not signed

**Rules.** Required. Chosen from Investigation Root Cause master data.

## RootCauseAnalysis {#IncidentInvestigation.RootCauseAnalysis}
<!-- alias: loc:RootCauseAnalysis, dom:IncidentInvestigation_RootCauseAnalysis -->

**What it is.** How the cause was reached.

**Rules.** Optional.
