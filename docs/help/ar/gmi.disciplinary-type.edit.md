---
helpId: gmi.disciplinary-type.edit
lang: ar
title: أنواع المخالفات — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.DisciplinaryType
surfaces:
  - angular/src/app/admin/general-master-info/disciplinary-types/create-or-edit-disciplinary-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DisciplinaryTypes.Create]
---

## Overview

أنواع المخالفات -- التأخر في الحضور، والغياب دون إذن، ونحو ذلك. يسمّي كل إجراء تأديبي يُسجَّل على الموظف نوعاً منها، ويحدد سُلّم التصعيد الخاص بالنوع الخصم الذي تترتب عليه المخالفة.

## RepeatDurationInDays {#DisciplinaryType.RepeatDurationInDays}
<!-- alias: loc:RepeatDurationInDays, dom:DisciplinaryType_RepeatDurationInDays -->

**ما هو.** عدد الأيام التي تُحتسب خلالها المخالفات السابقة من هذا النوع لأغراض التصعيد.

**مثال.** `365`

**لماذا يهم.** تُحتسب المخالفة السابقة إذا وقعت خلال هذا العدد من الأيام قبل المخالفة الجديدة، مع احتساب طرفي المدة. ومع القيمة 0 لا تُحتسب إلا المخالفات الواقعة في اليوم نفسه، فلا يتصاعد النوع أبداً.

**القواعد.** إلزامي. رقم صحيح.
