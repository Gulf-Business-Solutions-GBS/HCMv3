---
helpId: gmi.overtime-type.edit
lang: en
title: Overtime Types — Create / Edit
entity: HCMv3.GeneralMasterInfo.OvertimeType
surfaces:
  - angular/src/app/admin/general-master-info/overtime-types/create-or-edit-overtime-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.OvertimeTypes.Create]
---

## Overview

The kinds of overtime -- on a working day, a rest day, a public holiday and so on. Every overtime request names a type, and duties and holidays can point to the type that applies to them.

## OvertimeRate {#OvertimeType.OvertimeRate}
<!-- alias: loc:OvertimeRate, dom:OvertimeType_OvertimeRate -->

**What it is.** The multiplier applied to approved overtime hours.

**Example.** `1.25`

**Why it matters.** Payroll multiplies the approved overtime hours by this rate before pricing them, and project costing uses the same multiplier.

**Rules.** Required. Number.

## WageType {#OvertimeType.WageTypeId}
<!-- alias: loc:WageType, dom:OvertimeType_WageTypeId -->

**What it is.** The wagetype that overtime of this type is paid under.

**Example.** `OT125 - Overtime 125%`

**Why it matters.** Payroll pays approved overtime only for overtime types that have a wagetype: it prices the hours when it reaches that wagetype and puts the amount on the payslip under it. Left blank, overtime of this type is recorded but never paid. The project month-wise summary report also uses it to tell overtime cost apart from other pay.

**Rules.** Optional. Chosen from Wage Type master data.
