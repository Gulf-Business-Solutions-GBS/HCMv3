---
helpId: gmi.disciplinary-deduction-type.edit
lang: en
title: Disciplinary Deduction Types — Create / Edit
entity: HCMv3.GeneralMasterInfo.DisciplinaryDeductionType
surfaces:
  - angular/src/app/admin/general-master-info/disciplinary-deduction-types/create-or-edit-disciplinary-deduction-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DisciplinaryDeductionTypes.Create]
---

## Overview

The deductions a disciplinary action can carry -- a number of days' or hours' pay, a fixed amount, or a percentage. Disciplinary types list which deduction applies to which offence.

## DisciplinaryDeductionUnit {#DisciplinaryDeductionType.DisciplinaryDeductionUnit}
<!-- alias: loc:DisciplinaryDeductionUnit, dom:DisciplinaryDeductionType_DisciplinaryDeductionUnit -->

**What it is.** What the deduction value is measured in.

**Rules.** Required. One of: Days, Hours, Fixed Amount, Percentage.

## DeductionValue {#DisciplinaryDeductionType.DeductionValue}
<!-- alias: loc:DeductionValue, dom:DisciplinaryDeductionType_DeductionValue -->

**What it is.** How much is deducted, in the chosen unit.

**Example.** `1` with the unit Days, for one day's pay

**Why it matters.** For a percentage it must be between 0 and 100.

**Rules.** Required. Number.

:::admin
Stored only: no payroll calculation reads the unit or the value at present, so recording a disciplinary action does not reduce pay by itself.
:::
