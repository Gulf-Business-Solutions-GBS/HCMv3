---
helpId: sec.email-notification-log.edit
lang: en
title: Email Notification Logs — Create / Edit
entity: HCMv3.SystemSecurity.EmailNotificationLog
surfaces:
  - angular/src/app/admin/system-security/email-notification-logs/create-or-edit-email-notification-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.EmailNotificationLogs.Create]
---

## Overview

One email the system tried to send, and whether it went. Written by the system as it runs; there is normally no reason to add or change entries by hand.

## Date {#EmailNotificationLog.Date}
<!-- alias: loc:Date, dom:EmailNotificationLog_Date -->

**What it is.** When it was sent.

**Rules.** Optional. Date value.

## Email {#EmailNotificationLog.Email}
<!-- alias: loc:Email, dom:EmailNotificationLog_Email -->

**What it is.** The address it was sent to.

**Rules.** Required. Maximum 256 characters.

## Source {#EmailNotificationLog.Source}
<!-- alias: loc:Source, dom:EmailNotificationLog_Source -->

**What it is.** What sent it.

**Example.** Punch notification

**Rules.** Optional. Maximum 256 characters.

## Subject {#EmailNotificationLog.Subject}
<!-- alias: loc:Subject, dom:EmailNotificationLog_Subject -->

**What it is.** The subject.

**Rules.** Optional. Maximum 500 characters.

## IsSuccessful {#EmailNotificationLog.IsSuccessful}
<!-- alias: loc:IsSuccessful, dom:EmailNotificationLog_IsSuccessful -->

**What it is.** Whether it was sent.

**Rules.** Yes or no.

## Body {#EmailNotificationLog.Body}
<!-- alias: loc:Body, dom:EmailNotificationLog_Body -->

**What it is.** The body.

**Rules.** Optional.

## Exception {#EmailNotificationLog.Exception}
<!-- alias: loc:Exception, dom:EmailNotificationLog_Exception -->

**What it is.** The error, if it failed.

**Rules.** Optional.
