---
helpId: sys.notification-settings.edit
lang: en
title: Notification settings
entity: HCMv3.SystemSettings.ManageNotificationSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-notification-settings/create-or-edit-manage-notification-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageNotificationSettings]
---

## Overview

Settings for notifications.

There is one settings record per company; it cannot be deleted.

## DisableNotificationWindow {#ManageNotificationSettings.DisableNotificationWindow}
<!-- alias: loc:DisableNotificationWindow, dom:NotificationSettings_DisableNotificationWindow -->

**What it is.** Whether the notification window is hidden.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AdvancePaymentReminderDateType {#ManageNotificationSettings.AdvancePaymentReminderDateType}
<!-- alias: loc:AdvancePaymentReminderDateType, dom:NotificationSettings_AdvancePaymentReminderDateType -->

**What it is.** Which date the advance-payment reminder counts back from.

**Example.** Leave Start Date

**Why it matters.** The reminder about paid leave is sent a set number of days before either the leave's start date or its payment date, as chosen here.

**Rules.** Required. One of: Leave Start Date, Leave Payment Date.
