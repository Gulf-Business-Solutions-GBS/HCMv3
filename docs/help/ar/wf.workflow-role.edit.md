---
helpId: wf.workflow-role.edit
lang: ar
title: أدوار سير العمل — إنشاء / تحرير
entity: HCMv3.Workflow.WorkflowRole
surfaces:
  - angular/src/app/admin/workflow/workflow-roles/create-or-edit-workflow-role-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowRoles.Create]
---

## Overview

دور معتمد، مثل مسؤول الرواتب. يُمنح الموظفون الأدوار في سجلاتهم، والخطوة التي يكون فيها الشخص المعتمد "شخص" تذهب إلى كل من يشغل دورها.

## WorkflowRoleGroup {#WorkflowRole.WorkflowRoleGroup}
<!-- alias: loc:WorkflowRoleGroup, dom:WorkflowRole_WorkflowRoleGroup -->

**ما هو.** نطاق تنظيمي افتراضي للدور.

**لماذا يهم.** لا يُقرأ حالياً. النطاق الذي يضيّق المعتمدين هو الموجود في كل خطوة.

**القواعد.** إلزامي. إحدى القيم: لا شيء، الشركة، الإدارة، الفرع، مركز التكلفة.

## OrganizationUnit {#WorkflowRole.WorkflowRoleOrgId}
<!-- alias: loc:OrganizationUnit, dom:WorkflowRole_WorkflowRoleOrgId -->

**ما هو.** مستوى وحدة تنظيمية للدور.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة أنواع وحدات الشركة.
