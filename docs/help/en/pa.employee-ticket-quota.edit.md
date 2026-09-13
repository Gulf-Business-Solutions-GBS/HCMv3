---
helpId: pa.employee-ticket-quota.edit
lang: en
title: Employee Ticket Quotas — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeTicketQuota
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/ticket-quotas/create-or-edit-employee-ticket-quota-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTicketQuotas.Create]
---

## Overview

The employee's air ticket entitlement for one ticket type over an entitlement period.

It is shown on the air ticket quota balance report, whose balance is accrued minus used and adjusted. The time engine works out ticket entitlement from the sub group's ticket rules and the tickets already paid, and does not read these quotas at present.

## TicketTypeId {#EmployeeTicketQuota.TicketTypeId}
<!-- alias: loc:TicketTypeId, dom:EmployeeTicketQuota_TicketTypeId -->

**What it is.** The ticket type this quota is for.

**Example.** Employee

**Rules.** Required. Chosen from Ticket Type master data.

## EntitlementFrom {#EmployeeTicketQuota.EntitlementFrom}
<!-- alias: loc:EntitlementFrom, dom:EmployeeTicketQuota_EntitlementFrom -->

**What it is.** The first day of the entitlement period.

**Example.** 1 Jan 2026

**Why it matters.** The report uses the quota covering its as-on date.

**Rules.** Required. Date value.

## EntitlementTo {#EmployeeTicketQuota.EntitlementTo}
<!-- alias: loc:EntitlementTo, dom:EmployeeTicketQuota_EntitlementTo -->

**What it is.** The last day of the entitlement period.

**Example.** 31 Dec 2027

**Rules.** Required. Date value.

## DeductionFrom {#EmployeeTicketQuota.DeductionFrom}
<!-- alias: loc:DeductionFrom, dom:EmployeeTicketQuota_DeductionFrom -->

**What it is.** The first day tickets can be taken from this quota.

**Example.** 1 Jan 2026

**Rules.** Required. Date value.

## DeductionTo {#EmployeeTicketQuota.DeductionTo}
<!-- alias: loc:DeductionTo, dom:EmployeeTicketQuota_DeductionTo -->

**What it is.** The last day tickets can be taken from this quota.

**Example.** 31 Dec 2027

**Rules.** Required. Date value.

## QuotaAccrued {#EmployeeTicketQuota.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeTicketQuota_QuotaAccrued -->

**What it is.** Tickets earned in the period.

**Example.** 1

**Rules.** Optional. Number.

## QuotaUsed {#EmployeeTicketQuota.QuotaUsed}
<!-- alias: loc:QuotaUsed, dom:EmployeeTicketQuota_QuotaUsed -->

**What it is.** Tickets taken in the period.

**Example.** 0

**Rules.** Optional. Number.

## QuotaAdjusted {#EmployeeTicketQuota.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeTicketQuota_QuotaAdjusted -->

**What it is.** Tickets adjusted in the period.

**Example.** 0

**Rules.** Optional. Number.
