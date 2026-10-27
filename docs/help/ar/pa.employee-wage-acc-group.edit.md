---
helpId: pa.employee-wage-acc-group.edit
lang: ar
title: مجموعات حسابات أجور الموظف — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeWageAccGroup
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/wage-acc-groups/create-or-edit-employee-wage-acc-group-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeWageAccGroups.Create]
---

## Overview

مجموعة حسابات الأجور المخصصة للموظف اعتباراً من تاريخ معين. عند حفظ مجموعة جديدة تُغلق السابقة في اليوم السابق لتاريخ البداية الجديد. ولا يجوز تسجيل المجموعة نفسها مرتين بتاريخ البداية نفسه.

تُسجَّل للرجوع إليها، ولا يقرؤها محرك التكلفة ولا القيود المحاسبية حالياً.

## WageAccGroupId {#EmployeeWageAccGroup.WageAccGroupId}
<!-- alias: loc:WageAccGroupId, dom:EmployeeWageAccGroup_WageAccGroupId -->

**ما هو.** مجموعة حسابات الأجور.

**مثال.** موظفو المكتب الرئيسي

**القواعد.** إلزامي. يُختار من قائمة مجموعات حسابات الأجور.

:::admin
تُدار من إدارة الرواتب ← مجموعات حسابات الأجور.
:::
