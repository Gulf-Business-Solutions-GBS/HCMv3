---
helpId: gmi.personal-action.edit
lang: en
title: Personal Actions — Create / Edit
entity: HCMv3.GeneralMasterInfo.PersonalAction
surfaces:
  - angular/src/app/admin/general-master-info/personal-actions/create-or-edit-personal-action-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.PersonalActions.Create]
---

## Overview

The kinds of personal action recorded against employees -- hiring, transfer, promotion, separation and so on. Each employee action names one.

The requirement flags on this form are stored but not enforced at present: saving an employee action does not check them.

## ReqOrgAssignment {#PersonalAction.ReqOrgAssignment}
<!-- alias: loc:ReqOrgAssignment, dom:PersonalAction_ReqOrgAssignment -->

**What it is.** Meant to require a new organisation assignment with the action.

**Why it matters.** Not enforced at present.

**Rules.** Yes or no.

## ReqBasicPay {#PersonalAction.ReqBasicPay}
<!-- alias: loc:ReqBasicPay, dom:PersonalAction_ReqBasicPay -->

**What it is.** Meant to require a new basic pay record with the action.

**Why it matters.** Not enforced at present.

**Rules.** Yes or no.

## ReqContract {#PersonalAction.ReqContract}
<!-- alias: loc:ReqContract, dom:PersonalAction_ReqContract -->

**What it is.** Meant to require a new contract with the action.

**Why it matters.** Not enforced at present.

**Rules.** Yes or no.

## ReqManager {#PersonalAction.ReqManager}
<!-- alias: loc:ReqManager, dom:PersonalAction_ReqManager -->

**What it is.** Meant to require a new manager assignment with the action.

**Why it matters.** Not enforced at present.

**Rules.** Yes or no.

## ReqOnHold {#PersonalAction.ReqOnHold}
<!-- alias: loc:ReqOnHold, dom:PersonalAction_ReqOnHold -->

**What it is.** Meant to require an on-hold record with the action.

**Why it matters.** Not enforced at present.

**Rules.** Yes or no.

## ReqSeparation {#PersonalAction.ReqSeparation}
<!-- alias: loc:ReqSeparation, dom:PersonalAction_ReqSeparation -->

**What it is.** Meant to require a separation record with the action.

**Why it matters.** Not enforced at present.

**Rules.** Yes or no.

## ReqFnFPayment {#PersonalAction.ReqFnFPayment}
<!-- alias: loc:ReqFnFPayment, dom:PersonalAction_ReqFnFPayment -->

**What it is.** Meant to require a final settlement (full and final) payment with the action.

**Why it matters.** Not enforced at present.

**Rules.** Yes or no.

## IncludeInWorkflow {#PersonalAction.IncludeInWorkflow}
<!-- alias: loc:IncludeInWorkflow, dom:PersonalAction_IncludeInWorkflow -->

**What it is.** Meant to send actions of this type for approval.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## IsAttachmentRequired {#PersonalAction.IsAttachmentRequired}
<!-- alias: loc:IsAttachmentRequired, dom:PersonalAction_IsAttachmentRequired -->

**What it is.** Meant to require an attachment with the action.

**Why it matters.** Not enforced at present.

**Rules.** Yes or no.
