---
helpId: gmi.notification-group.edit
lang: en
title: Notification Groups — Create / Edit
entity: HCMv3.GeneralMasterInfo.NotificationGroup
surfaces:
  - angular/src/app/admin/general-master-info/notification-groups/create-or-edit-notification-group-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.NotificationGroups.Create]
---

## Overview

Groups linked to a notification configuration. Not used at present: sending notifications to groups was not carried over into HCMv3.

## NotificationConfig {#NotificationGroup.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:NotificationGroup_NotificationConfigId -->

**What it is.** The notification configuration the group belongs to.

**Rules.** Optional. Chosen from Notification Config master data.

## NotifyRoleHolders {#NotificationGroup.WorkflowRoleId}
<!-- alias: loc:NotifyRoleHolders, dom:NotificationGroup_WorkflowRoleId -->

**What it is.** A workflow role whose holders are told when this notification fires.

**Example.** HR Business Partner

**Why it matters.** Everyone holding the role on the day is added to the recipients. A group with no role adds nobody. When the configuration has **Send To Approver** ticked, groups are not used at all and only the approvers are told.

**Rules.** Optional. Chosen from Workflow Role master data.

## NarrowToRequesterOrgScope {#NotificationGroup.WorkflowRoleGroup}
<!-- alias: loc:NarrowToRequesterOrgScope, dom:NotificationGroup_WorkflowRoleGroup -->

**What it is.** Limits the role holders to those in the same part of the organisation as the requester.

**Example.** Department

**Why it matters.** With Department, for example, only role holders in the requester's department are told. None tells every holder. If the limit leaves nobody, every holder of the role is told instead, so the notification is never lost.

**Rules.** Required. One of: None, Company, Department, Branch, CostCenter.
