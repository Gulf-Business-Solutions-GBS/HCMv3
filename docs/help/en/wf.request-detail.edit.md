---
helpId: wf.request-detail.edit
lang: en
title: Request Details — Create / Edit
entity: HCMv3.Workflow.RequestDetail
surfaces:
  - angular/src/app/admin/workflow/request-details/create-or-edit-request-detail-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.RequestDetails.Create]
---

## Overview

One step of an approval chain: who approves it, whether one or all of them must, what they may do, and when the request is chased or escalated. Steps are numbered from 1 and the request moves up one step at each approval; approving the last step approves the request. Reminder days must be less than escalate-after days.

## RequestMaster {#RequestDetail.RequestMasterId}
<!-- alias: loc:RequestMaster, dom:RequestDetail_RequestMasterName -->

**What it is.** The approval chain this step belongs to.

**Rules.** Optional. Chosen from Request Master master data.

## Step {#RequestDetail.Step}
<!-- alias: loc:Step, dom:RequestDetail_Step -->

**What it is.** The step's position in the chain, from 1.

**Example.** 2

**Rules.** Required. Whole number.

## WorkflowRole {#RequestDetail.WorkflowRoleId}
<!-- alias: loc:WorkflowRole, dom:RequestDetail_WorkflowRoleId -->

**What it is.** The workflow role whose holders approve this step.

**Example.** Payroll Officer

**Why it matters.** Used only when Approver Person is Person: every employee holding the role becomes an approver, narrowed by Workflow Role Group. For the manager options this is not used.

**Rules.** Optional. Chosen from Workflow Role master data.

## ApprovalRequire {#RequestDetail.ApprovalRequire}
<!-- alias: loc:ApprovalRequire, dom:RequestDetail_ApprovalRequire -->

**What it is.** Whether one approver is enough or all must approve.

**Why it matters.** Single: the first approver to approve completes the step. Parallel: the step completes only when every approver on it has approved.

**Rules.** Required. One of: Single, Parallel.

## WorkflowRoleGroup {#RequestDetail.WorkflowRoleGroup}
<!-- alias: loc:WorkflowRoleGroup, dom:RequestDetail_WorkflowRoleGroup -->

**What it is.** Limits the role's holders to the requester's part of the organisation.

**Why it matters.** Department keeps holders in the requester's current organisation unit, Branch those at the same work location, Cost Center those on the same cost centre. None and Company keep everyone. If nobody matches, all holders are kept rather than none.

**Rules.** Required. One of: None, Company, Department, Branch, CostCenter.

## OrganizationUnit {#RequestDetail.WorkflowRoleOrgId}
<!-- alias: loc:OrgLevel, dom:RequestDetail_WorkflowRoleOrgId -->

**What it is.** An organisation unit level for the role.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Organization Unit Type master data.

## Person {#RequestDetail.Person}
<!-- alias: loc:Person, dom:RequestDetail_Person -->

**What it is.** Whose request the step applies to.

**Why it matters.** Not read at present.

**Rules.** Required. One of: None, Themselves, Department, SelfSubordinates.

## ApproverPerson {#RequestDetail.ApproverPerson}
<!-- alias: loc:ApproverPerson, dom:RequestDetail_ApproverPerson -->

**What it is.** Who approves: role holders, or a manager above the requester.

**Example.** Person Manager

**Why it matters.** Person uses the workflow role. Person Manager is the requester's direct manager, and Manager Level 2 to 7 climb that many levels up the reporting line. If the line runs out before that level, or the option is None, the chain's default approver is used.

**Rules.** Required. One of: None, Person, PersonManager, ManagerLevel2, ManagerLevel3, ManagerLevel4, ManagerLevel5, ManagerLevel6, ManagerLevel7.

## EscalateAfterDays {#RequestDetail.EscalateAfterDays}
<!-- alias: loc:EscalateAfterDays, dom:RequestDetail_EscalateAfterDays -->

**What it is.** How many days without action before the request escalates.

**Example.** 5

**Why it matters.** Counted from the request's last logged action. Used only when Should Escalate is ticked; 0 turns escalation off.

**Rules.** Optional. Whole number.

