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

A notification rule: "tell these people when a record of this kind matches this condition". For example, warn the employee and their manager when a passport expires within 30 days.

Enabled rules run with the other daily notifications, every four hours. Each run finds the records of the chosen type and sub type that match the criterion today, and sends each one to the people in the notification config, in the app (the bell) and/or by email. **Each record is notified once by each rule**: after that it is logged and never sent again by the same rule. A second rule on the same records sends its own notice. A rule that cannot run, for example because its criterion no longer fits its type, is skipped and logged, and the other rules still run.

**Before you enable a rule, press Preview.** It shows how many records match today and the first ten of them, with the date each one is about. On its first run a new rule notifies about **every** matching record that has not been notified yet, which for a broad rule can be hundreds of e-mails. If the existing records should not be notified, save the rule, press **Preview**, then **Mark these as sent**: the records matching today are logged as done without anyone being told, and only records that start matching later are sent. Mark these as sent uses the rule as last saved, so save any change first. You are asked to confirm.

**Sent History**, at the top of the list or on a rule's actions, shows what has been logged: the day, the notification, the record, how many people it went to, and whether it was **Sent** or **Marked only** (marked by Mark these as sent, or matched when the configuration found nobody to send to). The button at the top shows every rule and the fixed daily notifications too.

A rule whose type is shown as **Needs re-entry** was saved before notification types became a fixed list; open it and choose a type, or it will not run.

:::admin
A record matched when the notification config addresses nobody that day is still logged, as the old system did, so it is not re-checked every four hours. A record the criterion cannot evaluate (bad data) is not logged and is tried again on the next run. "Today" is the server date at the time of the run. Users can switch each notification type off for themselves under Notification settings.
:::

## NotificationType {#NotificationMaster.NotificationType}
<!-- alias: loc:NotificationType, dom:NotificationMaster_NotificationType -->

**What it is.** The kind of record the rule watches: **Employee Document**, **Company Document**, **Leave**, **Disciplinary Action**, **Employee** or **Loan**.

**Example.** Employee Document

**Why it matters.** The type decides the fields the criterion can use and the date each notice is about: the expiry date for documents, the start date for leave, the disciplinary date, the loan end date. The **Employee** type has no date of its own, so its criterion names the date it cares about (probation end, birth date, joining date). Leave only sees approved leave. Company documents belong to no employee, so "to employee" and "to manager" in the notification config find nobody; notification groups and "send to all" still work.

Next to it, the **sub type** narrows the rule to one kind of record, and its label changes with the type: Document Type for documents, Leave Type for leave, Violation Type for disciplinary actions and Loan Type for loans. **All** takes every kind. The Employee type has no sub type.

**Rules.** Required. Maximum 256 characters.

## NotificationConfig {#NotificationMaster.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:NotificationMaster_NotificationConfigId -->

**What it is.** Who receives the notice and how, set up under **Notification Configs**: the employee, their manager, notification groups or everybody, by in-app notice and/or email.

**Example.** "Employee and manager, in-app and email"

**Why it matters.** Email is sent only when the config has an email template; without one the notice goes to the bell only. Two rules with the same type, sub type, config and criterion are refused as duplicates.

**Rules.** Required. Chosen from Notification Config master data.

## Criterion {#NotificationMaster.Criterion}
<!-- alias: loc:Criterion, dom:NotificationMaster_Criterion -->

**What it is.** The condition a record must meet, written as a formula with the business rule editor. It offers the fields of the chosen type, the employee's fields, and functions such as **DaysUntil**, which gives the days from today to a date: negative once the date has passed, 0 on the day itself, and blank when the date is empty.

**Example.** `DaysUntil(Document.ExpiryDate) <= 30 AND DaysUntil(Document.ExpiryDate) >= 0` (expires within the next 30 days).

**Why it matters.** Leave it empty to notify about every record of the type and sub type. A record with an empty date never matches a DaysUntil condition. The Save and Preview buttons stay disabled while the formula is not valid. Criteria from the old system's filter editor cannot be reused: write them again here.

**Rules.** Optional.

## IsEnabled {#NotificationMaster.IsEnabled}
<!-- alias: loc:IsEnabled, dom:NotificationMaster_IsEnabled -->

**What it is.** Whether the rule runs.

**Example.** Off while you are still testing the criterion with Preview.

**Why it matters.** Only enabled rules run. Preview and Mark these as sent work whether the rule is on or off, so you can prepare a rule fully before switching it on.

**Rules.** Yes or no.
