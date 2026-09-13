---
helpId: pa.employee-dotted-line-manager.edit
lang: ar
title: المدراء غير المباشرين للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeDottedLineManager
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/dotted-line-managers/create-or-edit-employee-dotted-line-manager-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDottedLineManagers.Create]
---

## Overview

مدير ثانوي (خط منقط) لفترة محددة -- كقائد مشروع يتبع له الموظف أيضاً.

يُسجَّل للرجوع إليه فقط: فالمدير الثانوي لا يرى الموظف في الخدمة الذاتية، ولا يعتمد طلباته، ولا تصله إشعاراته.

## Manager {#EmployeeDottedLineManager.ManagerId}
<!-- alias: loc:Manager, dom:EmployeeDottedLineManager_ManagerId -->

**ما هو.** المدير الثانوي خلال هذه الفترة.

**مثال.** خالد الرشيدي (1044)

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
