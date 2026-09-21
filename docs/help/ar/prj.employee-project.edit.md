---
helpId: prj.employee-project.edit
lang: ar
title: مشاريع الموظف — إنشاء / تحرير
entity: HCMv3.ProjectManagement.EmployeeProject
surfaces:
  - angular/src/app/admin/project-management/employee-projects/create-or-edit-employee-project-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.EmployeeProjects.Create]
---

## Overview

المشاريع التي يعمل عليها الموظف خلال فترة، وكيفية توزيع راتبه بينها. يجب أن يكون تاريخ البدء أول يوم في الشهر وتاريخ الانتهاء آخر يوم في الشهر، ولا يجوز أن تتداخل فترات الموظف، ويجب إضافة مشروع واحد على الأقل.

## Employee {#EmployeeProject.EmployeeId}
<!-- alias: loc:Employee, dom:EmployeeProject_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## CostingMethod {#EmployeeProject.CostingMethod}
<!-- alias: loc:CostingMethod, dom:EmployeeProject_CostingMethod -->

**ما هو.** كيفية توزيع الراتب بين المشاريع.

**مثال.** النسبة المئوية

**لماذا يهم.** النسبة المئوية: يُوزَّع كل بند راتب حسب النسب المحددة على المشاريع. الأيام الفعلية: يُوزَّع كل بند حسب أيام العمل في فترة كل مشروع، مأخوذة من سجل الوقت، بينما تُوزَّع الإجازات بالتساوي. أما الساعات الفعلية فلا ينتج عنها أي تكلفة مشاريع، كما في النظام القديم.

**القواعد.** إلزامي. إحدى القيم: الساعات الفعلية، الأيام الفعلية، النسبة المئوية.
