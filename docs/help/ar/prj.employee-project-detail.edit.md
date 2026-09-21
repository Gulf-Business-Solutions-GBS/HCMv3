---
helpId: prj.employee-project-detail.edit
lang: ar
title: تفاصيل مشاريع الموظف — إنشاء / تحرير
entity: HCMv3.ProjectManagement.EmployeeProjectDetail
surfaces:
  - angular/src/app/admin/project-management/employee-project-details/create-or-edit-employee-project-detail-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.EmployeeProjectDetails.Create]
---

## Overview

مشروع واحد ضمن تعيين مشاريع الموظف. ويجب أن تقع تواريخه ضمن تواريخ التعيين.

## EmployeeProject {#EmployeeProjectDetail.EmployeeProjectId}
<!-- alias: loc:EmployeeProject, dom:EmployeeProjectDetail_EmployeeProjectId -->

**ما هو.** تعيين مشاريع الموظف.

**القواعد.** إلزامي. يُختار من قائمة.

## Project {#EmployeeProjectDetail.ProjectId}
<!-- alias: loc:Project, dom:EmployeeProjectDetail_ProjectId -->

**ما هو.** المشروع.

**القواعد.** إلزامي. يُختار من قائمة المشاريع.

## Percentage {#EmployeeProjectDetail.Percentage}
<!-- alias: loc:Percentage, dom:EmployeeProjectDetail_Percentage -->

**ما هو.** حصة الراتب المحمّلة على هذا المشروع.

**مثال.** 60

**لماذا يهم.** يُستخدم مع طريقة النسبة المئوية: يجب أن يكون أكبر من 0 وحتى 100، ولا يجوز أن يتجاوز مجموع المشاريع 100. ويُضرب فيه العدد والمبلغ لكل بند راتب محمَّل.

**القواعد.** إلزامي. رقم.
