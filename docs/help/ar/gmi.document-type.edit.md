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

## IncludeInWorkflow {#DocumentType.IncludeInWorkflow}
<!-- alias: loc:IncludeInWorkflow, dom:DocumentType_IncludeInWorkflow -->

**ما هو.** هل يُعرض هذا النوع على الموظفين عند تقديم طلب في الخدمة الذاتية.

**مثال.** مفعّل لجواز السفر وشهادة الراتب، وغير مفعّل لنوع لا يسجّله إلا الموارد البشرية.

**لماذا يهم.** عند إلغاء التفعيل يختفي النوع من قائمة «طلب جديد» في شاشة وثائق الموظف ويُرفض أي طلب له. وتبقى الموارد البشرية قادرة على تسجيل وثائق هذا النوع.

## IsLetterType {#DocumentType.IsLetterType}
<!-- alias: loc:IsLetterType, dom:DocumentType_IsLetterType -->

**ما هو.** هل هذا النوع خطاب تصدره الشركة للموظف، كشهادة الراتب، وليس وثيقة يحملها الموظف.

**مثال.** مفعّل لشهادة الراتب وشهادة الخبرة، وغير مفعّل لجواز السفر والبطاقة المدنية.

**لماذا يهم.** النوع المفعّل يُعرض ضمن طلب الخطابات ويُسأل فيه عن الجهة الموجَّه إليها الخطاب. والنوع غير المفعّل يُعرض ضمن «تسجيل وثيقة» ويُسأل فيه عن الرقم والتواريخ. ولا يكون النوع الواحد كليهما.

## IsWithdrawable {#DocumentType.IsWithdrawable}
<!-- alias: loc:IsWithdrawable, dom:DocumentType_IsWithdrawable -->

**ما هو.** هل يجوز للموظفين طلب سحب وثيقة من هذا النوع من عهدة الشركة ثم إعادتها.

**مثال.** مفعّل لجواز السفر.

**لماذا يهم.** لا يُعرض «سحب الوثيقة» على الموظف إلا للأنواع المفعّلة. ويجوز تفعيل أكثر من نوع. وإن لم يُفعَّل أي نوع رجع النظام إلى النوع الواحد المحدَّد في إعداد جواز السفر السابق (الرمز 2 ما لم يُغيَّر).

## IsHiddenFromEmployee {#DocumentType.IsHiddenFromEmployee}
<!-- alias: loc:IsHiddenFromEmployee, dom:DocumentType_IsHiddenFromEmployee -->

**ما هو.** يُبقي وثائق هذا النوع خارج شاشة الوثائق الخاصة بالموظف.

**مثال.** مفعّل لوثيقة تحتفظ بها الموارد البشرية عن الموظف لا له.

**لماذا يهم.** لا يرى الموظف هذه الوثائق ولا يستطيع تسجيل واحدة منها. ولا تتأثر شاشات الموارد البشرية.
