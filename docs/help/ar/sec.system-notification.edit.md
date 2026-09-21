---
helpId: sec.system-notification.edit
lang: ar
title: إشعارات النظام — إنشاء / تحرير
entity: HCMv3.SystemSecurity.SystemNotification
surfaces:
  - angular/src/app/admin/system-security/system-notifications/create-or-edit-system-notification-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.SystemNotifications.Create]
---

## Overview

تذكير يرسله النظام وفق جدول، ويُتعرَّف عليه **باسم الإشعار**: `BIRTHDAY_NOTIFICATION` و`ANNIVERSARY_NOTIFICATION` و`AdvancePayment_NOTIFICATION` و`LEAVERESUMPTION_NOTIFICATION` و`PUNCHDURINGLEAVE_NOTIFICATION` و`ANNUALLEAVEMAXLIMITREACHED_NOTIFICATION`. والاسم الذي لا يتعرّف عليه النظام لا يفعل شيئاً.

## NotificationName {#SystemNotification.NotificationName}
<!-- alias: loc:NotificationName, dom:SystemNotification_NotificationName -->

**ما هو.** أي تذكير هذا، بالاسم الذي يستخدمه النظام.

**مثال.** `LEAVERESUMPTION_NOTIFICATION`

**لماذا يهم.** يجب أن يطابق أحد الأسماء أعلاه تماماً.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## NotificationConfig {#SystemNotification.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:SystemNotification_NotificationConfigId -->

**ما هو.** إعداد الإشعار -- النموذج والمستلمون -- الذي يُرسل به التذكير.

**القواعد.** إلزامي. يُختار من قائمة إعدادات الإخطار.

## ReminderDays {#SystemNotification.ReminderDays}
<!-- alias: loc:ReminderDays, dom:SystemNotification_ReminderDays -->

**ما هو.** قبل كم يوماً يُرسل التذكير.

**مثال.** `3`

**لماذا يهم.** يستخدمه تذكير الدفعة المقدمة: فيُرسل قبل هذا العدد من الأيام من بداية الإجازة أو تاريخ الدفع، بحسب إعدادات الإشعارات.

**القواعد.** إلزامي. رقم صحيح.

## IsActive {#SystemNotification.IsActive}
<!-- alias: loc:IsActive, dom:SystemNotification_IsActive -->

**ما هو.** ما إذا كان التذكير يُرسل.

**لماذا يهم.** إذا لم يُؤشَّر عليه لا يُرسل التذكير.

**القواعد.** نعم أو لا.
