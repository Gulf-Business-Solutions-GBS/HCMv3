---
helpId: wf.workflow-group.edit
lang: ar
title: مجموعات سير العمل — إنشاء / تحرير
entity: HCMv3.Workflow.WorkflowGroup
surfaces:
  - angular/src/app/admin/workflow/workflow-groups/create-or-edit-workflow-group-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowGroups.Create]
---

## Overview

مجموعة من اختيارات السلاسل لفئة من الموظفين، بحيث يستخدم موظفو المكتب الرئيسي مثلاً سلسلة إجازات مختلفة. تأتي مجموعة الموظف من تعيينه التنظيمي الحالي، ويستخدم الموظف الذي ليس له مجموعة المجموعة الافتراضية. ولا تُستخدم المجموعات إلا عند تفعيل إعداد سير العمل EnableWorkflowGroup.

## IsDefault {#WorkflowGroup.IsDefault}
<!-- alias: loc:IsDefault, dom:WorkflowGroup_IsDefault -->

**ما هو.** ما إذا كانت هذه مجموعة الموظفين الذين ليست لهم مجموعة.

**لماذا يهم.** لا يمكن أن تكون إلا مجموعة واحدة افتراضية.

**القواعد.** نعم أو لا.
