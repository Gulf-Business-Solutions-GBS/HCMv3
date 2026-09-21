---
helpId: perf.kpi-by-org-unit.edit
lang: en
title: KPI By Org Unit — Create / Edit
entity: HCMv3.PerformanceManagement.KpiByOrgUnit
surfaces:
  - angular/src/app/admin/performance-management/kpi-by-org-units/create-or-edit-kpi-by-org-unit-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.KpiByOrgUnits.Create]
---

## Overview

A set of KPIs for an organisation unit, from a given date. Each unit can have one set per start date.

Stored as a template: appraisals do not take their KPIs from it automatically at present.

## OrganizationUnit {#KpiByOrgUnit.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:KpiByOrgUnit_OrganizationUnitId -->

**What it is.** The organisation unit.

**Example.** Finance Department

**Rules.** Required. Chosen from Organization Unit master data.
