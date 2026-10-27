---
helpId: sec.system-notification.edit
lang: en
title: System Notifications — Create / Edit
entity: HCMv3.SystemSecurity.SystemNotification
surfaces:
  - angular/src/app/admin/system-security/system-notifications/create-or-edit-system-notification-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.SystemNotifications.Create]
---

## Overview

A reminder the system sends on a schedule, recognised by its **Notification Name**: `BIRTHDAY_NOTIFICATION`, `ANNIVERSARY_NOTIFICATION`, `AdvancePayment_NOTIFICATION`, `LEAVERESUMPTION_NOTIFICATION`, `PUNCHDURINGLEAVE_NOTIFICATION` and `ANNUALLEAVEMAXLIMITREACHED_NOTIFICATION`. A name the system does not recognise does nothing.

## NotificationName {#SystemNotification.NotificationName}
<!-- alias: loc:NotificationName, dom:SystemNotification_NotificationName -->

**What it is.** Which reminder this is, as the system names it.

**Example.** `LEAVERESUMPTION_NOTIFICATION`

**Why it matters.** Must match one of the names above exactly.

**Rules.** Required. Maximum 256 characters.

## NotificationConfig {#SystemNotification.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:SystemNotification_NotificationConfigId -->

**What it is.** The notification configuration -- template and recipients -- the reminder is sent with.

**Rules.** Required. Chosen from Notification Config master data.

## ReminderDays {#SystemNotification.ReminderDays}
<!-- alias: loc:ReminderDays, dom:SystemNotification_ReminderDays -->

**What it is.** How many days ahead the reminder is sent.

**Example.** `3`

**Why it matters.** Used by the advance-payment reminder: it goes out this many days before the leave start or payment date, as set in Notification Settings.

**Rules.** Required. Whole number.

## IsActive {#SystemNotification.IsActive}
<!-- alias: loc:IsActive, dom:SystemNotification_IsActive -->

**What it is.** Whether the reminder is sent.

**Why it matters.** Unticked, the reminder is not sent.

**Rules.** Yes or no.
