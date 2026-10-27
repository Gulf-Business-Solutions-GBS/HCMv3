---
helpId: gmi.separation-reason.edit
lang: ar
title: أسباب الفصل — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.SeparationReason
surfaces:
  - angular/src/app/admin/general-master-info/separation-reasons/create-or-edit-separation-reason-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.SeparationReasons.Create]
---

## Overview

الأسباب التي قد تنتهي بها خدمة الموظف -- الاستقالة، والإنهاء، وانتهاء العقد، ونحو ذلك. ويسمّي كل سجل فصل سبباً منها.

## IsSystemRecord {#SeparationReason.IsSystemRecord}
<!-- alias: loc:IsSystemRecord, dom:SeparationReason_IsSystemRecord -->

**ما هو.** يميّز السبب الذي جاء مع النظام.

**لماذا يهم.** لا يستطيع تعديل سبب النظام أو حذفه إلا المسؤول.

**القواعد.** نعم أو لا.

## AllowAdvanceEoS {#SeparationReason.AllowAdvanceEoS}
<!-- alias: loc:AllowAdvanceEoS, dom:SeparationReason_AllowAdvanceEoS -->

**ما هو.** كان المقصود منه السماح بصرف مكافأة نهاية الخدمة مقدماً لهذا السبب.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.
