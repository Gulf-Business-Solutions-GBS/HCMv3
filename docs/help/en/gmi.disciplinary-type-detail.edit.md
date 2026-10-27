---
helpId: gmi.disciplinary-type-detail.edit
lang: en
title: Violation Type Details — Create / Edit
entity: HCMv3.GeneralMasterInfo.DisciplinaryTypeDetail
surfaces:
  - angular/src/app/admin/general-master-info/disciplinary-type-details/create-or-edit-disciplinary-type-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DisciplinaryTypeDetails.Create]
---

## Overview

One rung of a violation type's escalation ladder: from which offence onwards a deduction type applies.

When an offence is recorded, HCMv3 counts the employee's earlier offences of the same type within the type's repeat duration, leaving out any that were refused, and adds one for the new offence. The rung used is the one with the highest offence count that does not exceed that number. With rungs at 1, 3 and 5, the first and second offences use rung 1, the third and fourth rung 3, and every offence from the fifth on rung 5.

## DisciplinaryType {#DisciplinaryTypeDetail.DisciplinaryTypeId}
<!-- alias: loc:DisciplinaryType, dom:DisciplinaryTypeDetail_DisciplinaryTypeId -->

**What it is.** The violation type this rung belongs to.

**Rules.** Optional. Chosen from Violation Type master data.

## DisciplinaryCount {#DisciplinaryTypeDetail.DisciplinaryCount}
<!-- alias: loc:DisciplinaryCount, dom:DisciplinaryTypeDetail_DisciplinaryCount -->

**What it is.** The offence number from which this rung applies.

**Example.** `3`, for the third offence onwards

**Rules.** Required. Whole number.

## DisciplinaryDeductionType {#DisciplinaryTypeDetail.DisciplinaryDeductionTypeId}
<!-- alias: loc:DisciplinaryDeductionType, dom:DisciplinaryTypeDetail_DisciplinaryDeductionTypeId -->

**What it is.** The deduction that applies from this offence onwards.

**Rules.** Optional. Chosen from Violation Deduction Type master data.
