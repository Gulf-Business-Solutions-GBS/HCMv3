# Workflow feature comparison (legacy ESS + SAP SuccessFactors -> HCMv3)

Audit date: 2026-09-24. Source: the 7-page feedback document with 19 screenshots (legacy ESS workflow
tabs, then SAP SuccessFactors screens supplied for reference).

Every claim below was checked against the HCMv3 code, not inferred from the screenshots. Where the
document asks for something HCMv3 already does differently, the difference is stated rather than
silently counted as present.

## The brief

Two constraints set by the user, and they pull against each other on purpose:

- **All the legacy functionality must exist**, but better organised, reportable, and exportable to Excel.
  Not a copy of the legacy UI.
- **Do not copy SAP.** Take the relevant ideas only. "SAP is huge, not necessary we develop exactly same,
  I want to keep it simple but function rich."

So a SAP feature is only taken here when it answers a need the legacy list already raised, or when it
closes a gap that bites in production. Three SAP features are deliberately declined, with reasons.

## Part 1 -- legacy workflow tabs

Legend: **Have** = works today. **Partial** = the data or engine exists, the UI or a calculation does not.
**Missing** = not there.

| # | Feature (as described in the document) | HCMv3 today | Status |
|---|---|---|---|
| 1 | **Workflow Request** -- request number, current status, current step, current workflow role | All four render in the "Workflow Actions & Information" panel (`WorkflowActivityAppService.GetParticipants`). `CurrentWorkflowRole` shows `-` when the current step routes to a *manager* rather than a workflow role -- correct behaviour, not a gap | **Have** |
| 2 | **Request Log** -- photo, code, action by, action date, action time, action type, workflow step, action days, platform; exportable | `RequestLog` carries every one of those columns. It is rendered as the social-style thread the user asked for. But `ActionDays` is written as `0` everywhere, `Platform` is written as `Core` everywhere, and there is no grid view and no export | **Partial** |
| 3 | **Workflow Approver** -- current step approvers, one or several, who approved; "may be one approval is enough or all must approve" | The participants panel lists approvers with step, job title and approved/pending. `ApprovalRequire` (`Single` / `All` / `Parallel`) is configured per step *and* enforced by the engine (`WorkflowApprovalService`) | **Partial** -- the panel never states which rule is in force |
| 4 | **Delegated approvers** -- delegatee receives the request and can see who delegated to them | `WorkflowApproverResolver` substitutes the stand-in into the candidate list, so the request reaches their inbox. The panel's Delegates tab carries person, delegated-by, relation and the date range | **Have** |
| 5 | **Workflow Remark**, renamed to **Cc / Get Feedback** -- approver asks one or more people for an opinion; approval stays with the approver; feedback is optional and must not block | `WorkflowRemark` exists, the feed shows it as a `FeedbackRequested` event, and `RequestAccess.IsFeedbackRecipient` grants view and comment rights. But **no UI creates one**, and a recipient **never sees the request in their inbox** -- the inbox query reads `WorkFlowApprover` rows only | **Missing** in practice |
| 6 | **Workflow Read Log** -- someone opened the request but took no action | Written by the engine, gated per step by `EnableReadLog`, surfaced in the panel | **Have** -- `WFReadLog.User` is a raw string rather than an employee link |
| 7 | **Attachments** -- multiple files, configurable per entity, and a mandatory flag (e.g. sick leave report) | `AppAttachment` and `AttachmentManager` exist but are wired into **MyLeave only**. No per-transaction-type configuration, no mandatory rule, and the approver cannot see attachments | **Missing** |
| 8 | **Comments configurable** -- mandatory or blank | `RequestDetail.IsCommentRequire` per step, enforced in `WorkflowApprovalService` (throws `ACommentIsRequiredToApproveAtThisWorkflowStep`) | **Have** -- the textarea is not marked required, and there is no equivalent for the creator's submission comment |

