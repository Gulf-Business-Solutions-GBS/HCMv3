---
helpId: hs.first-aid-box-detail.edit
lang: en
title: First Aid Box Details — Create / Edit
entity: HCMv3.HealthAndSafetyManagement.FirstAidBoxDetail
surfaces:
  - angular/src/app/admin/health-and-safety-management/first-aid-box-detail/details/create-or-edit-first-aid-box-detail-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.FirstAidBoxDetails.Create]
---

## Overview

One medicine or item in a first-aid box.

## Medicine {#FirstAidBoxDetail.MedicineId}
<!-- alias: loc:Medicine, dom:FirstAidBoxDetail_MedicineId -->

**What it is.** The medicine or item.

**Example.** Bandages

**Rules.** Required. Chosen from First Aid Medicine master data.

## AllocatedBy {#FirstAidBoxDetail.AllocatedById}
<!-- alias: loc:AllocatedBy, dom:FirstAidBoxDetail_AllocatedById -->

**What it is.** Who put it in the box.

**Rules.** Required. Chosen from the employee list.

## ExpiryDate {#FirstAidBoxDetail.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:FirstAidBoxDetail_ExpiryDate -->

**What it is.** When it expires.

**Why it matters.** A record only: no reminder is sent when it comes round.

**Rules.** Required. Date value.

## AllocationDate {#FirstAidBoxDetail.AllocationDate}
<!-- alias: loc:AllocationDate, dom:FirstAidBoxDetail_AllocationDate -->

**What it is.** When it was put in the box.

**Rules.** Required. Date value.

## IsExpired {#FirstAidBoxDetail.IsExpired}
<!-- alias: loc:IsExpired, dom:FirstAidBoxDetail_IsExpired -->

**What it is.** Whether it has expired.

**Why it matters.** Ticked by hand; it is not set from the expiry date.

**Rules.** Yes or no.

## IsReplaced {#FirstAidBoxDetail.IsReplaced}
<!-- alias: loc:IsReplaced, dom:FirstAidBoxDetail_IsReplaced -->

**What it is.** Whether it has been replaced.

**Rules.** Yes or no.
