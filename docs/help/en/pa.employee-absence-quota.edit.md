---
helpId: pa.employee-absence-quota.edit
lang: en
title: Employee Absence Quotas — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAbsenceQuota
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/absence-quotas/create-or-edit-employee-absence-quota-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAbsenceQuotas.Create]
---

## Overview

The employee's leave entitlement for one leave type over an entitlement period: how much accrued, and how much was used, adjusted or forfeited.

The self-service leave balance is built from the quota whose entitlement period covers the date. For annual leave, the time engine adds each day's accrual to Quota Accrued as it runs.

## AbsenceTypeId {#EmployeeAbsenceQuota.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:EmployeeAbsenceQuota_AbsenceTypeId -->

**What it is.** The leave type this quota is for.

**Example.** Annual Leave

**Rules.** Required. Chosen from Absence Type master data.

:::admin
Maintained under Payroll Management → Absence Types.
:::

## NotificationSent {#EmployeeAbsenceQuota.NotificationSent}
<!-- alias: loc:NotificationSent, dom:EmployeeAbsenceQuota_NotificationSent -->

**What it is.** Whether the annual-leave maximum-limit notification has been sent.

**Why it matters.** The time engine clears it once the balance drops back below the notification threshold, so the notification can be sent again.

**Rules.** Yes or no.

## EntitlementFrom {#EmployeeAbsenceQuota.EntitlementFrom}
<!-- alias: loc:EntitlementFrom, dom:EmployeeAbsenceQuota_EntitlementFrom -->

**What it is.** The first day of the entitlement period.

**Example.** 1 Jan 2026

**Why it matters.** The quota whose entitlement period covers a date is the one used for the balance on that date.

**Rules.** Required. Date value.

:::admin
Keep annual leave entitlement periods continuous. The time engine creates the first annual leave quota itself when the employee has none, but if the employee already has one and none covers a day it processes, it stops with "AbsenceQuota exists for Employee".
:::

## EntitlementTo {#EmployeeAbsenceQuota.EntitlementTo}
<!-- alias: loc:EntitlementTo, dom:EmployeeAbsenceQuota_EntitlementTo -->

**What it is.** The last day of the entitlement period.

**Example.** 31 Dec 2026

**Rules.** Required. Date value.

## DeductionFrom {#EmployeeAbsenceQuota.DeductionFrom}
<!-- alias: loc:DeductionFrom, dom:EmployeeAbsenceQuota_DeductionFrom -->

**What it is.** The first day leave can be taken from this quota.

**Example.** 1 Jan 2026

**Why it matters.** Leave forfeiture calculations pick the quota whose deduction period covers the month.

**Rules.** Required. Date value.

## DeductionTo {#EmployeeAbsenceQuota.DeductionTo}
<!-- alias: loc:DeductionTo, dom:EmployeeAbsenceQuota_DeductionTo -->

**What it is.** The last day leave can be taken from this quota.

**Example.** 31 Mar 2027

**Rules.** Required. Date value.

## QuotaAccrued {#EmployeeAbsenceQuota.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeAbsenceQuota_QuotaAccrued -->

**What it is.** The days earned in this period.

**Example.** 30

**Rules.** Optional. Number.

## QuotaUsed {#EmployeeAbsenceQuota.QuotaUsed}
<!-- alias: loc:QuotaUsed, dom:EmployeeAbsenceQuota_QuotaUsed -->

**What it is.** The days taken from this quota.

**Example.** 12

**Why it matters.** Leave approved through self-service adds to it, and cancelling approved leave subtracts from it. Absences entered by HR on the absence form do not change it.

**Rules.** Optional. Number.

## QuotaAdjusted {#EmployeeAbsenceQuota.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeAbsenceQuota_QuotaAdjusted -->

**What it is.** Days adjusted on this quota -- for example encashed.

**Example.** 5

**Why it matters.** Saving a leave adjustment does not update this figure; enter it here if the quota should reflect the adjustment.

**Rules.** Optional. Number.

## QuotaForfeited {#EmployeeAbsenceQuota.QuotaForfeited}
<!-- alias: loc:QuotaForfeited, dom:EmployeeAbsenceQuota_QuotaForfeited -->

**What it is.** Days lost from this quota.

**Example.** 3

**Why it matters.** Leave forfeiture calculations update it. Saving a manual forfeit does not.

**Rules.** Optional. Number.
