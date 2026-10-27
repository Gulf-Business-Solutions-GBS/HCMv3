---
helpId: gmi.personal-action-reason.edit
lang: ar
title: أسباب الإجراءات الشخصية — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.PersonalActionReason
surfaces:
  - angular/src/app/admin/general-master-info/personal-action-reasons/create-or-edit-personal-action-reason-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.PersonalActionReasons.Create]
---

## Overview

الأسباب التي يمكن ذكرها لإجراء شخصي -- لماذا نُقل الموظف أو رُقّي، ونحو ذلك. ويُسجِّل كل إجراء للموظف الإجراءَ وسبباً له.

## PersonalAction {#PersonalActionReason.PersonalActionId}
<!-- alias: loc:PersonalAction, dom:PersonalActionReason_PersonalActionId -->

**ما هو.** الإجراء الشخصي الذي ينتمي إليه هذا السبب.

**القواعد.** اختياري. يُختار من قائمة الإجراءات الشخصية.

## NotificationConfig {#PersonalActionReason.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:PersonalActionReason_NotificationConfigId -->

**ما هو.** البريد الإلكتروني الذي يُرسل عند اعتماد إجراء موظف بهذا السبب.

**مثال.** خطاب الترقية

**لماذا يهم.** عند الاعتماد يُرسل قالب البريد الإلكتروني الخاص بالإعداد بشأن الموظف إلى من يحددهم الإعداد. ولا يُرسل إلا البريد الإلكتروني. ولا يُرسل شيء إذا تُرك فارغاً أو لم يكن للإعداد قالب بريد إلكتروني، أو عند اعتماد تعديل على إجراء. وإخفاق الإرسال لا يُلغي الاعتماد أبداً.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.
