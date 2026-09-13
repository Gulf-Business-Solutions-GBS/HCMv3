---
helpId: pa.employee-attendance-quota.edit
lang: en
title: Employee Attendance Quotas — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAttendanceQuota
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/attendance-quotas/create-or-edit-employee-attendance-quota-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAttendanceQuotas.Create]
---

## Overview

An attendance entitlement for the employee over a period.

It is recorded for reference; no calculation reads it at present.

## EntitlementFrom {#EmployeeAttendanceQuota.EntitlementFrom}
<!-- alias: loc:EntitlementFrom, dom:EmployeeAttendanceQuota_EntitlementFrom -->

**What it is.** The first day of the entitlement period.

**Example.** 1 Jan 2026

**Rules.** Required. Date value.

## EntitlementTo {#EmployeeAttendanceQuota.EntitlementTo}
<!-- alias: loc:EntitlementTo, dom:EmployeeAttendanceQuota_EntitlementTo -->

**What it is.** The last day of the entitlement period.

**Example.** 31 Dec 2026

**Rules.** Required. Date value.

## DeductionFrom {#EmployeeAttendanceQuota.DeductionFrom}
<!-- alias: loc:DeductionFrom, dom:EmployeeAttendanceQuota_DeductionFrom -->

**What it is.** The first day the entitlement can be used.

**Example.** 1 Jan 2026

**Rules.** Required. Date value.

## DeductionTo {#EmployeeAttendanceQuota.DeductionTo}
<!-- alias: loc:DeductionTo, dom:EmployeeAttendanceQuota_DeductionTo -->

**What it is.** The last day the entitlement can be used.

**Example.** 31 Dec 2026

**Rules.** Required. Date value.

## QuotaAccrued {#EmployeeAttendanceQuota.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeAttendanceQuota_QuotaAccrued -->

**What it is.** The amount earned in the period.

**Example.** 10

**Rules.** Optional. Number.

## QuotaUsed {#EmployeeAttendanceQuota.QuotaUsed}
<!-- alias: loc:QuotaUsed, dom:EmployeeAttendanceQuota_QuotaUsed -->

**What it is.** The amount used in the period.

**Example.** 4

**Rules.** Optional. Number.
