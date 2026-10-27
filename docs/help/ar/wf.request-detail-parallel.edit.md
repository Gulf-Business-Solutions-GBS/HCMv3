---
helpId: wf.request-detail-parallel.edit
lang: ar
title: التفاصيل المتوازية للطلبات — إنشاء / تحرير
entity: HCMv3.Workflow.RequestDetailParallel
surfaces:
  - angular/src/app/admin/workflow/request-detail-parallels/create-or-edit-request-detail-parallel-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.RequestDetailParallels.Create]
---

## Overview

معتمد إضافي لخطوة، يُحدَّد بالمسمى الوظيفي ودور سير العمل. ولا يمكن ترك دور سير العمل فارغاً. لا يُقرأ حالياً: يأتي المعتمدون من الخطوة نفسها فقط، فلا تضيف هذه السجلات أحداً.

## RequestDetails {#RequestDetailParallel.RequestDetailId}
<!-- alias: loc:RequestDetails, dom:RequestDetailParallel_RequestDetailId -->

**ما هو.** الخطوة التي يُضاف إليها هذا المعتمد.

**القواعد.** إلزامي. يُختار من قائمة تفاصيل الطلبات.

## JobTitle {#RequestDetailParallel.JobTitleId}
<!-- alias: loc:JobTitle, dom:RequestDetailParallel_JobTitleId -->

**ما هو.** المسمى الوظيفي للمعتمد الإضافي.

**القواعد.** اختياري. يُختار من قائمة المسميات الوظيفية.

## WorkflowRole {#RequestDetailParallel.WorkflowRoleId}
<!-- alias: loc:WorkflowRole, dom:RequestDetailParallel_WorkflowRoleId -->

**ما هو.** دور سير العمل للمعتمد الإضافي.

**القواعد.** إلزامي. يُختار من قائمة أدوار سير العمل.
