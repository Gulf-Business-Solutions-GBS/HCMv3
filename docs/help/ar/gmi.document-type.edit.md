---
helpId: gmi.document-type.edit
lang: ar
title: أنواع الوثائق — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.DocumentType
surfaces:
  - angular/src/app/admin/general-master-info/document-types/create-or-edit-document-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DocumentTypes.Create]
---

## Overview

أنواع الوثائق التي يحتفظ بها HCMv3 -- للموظفين وللشركة. تسمّي وثائق الموظفين ووثائق الشركة وطلبات الخطابات في الخدمة الذاتية كلٌّ منها نوعاً من أنواع الوثائق.

إعدادات التذكير في هذا النموذج محفوظة، لكن لا تقرؤها أي عملية حالياً، فهي لا ترسل بعدُ تذكيرات قبل انتهاء صلاحية الوثيقة.

## NotificationPeriod {#DocumentType.NotificationPeriod}
<!-- alias: loc:NotificationPeriod, dom:DocumentType_NotificationPeriod -->

**ما هو.** المدة التي كان يُفترض أن تبدأ فيها التذكيرات قبل انتهاء صلاحية الوثيقة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. رقم صحيح.

## NotificationConfig {#DocumentType.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:DocumentType_NotificationConfigId -->

**ما هو.** إعدادات الإخطار المخصصة لتذكيرات هذا النوع من الوثائق.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## NotificationFrequency {#DocumentType.NotificationFrequency}
<!-- alias: loc:NotificationFrequency, dom:DocumentType_NotificationFrequency -->

**ما هو.** عدد مرات تكرار التذكير التي كانت مقصودة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. إحدى القيم: مرة واحدة، يومي، إسبوعي، شهري.

## NotificationCountLimit {#DocumentType.NotificationCountLimit}
<!-- alias: loc:NotificationCountLimit, dom:DocumentType_NotificationCountLimit -->

**ما هو.** الحد الأقصى المقصود لعدد التذكيرات للوثيقة الواحدة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. رقم صحيح.

## DocumentBelongsTo {#DocumentType.DocumentBelongsTo}
<!-- alias: loc:DocumentBelongsTo, dom:DocumentType_DocumentBelongsTo -->

**ما هو.** هل وثائق هذا النوع تخص الموظفين أم الشركة.

**لماذا يهم.** يُسجَّل للعلم، ولا يُصفّى به أي شيء حالياً.

**القواعد.** إلزامي. إحدى القيم: الكل، الموظف، الشركة.
