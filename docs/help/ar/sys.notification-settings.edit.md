---
helpId: sys.notification-settings.edit
lang: ar
title: Notification settings
entity: HCMv3.SystemSettings.ManageNotificationSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-notification-settings/create-or-edit-manage-notification-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageNotificationSettings]
---

## Overview

إعدادات الإشعارات.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## DisableNotificationWindow {#ManageNotificationSettings.DisableNotificationWindow}
<!-- alias: loc:DisableNotificationWindow, dom:NotificationSettings_DisableNotificationWindow -->

**ما هو.** ما إذا كانت نافذة الإشعارات مخفية.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AdvancePaymentReminderDateType {#ManageNotificationSettings.AdvancePaymentReminderDateType}
<!-- alias: loc:AdvancePaymentReminderDateType, dom:NotificationSettings_AdvancePaymentReminderDateType -->

**ما هو.** التاريخ الذي يُعدّ منه تذكير الدفعة المقدمة إلى الوراء.

**مثال.** تاريخ بداية الإجازة

**لماذا يهم.** يُرسل التذكير بالإجازة المدفوعة قبل عدد محدد من الأيام من تاريخ بداية الإجازة أو من تاريخ دفعها، بحسب الاختيار هنا.

**القواعد.** إلزامي. إحدى القيم: تاريخ بداية الإجازة، تاريخ دفع الإجازة.
