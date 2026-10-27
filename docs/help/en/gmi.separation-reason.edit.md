---
helpId: gmi.separation-reason.edit
lang: en
title: Separation Reasons — Create / Edit
entity: HCMv3.GeneralMasterInfo.SeparationReason
surfaces:
  - angular/src/app/admin/general-master-info/separation-reasons/create-or-edit-separation-reason-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.SeparationReasons.Create]
---

## Overview

The reasons an employee's service can end -- resignation, termination, end of contract and so on. Each separation record names one.

## IsSystemRecord {#SeparationReason.IsSystemRecord}
<!-- alias: loc:IsSystemRecord, dom:SeparationReason_IsSystemRecord -->

**What it is.** Marks a reason that came with the system.

**Why it matters.** Only an administrator can edit or delete a system reason.

**Rules.** Yes or no.

## AllowAdvanceEoS {#SeparationReason.AllowAdvanceEoS}
<!-- alias: loc:AllowAdvanceEoS, dom:SeparationReason_AllowAdvanceEoS -->

**What it is.** Meant to allow end-of-service benefits to be paid in advance for this reason.

**Why it matters.** Not read at present.

**Rules.** Yes or no.