## EscalateWorkflowRole {#RequestDetail.EscalateWorkflowRoleId}
<!-- alias: loc:EscalateWorkflowRole, dom:RequestDetail_EscalateWorkflowRoleId -->

**What it is.** The workflow role a stalled request is handed to.

**Why it matters.** Used when Escalate Next Level is not ticked: the request stays on the same step and its approvers are replaced by this role's holders, leaving out anyone already on it.

**Rules.** Optional. Chosen from Workflow Role master data.

## EscalateWorkflowRoleGroup {#RequestDetail.EscalateWorkflowRoleGroup}
<!-- alias: loc:EscalateWorkflowRoleGroup, dom:RequestDetail_EscalateWorkflowRoleGroup -->

**What it is.** Limits the escalation role's holders to the requester's part of the organisation.

**Why it matters.** Works the same way as Workflow Role Group.

**Rules.** Required. One of: None, Company, Department, Branch, CostCenter.

## EscalateWorkflowRoleOrg {#RequestDetail.EscalateWorkflowRoleOrgId}
<!-- alias: loc:EscalateWorkflowRoleOrg, dom:RequestDetail_EscalateWorkflowRoleOrgId -->

**What it is.** An organisation unit level for the escalation role.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Organization Unit Type master data.

## ReminderDays {#RequestDetail.ReminderDays}
<!-- alias: loc:ReminderDays, dom:RequestDetail_ReminderDays -->

**What it is.** How often pending approvers are reminded, in days.

**Example.** 2 -- reminders on days 2, 4, 6

**Why it matters.** A reminder goes to the approvers who have not yet acted every time the days since the last action reach a multiple of this number. 0 sends no reminders.

**Rules.** Optional. Whole number.

## OnSubmit {#RequestDetail.SubmitNotificationConfigId}
<!-- alias: loc:OnSubmit, dom:RequestDetail_SubmitNotificationConfigId -->

**What it is.** Who is told when the request arrives at this step.

**Why it matters.** Each action is set separately, so the people told about a rejection need not be the people told about an approval. Left empty, nothing is sent for that action at all.

**What it sends.** The chosen configuration decides both who receives it and how: the in-app notification, the e-mail, or both. A configuration with no e-mail template attached notifies inside the system only.

**Rules.** Optional. Chosen from Notification Config master data.

## OnApprove {#RequestDetail.NotificationConfigId}
<!-- alias: loc:OnApprove, dom:RequestDetail_NotificationConfigId -->

**What it is.** Who is told when this step approves.

**Why it matters.** Each action is set separately, so the people told about a rejection need not be the people told about an approval. Left empty, nothing is sent for that action at all.

**What it sends.** The chosen configuration decides both who receives it and how: the in-app notification, the e-mail, or both. A configuration with no e-mail template attached notifies inside the system only.

**Rules.** Optional. Chosen from Notification Config master data.

## OnReject {#RequestDetail.RejectNotificationConfigId}
<!-- alias: loc:OnReject, dom:RequestDetail_RejectNotificationConfigId -->

**What it is.** Who is told when this step rejects.

**Why it matters.** Each action is set separately, so the people told about a rejection need not be the people told about an approval. Left empty, nothing is sent for that action at all.

**What it sends.** The chosen configuration decides both who receives it and how: the in-app notification, the e-mail, or both. A configuration with no e-mail template attached notifies inside the system only.

**Rules.** Optional. Chosen from Notification Config master data.

## OnSendBack {#RequestDetail.SendBackNotificationConfigId}
<!-- alias: loc:OnSendBack, dom:RequestDetail_SendBackNotificationConfigId -->

**What it is.** Who is told when this step sends the request back.

**Why it matters.** Each action is set separately, so the people told about a rejection need not be the people told about an approval. Left empty, nothing is sent for that action at all.

**What it sends.** The chosen configuration decides both who receives it and how: the in-app notification, the e-mail, or both. A configuration with no e-mail template attached notifies inside the system only.

**Rules.** Optional. Chosen from Notification Config master data.

## OnRemark {#RequestDetail.RemarksNotificationConfigId}
<!-- alias: loc:OnRemark, dom:RequestDetail_RemarksNotificationConfigId -->

**What it is.** Who is told when somebody comments on the request.

