---
helpId: suc.successor-nominee.edit
lang: en
title: Successor Nominees — Create / Edit
entity: HCMv3.SuccessionManagement.SuccessorNominee
surfaces:
  - angular/src/app/admin/succession-management/successor-nominees/create-or-edit-successor-nominee-modal.component.html
audience: [admin]
permissions: [Pages.SuccessionManagement.SuccessorNominees.Create]
---

## Overview

An employee nominated to succeed another, with how ready they are. The same nominee can be named once per employee. Kept as a record: nothing reads it at present.

## Employee {#SuccessorNominee.EmployeeId}
<!-- alias: loc:Employee, dom:SuccessorNominee_EmployeeId -->

**What it is.** The employee being succeeded.

**Rules.** Required. Chosen from the employee list.

## Nominee {#SuccessorNominee.NomineeId}
<!-- alias: loc:Nominee, dom:SuccessorNominee_NomineeId -->

**What it is.** The employee nominated as successor.

**Rules.** Required. Chosen from the employee list.

## ReadinessId {#SuccessorNominee.ReadinessId}
<!-- alias: loc:ReadinessId, dom:SuccessorNominee_ReadinessId -->

**What it is.** How soon the nominee could step in.

**Example.** Ready in 1-2 years

**Rules.** Required. Chosen from Readiness master data.

## RankingId {#SuccessorNominee.RankingId}
<!-- alias: loc:RankingId, dom:SuccessorNominee_RankingId -->

**What it is.** The nominee's rank among the successors.

**Example.** 1

**Rules.** Required. Chosen from Ranking master data.
