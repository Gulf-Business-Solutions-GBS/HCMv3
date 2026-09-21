---
helpId: sec.email-notification-log.edit
lang: ar
title: سجلات إشعارات البريد الإلكتروني — إنشاء / تحرير
entity: HCMv3.SystemSecurity.EmailNotificationLog
surfaces:
  - angular/src/app/admin/system-security/email-notification-logs/create-or-edit-email-notification-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.EmailNotificationLogs.Create]
---

## Overview

رسالة بريد واحدة حاول النظام إرسالها، وما إذا أُرسلت. يكتبه النظام أثناء عمله، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## Date {#EmailNotificationLog.Date}
<!-- alias: loc:Date, dom:EmailNotificationLog_Date -->

**ما هو.** متى أُرسلت.

**القواعد.** اختياري. تاريخ.

## Email {#EmailNotificationLog.Email}
<!-- alias: loc:Email, dom:EmailNotificationLog_Email -->

**ما هو.** العنوان الذي أُرسلت إليه.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## Source {#EmailNotificationLog.Source}
<!-- alias: loc:Source, dom:EmailNotificationLog_Source -->

**ما هو.** ما الذي أرسلها.

**مثال.** إشعار البصمة

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Subject {#EmailNotificationLog.Subject}
<!-- alias: loc:Subject, dom:EmailNotificationLog_Subject -->

**ما هو.** الموضوع.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## IsSuccessful {#EmailNotificationLog.IsSuccessful}
<!-- alias: loc:IsSuccessful, dom:EmailNotificationLog_IsSuccessful -->

**ما هو.** ما إذا أُرسلت.

**القواعد.** نعم أو لا.

## Body {#EmailNotificationLog.Body}
<!-- alias: loc:Body, dom:EmailNotificationLog_Body -->

**ما هو.** النص.

**القواعد.** اختياري.

## Exception {#EmailNotificationLog.Exception}
<!-- alias: loc:Exception, dom:EmailNotificationLog_Exception -->

**ما هو.** الخطأ، إن فشلت.

**القواعد.** اختياري.