**Two cross-cutting asks are unmet everywhere.** No workflow screen is reportable as a grid, and nothing
exports to Excel. That is one shared piece of work, not eight separate ones.

## Part 2 -- SAP screens: take, trim, or decline

| SAP feature | Call | Reasoning |
|---|---|---|
| **Pending Workflows**, six tabs with counts | **Take, trimmed to four** | *Awaiting me* / *I sent back* / *I approved (still running)* / *Asked for my feedback*. "Requests Reassigned to Others" and "Requests for Information" split hairs the user will not use |
| **Bulk approve** -- select rows, one Approve button | **Take as-is** | High value, low complexity. Must skip any row whose step requires a comment, and report a per-row outcome rather than failing the batch |
| **Admin: Manage Workflow Requests** -- filters, grid, Stalled For Days | **Take** | This is the concrete answer to "reportable and exportable" for administrators. Stalled days is cheap: now minus the last action date |
| **Take Action** menu: Lock Down, Add Another Approver, Change Approvers, Remove Approvers, Route Request, Decline | **Take three of six** | Change Approver, Add Approver, Decline (admin force-close). Lock Down and Route Request add concepts without adding outcomes. Each action must write a `RequestLog` row or the audit trail lies |
| **Edit Transaction**: No Edit / Edit with Route Change / Edit without Route Change / Edit Attachment Only | **Take as two checkboxes** | "Approver may edit" plus "restart approvals after edit" produces the same three useful outcomes without a four-value enum nobody reads |
| **Workflow Participants** -- numbered steps, avatars, parallel approvers on one row | **Already have it** | Only needs the single/all label from item 3 |
| **"This workflow request is complete" banner**, Activity rail, "on behalf of" attribution | **Take the banner; "on behalf of" needs a column** | The rail exists. The banner is cheap and clear. "Approved by X on behalf of Y" needs `RequestLog.OnBehalfOfId`, worth adding once delegation is in real use |
| **Delegate Request** dialog -- ad-hoc, per request | **Decline** | Standing delegation already substitutes approvers, and Cc / Get Feedback covers "I want someone else's input". A third way to hand a request over is precisely the over-complication to avoid |
| **Step config extras**: Remind In Days, Days until Auto Approval, No Approver Behaviour, Alternate Workflow, Respect Permission, Contributors, Cc Role | **Take two** | `ReminderDays` already exists. Take **"no approver -> skip step"** (today a step whose role resolves to nobody stalls silently and forever) and **Days until Auto Approval**. Alternate Workflow, Respect Permission and Contributors are configuration surface without a matching need |

## Part 3 -- waves

**Wave 1 -- finish what is half-built.** All of it runs on data that already exists: compute `ActionDays`,
record `Platform` honestly (ESS / Mobile / Core), state the single-vs-all rule in the approvers panel, mark
required comment boxes, add the completion banner. Plus the shared grid-and-export view, applied to the
request log first. Fix the approver-row deletion defect here too (see below).

**Wave 2 -- Cc / Get Feedback.** The one genuinely missing legacy feature, and the one the document
describes in most detail. It also forces the inbox to stop being "approver rows only", which Wave 3 needs.

**Wave 3 -- Approvals screen: tabs and bulk approve.** Depends on Wave 2's inbox change.

**Wave 4 -- Admin: Manage Workflow Requests**, with Stalled For Days, export, and the three Take Actions.

**Wave 5 -- Attachments** across all request types, with per-type required/optional configuration.

### Two standing items

**The approver-row deletion defect.** `WorkflowApprovalService.AssignApproversForStepAsync` deletes every
`WorkFlowApprover` row on each step change, so past approvers survive only in the request log.
`RequestAccess.IsPastApprover` already works around it to keep the conversation visible. Several features
above get harder while it stands; it belongs in Wave 1.

**The ESS layout sweep.** The Exit Permit request screen is the agreed reference layout. The other twenty
self-service screens have not been brought onto it yet. Doing that before Wave 2 means new features land on
one layout rather than twenty-one.