**Why it matters.** Each action is set separately, so the people told about a rejection need not be the people told about an approval. Left empty, nothing is sent for that action at all.

**What it sends.** The chosen configuration decides both who receives it and how: the in-app notification, the e-mail, or both. A configuration with no e-mail template attached notifies inside the system only.

**Rules.** Optional. Chosen from Notification Config master data.

## OnReassign {#RequestDetail.AssignNotificationConfigId}
<!-- alias: loc:OnReassign, dom:RequestDetail_AssignNotificationConfigId -->

**What it is.** Who is told when the request is handed to somebody else.

**Why it matters.** Each action is set separately, so the people told about a rejection need not be the people told about an approval. Left empty, nothing is sent for that action at all.

**What it sends.** The chosen configuration decides both who receives it and how: the in-app notification, the e-mail, or both. A configuration with no e-mail template attached notifies inside the system only.

**Rules.** Optional. Chosen from Notification Config master data.

## OnReminder {#RequestDetail.ReminderNotificationConfigId}
<!-- alias: loc:OnReminder, dom:RequestDetail_ReminderNotificationConfigId -->

**What it is.** Who is told while the request is still waiting here.

**Why it matters.** Each action is set separately, so the people told about a rejection need not be the people told about an approval. Left empty, nothing is sent for that action at all.

**What it sends.** The chosen configuration decides both who receives it and how: the in-app notification, the e-mail, or both. A configuration with no e-mail template attached notifies inside the system only.

**Rules.** Optional. Chosen from Notification Config master data.

## ShowPayment {#RequestDetail.ShowPayment}
<!-- alias: loc:ShowPayment, dom:RequestDetail_ShowPayment -->

**What it is.** Whether the approver sees payment details.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## CanSendBack {#RequestDetail.CanSendBack}
<!-- alias: loc:CanSendBack, dom:RequestDetail_CanSendBack -->

**What it is.** Whether approvers at this step may send the request back.

**Why it matters.** Sending back returns the request to its creator as a draft at step 1, where it can be changed and submitted again. When unticked the option is hidden and refused.

**Rules.** Yes or no.

## CanReject {#RequestDetail.CanReject}
<!-- alias: loc:CanReject, dom:RequestDetail_CanReject -->

**What it is.** Whether approvers at this step may reject the request.

**Why it matters.** A rejection ends the request. When unticked the option is hidden and refused.

**Rules.** Yes or no.

## ShouldEscalate {#RequestDetail.ShouldEscalate}
<!-- alias: loc:ShouldEscalate, dom:RequestDetail_ShouldEscalate -->

**What it is.** Whether a stalled request at this step escalates.

**Why it matters.** Needs Escalate After Days above 0.

**Rules.** Yes or no.

## EscalateNextLevel {#RequestDetail.EscalateNextLevel}
<!-- alias: loc:EscalateNextLevel, dom:RequestDetail_EscalateNextLevel -->

**What it is.** Whether escalation moves the request on to the next step.

**Why it matters.** When ticked, a stalled request moves to the next step as if approved, but the transaction itself is not applied -- only a person's approval of the last step does that. A request on its last step is left where it is. When unticked, the escalation role takes over the same step.

**Rules.** Yes or no.

## EnableReadLog {#RequestDetail.EnableReadLog}
<!-- alias: loc:EnableReadLog, dom:RequestDetail_EnableReadLog -->

**What it is.** Whether opening the request at this step is recorded.

**Why it matters.** The first time each person opens it at this step, a read entry is added to the read log and the activity feed.

**Rules.** Yes or no.

## IsCommentRequire {#RequestDetail.IsCommentRequire}
<!-- alias: loc:IsCommentRequire, dom:RequestDetail_IsCommentRequire -->

**What it is.** Whether approvers must write a comment to approve.

**Why it matters.** An approval without a comment is refused.

**Rules.** Yes or no.

## SignatoryStep {#RequestDetail.SignatoryStep}
<!-- alias: loc:SignatoryStep, dom:RequestDetail_SignatoryStep -->

**What it is.** Whether this is a signatory step, needing someone who has not signed before.

**Why it matters.** Anyone who approved an earlier signatory step of the same request is left out here, so one person cannot sign twice. If that leaves nobody, the chain's default approver is used.

**Rules.** Yes or no.
