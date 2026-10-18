---
helpId: pay.employee-sub-group-gratuity.edit
lang: en
title: Employee Sub Group Gratuities — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupGratuity
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/gratuities/create-or-edit-employee-sub-group-gratuity-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupGratuities.Create]
---

## Overview

The grade's end-of-service indemnity scale: how many days' pay each year of service earns, in slabs by years of service.

Service is split across the slabs in order of **Change Year**. Kuwait's law, for example, is two slabs: 15 days a year up to year 5, and 30 days a year after that.

## ChangeYear {#EmployeeSubGroupGratuity.ChangeYear}
<!-- alias: loc:ChangeYear, dom:EmployeeSubGroupGratuity_ChangeYear -->

**What it is.** The year of service at which this slab ends.

**Example.** `5` for the first slab; `99` for the last.

**Rules.** Optional. Number.

:::admin
With three or more slabs, check the result on a long-serving employee: a middle slab is counted as its full Change Year in years, and the last slab is reduced to make up, which is only exact when every slab after the first earns the same days. This matches the legacy system and has been kept on purpose.
:::

## GratuityDays {#EmployeeSubGroupGratuity.GratuityDays}
<!-- alias: loc:GratuityDays, dom:EmployeeSubGroupGratuity_GratuityDays -->

**What it is.** The days' pay earned for each year of service in this slab.

**Example.** `15`

**Why it matters.** Indemnity days for the slab are these days divided by 365, times the days served in it.

**Rules.** Optional. Number.

## BasisDays {#EmployeeSubGroupGratuity.BasisDays}
<!-- alias: loc:BasisDays, dom:EmployeeSubGroupGratuity_BasisDays -->

**What it is.** The number of days a month's pay is divided by to value one indemnity day in this slab.

**Example.** `26`

**Why it matters.** 0 uses the **Fixed Days** of wagetype `1010`.

**Rules.** Optional. Number.

## ImpactFromDateOfJoining {#EmployeeSubGroupGratuity.ImpactFromDateOfJoining}
<!-- alias: loc:ImpactFromDateOfJoining, dom:EmployeeSubGroupGratuity_ImpactFromDateOfJoining -->

**What it is.** Whether this slab is measured against the whole service from the joining date.

**Why it matters.** Used when choosing which slab's **Basis Days** values the indemnity pay: ticked, the employee's whole service is compared with this slab and the ones after it, not only the service left after the earlier slabs.

**Rules.** Yes or no.
