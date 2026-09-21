---
helpId: sec.notification-master.edit
lang: ar
title: الإشعارات الرئيسية — إنشاء / تحرير
entity: HCMv3.SystemSecurity.NotificationMaster
surfaces:
  - angular/src/app/admin/system-security/notification-masters/create-or-edit-notification-master-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.NotificationMasters.Create]
---

## Overview

قاعدة إشعار مرتبطة بنوع إشعار. ولكل نوع قاعدة واحدة.

لا تُقرأ حالياً: تعمل الإشعارات من إشعارات النظام وإعدادات الإشعارات، لا من هذه القائمة.

## NotificationType {#NotificationMaster.NotificationType}
<!-- alias: loc:NotificationType, dom:NotificationMaster_NotificationType -->

**ما هو.** نوع الإشعار.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## NotificationConfig {#NotificationMaster.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:NotificationMaster_NotificationConfigId -->

**ما هو.** إعداد الإشعار.

**القواعد.** إلزامي. يُختار من قائمة إعدادات الإخطار.

## Criterion {#NotificationMaster.Criterion}
<!-- alias: loc:Criterion, dom:NotificationMaster_Criterion -->

**ما هو.** شرط للإشعار.

**القواعد.** اختياري.

## IsEnabled {#NotificationMaster.IsEnabled}
<!-- alias: loc:IsEnabled, dom:NotificationMaster_IsEnabled -->

**ما هو.** ما إذا كانت القاعدة مفعّلة.

**القواعد.** نعم أو لا.
