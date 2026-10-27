---
helpId: hs.incident-claim.edit
lang: en
title: Incident Claims — Create / Edit
entity: HCMv3.HealthAndSafetyManagement.IncidentClaim
surfaces:
  - angular/src/app/admin/health-and-safety-management/incident-claims/create-or-edit-incident-claim-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.IncidentClaims.Create]
---

## Overview

An insurance or compensation claim arising from an incident. Kept as a record: it is not paid through payroll.

## ClaimDate {#IncidentClaim.ClaimDate}
<!-- alias: loc:ClaimDate, dom:IncidentClaim_ClaimDate -->

**What it is.** When the claim was made.

**Rules.** Required. Date value.

## Incident {#IncidentClaim.IncidentId}
<!-- alias: loc:Incident, dom:IncidentClaim_IncidentId -->

**What it is.** The incident this belongs to.

**Rules.** Required. Chosen from Incident Register master data.

## Amount {#IncidentClaim.Amount}
<!-- alias: loc:Amount, dom:IncidentClaim_Amount -->

**What it is.** The amount claimed.

**Example.** 1,500.000

**Rules.** Required. Number.

## Currency {#IncidentClaim.CurrencyId}
<!-- alias: loc:Currency, dom:IncidentClaim_CurrencyId -->

**What it is.** The currency of the amount.

**Example.** KWD

**Rules.** Required. Chosen from Currency master data.

## PaymentDate {#IncidentClaim.PaymentDate}
<!-- alias: loc:PaymentDate, dom:IncidentClaim_PaymentDate -->

**What it is.** When it was, or is expected to be, paid.

**Rules.** Required. Date value.

## ClaimStatus {#IncidentClaim.ClaimStatusId}
<!-- alias: loc:ClaimStatus, dom:IncidentClaim_ClaimStatusId -->

**What it is.** Where the claim stands.

**Example.** Submitted

**Rules.** Optional. Chosen from Incident Claim Status master data.
