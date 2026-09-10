---
helpId: gmi.notification-config.edit
lang: en
title: Notification Configs — Create / Edit
entity: HCMv3.GeneralMasterInfo.NotificationConfig
surfaces:
  - angular/src/app/admin/general-master-info/notification-configs/create-or-edit-notification-config-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.NotificationConfigs.Create]
---

## Overview

Settings that decide whether HCMv3 sends an email for an event, and to whom. Document types, system notifications and scheduled notifications -- such as birthday and work-anniversary greetings -- each point to one of these. However many boxes are ticked, nobody receives the same email twice.

## Name {#NotificationConfig.Name}
<!-- alias: loc:Name, dom:NotificationConfig_Name -->

**What it is.** A name to recognise these settings by.

**Example.** `Birthday greetings`

**Rules.** Required. Maximum 256 characters.

## SendNotification {#NotificationConfig.SendNotification}
<!-- alias: loc:SendNotification, dom:NotificationConfig_SendNotification -->

**What it is.** Meant to switch in-app notifications on.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## SendEmail {#NotificationConfig.SendEmail}
<!-- alias: loc:SendEmail, dom:NotificationConfig_SendEmail -->

**What it is.** Whether an email is sent at all.

**Why it matters.** Scheduled notifications such as birthday greetings are sent only when this is ticked.

**Rules.** Yes or no.

## ToEmployee {#NotificationConfig.ToEmployee}
<!-- alias: loc:ToEmployee, dom:NotificationConfig_ToEmployee -->

**What it is.** Email the employee concerned.

**Why it matters.** Skipped when the employee is inactive.

**Rules.** Yes or no.

## ToRequestCreator {#NotificationConfig.ToRequestCreator}
<!-- alias: loc:ToRequestCreator, dom:NotificationConfig_ToRequestCreator -->

**What it is.** Email the person who raised the request.

**Rules.** Yes or no.

## ToManager {#NotificationConfig.ToManager}
<!-- alias: loc:ToManager, dom:NotificationConfig_ToManager -->

**What it is.** Email the employee's manager.

**Why it matters.** Sent only when both the employee and the manager are active.

**Rules.** Yes or no.

## SendToAll {#NotificationConfig.SendToAll}
<!-- alias: loc:SendToAll, dom:NotificationConfig_SendToAll -->

**What it is.** Email every active employee.

**Rules.** Yes or no.

## SendToAllApproved {#NotificationConfig.SendToAllApproved}
<!-- alias: loc:SendToAllApproved, dom:NotificationConfig_SendToAllApproved -->

**What it is.** For a workflow request, email everyone who approved it.

**Why it matters.** Approvers who are now inactive are skipped.

**Rules.** Yes or no.

## SendToApprover {#NotificationConfig.SendToApprover}
<!-- alias: loc:SendToApprover, dom:NotificationConfig_SendToApprover -->

**What it is.** Meant to email the current approver.

**Why it matters.** Not read at present.

**Rules.** Yes or no.
