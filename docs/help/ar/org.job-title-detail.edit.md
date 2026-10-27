---
helpId: org.job-title-detail.edit
lang: ar
title: تفاصيل المسميات الوظيفية — إنشاء / تحرير
entity: HCMv3.OrganizationManagement.JobTitleDetail
surfaces:
  - angular/src/app/admin/organization-management/job-title-details/create-or-edit-job-title-detail-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.JobTitleDetails.Create]
---

## Overview

أدوار سير العمل الافتراضية للمسمى الوظيفي: يُدرج كل سطر دور سير عمل واحداً يتولاه شاغلو ذلك المسمى الوظيفي.

يُحتفظ بهذه الأسطر جاهزةً لخطوات الاعتماد المتوازية، ولا تُقرأ حالياً. ولا يزال توجيه المعتمدين يتم بحسب الأدوار الممنوحة في سجل كل موظف.

## JobTitle {#JobTitleDetail.JobTitleId}
<!-- alias: loc:JobTitle, dom:JobTitleDetail_JobTitleId -->

**ما هو.** المسمى الوظيفي الذي يُدرج له دور سير العمل.

**مثال.** مدير الموارد البشرية

**القواعد.** إلزامي. يُختار من قائمة المسميات الوظيفية.

## WorkflowRole {#JobTitleDetail.WorkflowRoleId}
<!-- alias: loc:WorkflowRole, dom:JobTitleDetail_WorkflowRoleId -->

**ما هو.** دور سير عمل يتولاه شاغلو المسمى الوظيفي بصفة افتراضية.

**مثال.** معتمد الموارد البشرية

**لماذا يهم.** يجوز أن يُدرج للمسمى الوظيفي عدة أدوار، على ألا يتكرر الدور الواحد. ويُحتفظ بها لخطوات الاعتماد المتوازية، التي ترجع إلى هذه الأدوار إذا لم تحدد الخطوة أي دور؛ ولا توجد في HCMv3 خطوة اعتماد متوازية بعد، فلا يقرؤها شيء حالياً.

**القواعد.** إلزامي. يُختار من قائمة أدوار سير العمل.
