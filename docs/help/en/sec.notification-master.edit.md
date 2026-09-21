---
helpId: sec.notification-master.edit
lang: en
title: Notification Masters — Create / Edit
entity: HCMv3.SystemSecurity.NotificationMaster
surfaces:
  - angular/src/app/admin/system-security/notification-masters/create-or-edit-notification-master-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.NotificationMasters.Create]
---

## Overview

A notification rule tied to a notification type. Each type can have one rule.

Not read at present: notifications are driven by System Notifications and notification configs, not by this list.

## NotificationType {#NotificationMaster.NotificationType}
<!-- alias: loc:NotificationType, dom:NotificationMaster_NotificationType -->

**What it is.** The notification type.

**Rules.** Required. Maximum 256 characters.

## NotificationConfig {#NotificationMaster.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:NotificationMaster_NotificationConfigId -->

**What it is.** The notification configuration.

**Rules.** Required. Chosen from Notification Config master data.

## Criterion {#NotificationMaster.Criterion}
<!-- alias: loc:Criterion, dom:NotificationMaster_Criterion -->

**What it is.** A condition for the notification.

**Rules.** Optional.

## IsEnabled {#NotificationMaster.IsEnabled}
<!-- alias: loc:IsEnabled, dom:NotificationMaster_IsEnabled -->

**What it is.** Whether the rule is on.

**Rules.** Yes or no.
