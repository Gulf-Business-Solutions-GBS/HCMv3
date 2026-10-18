---
helpId: gmi.personal-action-reason.edit
lang: en
title: Personal Action Reasons — Create / Edit
entity: HCMv3.GeneralMasterInfo.PersonalActionReason
surfaces:
  - angular/src/app/admin/general-master-info/personal-action-reasons/create-or-edit-personal-action-reason-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.PersonalActionReasons.Create]
---

## Overview

The reasons that can be given for a personal action -- why an employee was transferred, promoted and so on. Each employee action records its action and a reason.

## PersonalAction {#PersonalActionReason.PersonalActionId}
<!-- alias: loc:PersonalAction, dom:PersonalActionReason_PersonalActionId -->

**What it is.** The personal action this reason belongs to.

**Rules.** Optional. Chosen from Personal Action master data.

## NotificationConfig {#PersonalActionReason.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:PersonalActionReason_NotificationConfigId -->

**What it is.** The email sent when an employee action with this reason is approved.

**Example.** Promotion Letter

**Why it matters.** On approval, the configuration's email template is sent about the employee, to whoever the configuration addresses. Only email is sent. Nothing is sent when it is blank or the configuration has no email template, or when an amendment of an action is approved. A failed send never undoes the approval.

**Rules.** Optional. Chosen from Notification Config master data.
