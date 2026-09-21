---
helpId: wf.workflow-group-field-access.edit
lang: ar
title: صلاحيات حقول مجموعات سير العمل — إنشاء / تحرير
entity: HCMv3.Workflow.WorkflowGroupFieldAccess
surfaces:
  - angular/src/app/admin/workflow/workflow-group-detail/workflow-group-field-accesses/create-or-edit-workflow-group-field-access-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowGroupFieldAccesses.Create]
---

## Overview

ما إذا كان حقل في نموذج الطلب ظاهراً أو قابلاً للتعديل أو إلزامياً في خطوة معينة. يجب أن تكون الخطوة موجودة في السلسلة، ولا يُدرج الحقل إلا مرة واحدة لكل خطوة. لا يُقرأ حالياً: لا تطبّق نماذج الطلبات هذه السجلات.

## Step {#WorkflowGroupFieldAccess.Step}
<!-- alias: loc:Step, dom:WorkflowGroupFieldAccess_Step -->

**ما هو.** الخطوة التي تنطبق فيها القاعدة.

**مثال.** 2

**القواعد.** إلزامي. رقم صحيح.

## Field {#WorkflowGroupFieldAccess.Field}
<!-- alias: loc:Field, dom:WorkflowGroupFieldAccess_Field -->

**ما هو.** حقل النموذج.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## IsVisible {#WorkflowGroupFieldAccess.IsVisible}
<!-- alias: loc:IsVisible, dom:WorkflowGroupFieldAccess_IsVisible -->

**ما هو.** ما إذا كان الحقل ظاهراً.

**القواعد.** نعم أو لا.

## IsEditable {#WorkflowGroupFieldAccess.IsEditable}
<!-- alias: loc:IsEditable, dom:WorkflowGroupFieldAccess_IsEditable -->

**ما هو.** ما إذا كان يمكن تغيير الحقل.

**القواعد.** نعم أو لا.

## IsRequired {#WorkflowGroupFieldAccess.IsRequired}
<!-- alias: loc:IsRequired, dom:WorkflowGroupFieldAccess_IsRequired -->

**ما هو.** ما إذا كان يجب ملء الحقل.

**القواعد.** نعم أو لا.
