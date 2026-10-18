---
helpId: wf.workflow-group-detail.edit
lang: ar
title: تفاصيل مجموعات سير العمل — إنشاء / تحرير
entity: HCMv3.Workflow.WorkflowGroupDetail
surfaces:
  - angular/src/app/admin/workflow/workflow-group-detail/workflow-group-details/create-or-edit-workflow-group-detail-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowGroupDetails.Create]
---

## Overview

السلسلة التي تستخدمها مجموعة سير عمل لنوع واحد من الطلبات. داخل المجموعة يُختار السجل الأكثر تحديداً: النوع مع النوع الفرعي والنوع الفرعي المخصص، ثم النوع مع النوع الفرعي، ثم سجل النوع وحده. وإن لم يوجد تطابق في المجموعة، تُستخدم السلسلة الافتراضية لنوع المعاملة. ولا يمكن إسناد نوع طلب إلا إلى سلسلة واحدة في كل مجموعة.

## RequestMaster {#WorkflowGroupDetail.RequestMasterId}
<!-- alias: loc:RequestMaster, dom:WorkflowGroupDetail_RequestMasterId -->

**ما هو.** السلسلة المستخدمة.

**القواعد.** إلزامي. يُختار من قائمة الطلبات الأساسية.

## RequestType {#WorkflowGroupDetail.RequestTypeName}
<!-- alias: loc:RequestType, dom:WorkflowGroupDetail_RequestTypeName -->

**ما هو.** نوع الطلب.

**مثال.** طلب إجازة

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## RequestSubType {#WorkflowGroupDetail.RequestSubTypeId}
<!-- alias: loc:RequestSubType, dom:WorkflowGroupDetail_RequestSubTypeId -->

**ما هو.** يحصر السجل في نوع فرعي واحد، مثل نوع إجازة واحد.

**مثال.** الإجازة السنوية

**القواعد.** اختياري. يُختار من قائمة.

## CustomSubType {#WorkflowGroupDetail.CustomSubTypeId}
<!-- alias: loc:CustomSubType, dom:WorkflowGroupDetail_CustomSubTypeId -->

**ما هو.** يحصر السجل أكثر.

**لماذا يهم.** تُوجَّه الطلبات حالياً دون نوع فرعي مخصص، فلا يضيّق هذا شيئاً: السجل الذي فيه نوع فرعي يُطابَق على النوع الفرعي وحده، والسجل الذي فيه هذا الحقل وحده لا يُختار أبداً.

**القواعد.** اختياري. يُختار من قائمة.

## RequestActionAppliesTo {#WorkflowGroupDetail.RequestAction}
<!-- alias: loc:RequestActionAppliesTo, dom:WorkflowGroupDetail_RequestAction -->

**ما هو.** إجراء الطلب الذي ينطبق عليه هذا السطر: طلب جديد، أو تعديل، أو إلغاء، أو عودة من إجازة.

**مثال.** إلغاء

**لماذا يهم.** يتيح لمجموعة سير عمل واحدة أن توجّه مثلاً طلبات إلغاء الإجازة إلى سلسلة أقصر من الطلبات الجديدة. والسطر الذي يسمّي إجراء الطلب نفسه هو المعتمد؛ ولا تُستخدم الأسطر المتروكة على **جميع الإجراءات** إلا إذا لم يسمِّ أي سطر ذلك الإجراء.

**القواعد.** اختياري. إحدى القيم: جميع الإجراءات.
