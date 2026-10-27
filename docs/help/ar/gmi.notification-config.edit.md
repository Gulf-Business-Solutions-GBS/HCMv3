---
helpId: gmi.notification-config.edit
lang: ar
title: إعدادات الإخطار — إنشاء / تحرير
entity: HCMv3.GeneralMasterInfo.NotificationConfig
surfaces:
  - angular/src/app/admin/general-master-info/notification-configs/create-or-edit-notification-config-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.NotificationConfigs.Create]
---

## Overview

إعدادات تحدد هل يرسل HCMv3 بريداً إلكترونياً عن حدث ما، وإلى من. تشير إلى واحد منها أنواعُ الوثائق وإخطاراتُ النظام والإخطاراتُ المجدولة -- مثل التهنئة بعيد الميلاد وبذكرى الالتحاق بالعمل. ومهما كثرت الخانات المحددة، لا يتلقى أحد الرسالة نفسها مرتين.

## Name {#NotificationConfig.Name}
<!-- alias: loc:Name, dom:NotificationConfig_Name -->

**ما هو.** اسم تتعرّف به على هذه الإعدادات.

**مثال.** `التهنئة بعيد الميلاد`

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## EmailTemplate {#NotificationConfig.EmailTemplateId}
<!-- alias: loc:EmailTemplate, dom:NotificationConfig_EmailTemplateId -->

**ما هو.** قالب البريد الإلكتروني الذي يُرسل عند تنفيذ إعداد الإخطار هذا.

**مثال.** اعتماد طلب الإجازة

**لماذا يهم.** تُنشأ الرسالة من القالب، مع تعبئة رقم الطلب وملخصه ومن اتخذ الإجراء وملاحظاته. اختر **إشعار داخل النظام فقط** لعدم إرسال بريد إلكتروني؛ ويظل الإشعار داخل النظام يُرسل.

**القواعد.** اختياري. يُختار من قائمة قوالب البريد الإلكتروني.

## SendNotification {#NotificationConfig.SendNotification}
<!-- alias: loc:SendNotification, dom:NotificationConfig_SendNotification -->

**ما هو.** كان المقصود منه تشغيل الإخطارات داخل النظام.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## SendEmail {#NotificationConfig.SendEmail}
<!-- alias: loc:SendEmail, dom:NotificationConfig_SendEmail -->

**ما هو.** هل يُرسل بريد إلكتروني أصلاً.

**لماذا يهم.** لا تُرسل الإخطارات المجدولة مثل التهنئة بعيد الميلاد إلا عند تحديد هذه الخانة.

**القواعد.** نعم أو لا.

## ToEmployee {#NotificationConfig.ToEmployee}
<!-- alias: loc:ToEmployee, dom:NotificationConfig_ToEmployee -->

**ما هو.** مراسلة الموظف المعني.

**لماذا يهم.** تُتخطى إذا كان الموظف غير مفعّل.

**القواعد.** نعم أو لا.

## ToRequestCreator {#NotificationConfig.ToRequestCreator}
<!-- alias: loc:ToRequestCreator, dom:NotificationConfig_ToRequestCreator -->

**ما هو.** مراسلة الشخص الذي أنشأ الطلب.

**القواعد.** نعم أو لا.

## ToManager {#NotificationConfig.ToManager}
<!-- alias: loc:ToManager, dom:NotificationConfig_ToManager -->

**ما هو.** مراسلة مدير الموظف.

**لماذا يهم.** لا تُرسل إلا إذا كان الموظف والمدير كلاهما مفعّلين.

**القواعد.** نعم أو لا.

## SendToApprover {#NotificationConfig.SendToApprover}
<!-- alias: loc:SendToApprover, dom:NotificationConfig_SendToApprover -->

**ما هو.** كان المقصود منه مراسلة المعتمِد الحالي.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## SendToAll {#NotificationConfig.SendToAll}
<!-- alias: loc:SendToAll, dom:NotificationConfig_SendToAll -->

**ما هو.** مراسلة كل موظف مفعّل.

**القواعد.** نعم أو لا.

## SendToAllApproved {#NotificationConfig.SendToAllApproved}
<!-- alias: loc:SendToAllApproved, dom:NotificationConfig_SendToAllApproved -->

**ما هو.** في طلبات سير العمل، مراسلة كل من اعتمد الطلب.

**لماذا يهم.** يُتخطى المعتمِدون الذين أصبحوا غير مفعّلين.

**القواعد.** نعم أو لا.
