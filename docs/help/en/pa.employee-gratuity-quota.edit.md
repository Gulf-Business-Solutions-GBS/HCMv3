---
helpId: pa.employee-gratuity-quota.edit
lang: en
title: Employee Gratuity Quotas — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeGratuityQuota
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/gratuity-quotas/create-or-edit-employee-gratuity-quota-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeGratuityQuotas.Create]
---

## Overview

The employee's indemnity entitlement over an entitlement period.

The time engine creates a quota itself for any day no quota covers when the employee's sub group has an indemnity rule. The indemnity calculation overwrites Quota Accrued and Quota Used from its own results each time it runs.

## EntitlementFrom {#EmployeeGratuityQuota.EntitlementFrom}
<!-- alias: loc:EntitlementFrom, dom:EmployeeGratuityQuota_EntitlementFrom -->

**What it is.** The first day of the entitlement period.

**Example.** 1 Jan 2026

**Rules.** Required. Date value.

## EntitlementTo {#EmployeeGratuityQuota.EntitlementTo}
<!-- alias: loc:EntitlementTo, dom:EmployeeGratuityQuota_EntitlementTo -->

**What it is.** The last day of the entitlement period.

**Example.** 31 Dec 2026

**Why it matters.** The indemnity provision report uses the quota covering its as-on date.

**Rules.** Required. Date value.

## DeductionFrom {#EmployeeGratuityQuota.DeductionFrom}
<!-- alias: loc:DeductionFrom, dom:EmployeeGratuityQuota_DeductionFrom -->

**What it is.** The first day of the deduction period.

**Example.** 1 Jan 2026

**Why it matters.** Shown on the indemnity provision report.

**Rules.** Required. Date value.

## DeductionTo {#EmployeeGratuityQuota.DeductionTo}
<!-- alias: loc:DeductionTo, dom:EmployeeGratuityQuota_DeductionTo -->

**What it is.** The last day of the deduction period.

**Example.** 31 Dec 2026

**Why it matters.** Shown on the indemnity provision report.

**Rules.** Required. Date value.

## QuotaAccrued {#EmployeeGratuityQuota.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeGratuityQuota_QuotaAccrued -->

**What it is.** Indemnity days accrued.

**Example.** 21

**Why it matters.** Overwritten by the indemnity calculation.

**Rules.** Optional. Number.

## QuotaUsed {#EmployeeGratuityQuota.QuotaUsed}
<!-- alias: loc:QuotaUsed, dom:EmployeeGratuityQuota_QuotaUsed -->

**What it is.** Indemnity days paid.

**Example.** 0

**Why it matters.** Overwritten by the indemnity calculation.

**Rules.** Optional. Number.

## QuotaAdjusted {#EmployeeGratuityQuota.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeGratuityQuota_QuotaAdjusted -->

**What it is.** Indemnity days adjusted.

**Example.** 0

**Why it matters.** Recorded only; the indemnity calculation does not read it at present.

**Rules.** Optional. Number.
