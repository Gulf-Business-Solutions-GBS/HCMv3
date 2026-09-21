---
helpId: perf.papeer-nominee.edit
lang: en
title: PA Peer Nominees — Create / Edit
entity: HCMv3.PerformanceManagement.PAPeerNominee
surfaces:
  - angular/src/app/admin/performance-management/pa-peer-nominees/create-or-edit-pa-peer-nominee-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PAPeerNominees.Create]
---

## Overview

A peer an employee has nominated to review them in a period. A peer can be nominated once per employee and period. A peer review shows as submitted when its nomination is marked submitted.

## PAPeriod {#PAPeerNominee.PAPeriodId}
<!-- alias: loc:PAPeriod, dom:PAPeerNominee_PAPeriodId -->

**What it is.** The appraisal period.

**Rules.** Required. Chosen from PA Period master data.

## Employee {#PAPeerNominee.EmployeeId}
<!-- alias: loc:Employee, dom:PAPeerNominee_EmployeeId -->

**What it is.** The employee being reviewed.

**Rules.** Required. Chosen from the employee list.

## Peer {#PAPeerNominee.PeerId}
<!-- alias: loc:Peer, dom:PAPeerNominee_PeerId -->

**What it is.** The nominated peer.

**Rules.** Required. Chosen from the employee list.

## Submitted {#PAPeerNominee.Submitted}
<!-- alias: loc:Submitted, dom:PAPeerNominee_Submitted -->

**What it is.** Whether the peer has submitted the review.

**Rules.** Yes or no.
