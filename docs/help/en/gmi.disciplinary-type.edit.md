---
helpId: gmi.disciplinary-type.edit
lang: en
title: Violation Types — Create / Edit
entity: HCMv3.GeneralMasterInfo.DisciplinaryType
surfaces:
  - angular/src/app/admin/general-master-info/disciplinary-types/create-or-edit-disciplinary-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DisciplinaryTypes.Create]
---

## Overview

The kinds of disciplinary offence -- late arrival, absence without leave and so on. Each disciplinary action recorded against an employee names one, and the type's escalation ladder decides which deduction the offence carries.

## RepeatDurationInDays {#DisciplinaryType.RepeatDurationInDays}
<!-- alias: loc:RepeatDurationInDays, dom:DisciplinaryType_RepeatDurationInDays -->

**What it is.** How far back, in days, earlier offences of this type count towards escalation.

**Example.** `365`

**Why it matters.** An earlier offence counts if it falls within this many days before the new one, counting both ends. With 0, only offences on the same day count, so the type never escalates.

**Rules.** Required. Whole number.
