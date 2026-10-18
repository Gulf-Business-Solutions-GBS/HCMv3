---
helpId: wf.request-master.edit
lang: ar
title: الطلبات الأساسية — إنشاء / تحرير
entity: HCMv3.Workflow.RequestMaster
surfaces:
  - angular/src/app/admin/workflow/request-masters/create-or-edit-request-master-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.RequestMasters.Create]
---

## Overview

سلسلة موافقات: تسلسل الخطوات المسمّى الذي يمر به الطلب. وتُعدّ كل خطوة في تفاصيل الطلب الخاصة بالسلسلة. ويجد الطلب سلسلته بهذا الترتيب: قاعدة عمل تسمّي سلسلة برمزها، ثم مجموعة سير العمل الخاصة بالموظف (عند تفعيل إعداد سير العمل EnableWorkflowGroup)، ثم السلسلة الافتراضية لنوع المعاملة. وإن لم يُعطِ أيٌّ منها سلسلة، لا يمكن تقديم الطلب ويظهر سبب ذلك.

## SecurityLevel {#RequestMaster.SecurityLevel}
<!-- alias: loc:SecurityLevel, dom:RequestMaster_SecurityLevel -->

**ما هو.** من يمكنه رؤية الطلبات على هذه السلسلة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. إحدى القيم: EmployeeOnly، EmployeeManager، EmployeeDepartment، الجميع.

## DefaultApprover {#RequestMaster.DefaultApproverId}
<!-- alias: loc:DefaultApprover, dom:RequestMaster_DefaultApproverId -->

**ما هو.** الموظف الذي يوافق عندما لا تجد الخطوة أحداً.

**مثال.** مدير الموارد البشرية

**لماذا يهم.** يُستخدم كملاذ أخير في أي خطوة: عندما لا يوجد من يشغل دور الخطوة، أو تنتهي سلسلة المديرين، أو يكون كل المرشحين قد وقّعوا خطوة توقيع سابقة. وإذا فوّض هذا الموظف موافقاته، يذهب الطلب إلى من ينوب عنه.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## AutoCloseInDays {#RequestMaster.AutoCloseInDays}
<!-- alias: loc:AutoCloseInDays, dom:RequestMaster_AutoCloseInDays -->

**ما هو.** عدد الأيام بعد الموافقة التي يُغلق بعدها الطلب تلقائياً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم صحيح.

## DueInDays {#RequestMaster.DueInDays}
<!-- alias: loc:DueInDays, dom:RequestMaster_DueInDays -->

**ما هو.** عدد الأيام المتاحة لإنجاز الطلب.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم صحيح.

## AutoClose {#RequestMaster.AutoClose}
<!-- alias: loc:AutoClose, dom:RequestMaster_AutoClose -->

**ما هو.** ما إذا كانت الطلبات الموافق عليها تُغلق تلقائياً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## IsCommentRequire {#RequestMaster.IsCommentRequire}
<!-- alias: loc:IsCommentRequired, dom:RequestMaster_IsCommentRequire -->

**ما هو.** ما إذا كان على المعتمدين كتابة تعليق.

**لماذا يهم.** لا يُقرأ حالياً. الخانة نفسها في كل خطوة هي التي تُطبَّق.

**القواعد.** نعم أو لا.
