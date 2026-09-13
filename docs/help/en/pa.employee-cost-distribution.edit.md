---
helpId: pa.employee-cost-distribution.edit
lang: en
title: Employee Cost Distributions — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeCostDistribution
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/cost-distributions/create-or-edit-employee-cost-distribution-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeCostDistributions.Create]
---

## Overview

How the employee's cost was spread across organisation units for a period.

It is recorded for reference; the cost engine does not read it at present.

## OrganizationUnitId {#EmployeeCostDistribution.OrganizationUnitId}
<!-- alias: loc:OrganizationUnitId, dom:EmployeeCostDistribution_OrganizationUnitId -->

**What it is.** The organisation unit the cost is distributed to.

**Example.** Projects Division

**Rules.** Optional. Chosen from Organization Unit master data.

## CostCount {#EmployeeCostDistribution.CostCount}
<!-- alias: loc:CostCount, dom:EmployeeCostDistribution_CostCount -->

**What it is.** A count recorded with the distribution.

**Example.** 3

**Rules.** Optional. Whole number.

## UsedDays {#EmployeeCostDistribution.UsedDays}
<!-- alias: loc:UsedDays, dom:EmployeeCostDistribution_UsedDays -->

**What it is.** The number of days recorded against this unit.

**Example.** 12

**Rules.** Optional. Whole number.
