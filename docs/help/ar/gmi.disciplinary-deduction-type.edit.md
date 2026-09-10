---
helpId: gmi.disciplinary-deduction-type.edit
lang: ar
title: أنواع الخصم بسبب إجراء تأديبي — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.DisciplinaryDeductionType
surfaces:
  - angular/src/app/admin/general-master-info/disciplinary-deduction-types/create-or-edit-disciplinary-deduction-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DisciplinaryDeductionTypes.Create]
---

## Overview

الخصومات التي يمكن أن يترتب عليها إجراء تأديبي -- عدد من أيام الأجر أو ساعاته، أو مبلغ ثابت، أو نسبة مئوية. وتحدد أنواع المخالفات أي خصم ينطبق على أي مخالفة.

## DisciplinaryDeductionUnit {#DisciplinaryDeductionType.DisciplinaryDeductionUnit}
<!-- alias: loc:DisciplinaryDeductionUnit, dom:DisciplinaryDeductionType_DisciplinaryDeductionUnit -->

**ما هو.** الوحدة التي تُقاس بها قيمة الخصم.

**القواعد.** إلزامي. إحدى القيم: أيام، ساعات، مبلغ ثابت، نسبة مئوية.

## DeductionValue {#DisciplinaryDeductionType.DeductionValue}
<!-- alias: loc:DeductionValue, dom:DisciplinaryDeductionType_DeductionValue -->

**ما هو.** مقدار الخصم بالوحدة المختارة.

**مثال.** `1` مع وحدة أيام، لخصم أجر يوم واحد

**لماذا يهم.** إذا كانت الوحدة نسبة مئوية فيجب أن تكون القيمة بين 0 و100.

**القواعد.** إلزامي. رقم.

:::admin
تُحفظ فقط: لا يقرأ أي احتساب للرواتب الوحدة ولا القيمة حالياً، لذا فتسجيل إجراء تأديبي لا ينقص الأجر من تلقاء نفسه.
:::
