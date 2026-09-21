---
helpId: prj.project-register.edit
lang: ar
title: سجلات المشاريع — إنشاء / تحرير
entity: HCMv3.ProjectManagement.ProjectRegister
surfaces:
  - angular/src/app/admin/project-management/project-registers/create-or-edit-project-register-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.ProjectRegisters.Create]
---

## Overview

تكاليف راتب الموظف لفترة واحدة، موزعة حسب المشروع. يبنيه محرك تكلفة المشاريع من سجل رواتب الفترة، ولا يعمل إلا لرواتب منتظمة يكون سجل التحكم فيها مقفلاً وحالياً، وإعادة تشغيله تستبدل الأرقام. يكتبه محرك تكلفة المشاريع، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## Employee {#ProjectRegister.EmployeeId}
<!-- alias: loc:Employee, dom:ProjectRegister_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Period {#ProjectRegister.Period}
<!-- alias: loc:Period, dom:ProjectRegister_Period -->

**ما هو.** فترة الرواتب.

**القواعد.** إلزامي. تاريخ.
