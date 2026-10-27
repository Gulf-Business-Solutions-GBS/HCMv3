---
helpId: pa.employee-manager.edit
lang: ar
title: مدراء الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeManager
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/managers/create-or-edit-employee-manager-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeManagers.Create]
---

## Overview

المدير الذي يتبع له الموظف اعتباراً من تاريخ معين. تشكّل سجلات المدير تاريخاً متتابعاً: فعند حفظ سجل جديد يُغلق السابق في اليوم السابق لتاريخ البداية الجديد.

المدير الساري اليوم هو من يرى هذا الموظف ضمن مرؤوسيه المباشرين في الخدمة الذاتية، وتصله طلبات الموظف في خطوات اعتماد المدير، ويُنسخ على الإشعارات المحددة لإرسالها إلى المدير.

## Manager {#EmployeeManager.ManagerId}
<!-- alias: loc:Manager, dom:EmployeeManager_ManagerId -->

**ما هو.** المدير المباشر للموظف اعتباراً من هذا التاريخ.

**مثال.** فاطمة العنزي (1002)

**لماذا يهم.** يحدد ظهور الفريق في الخدمة الذاتية، وخطوات اعتماد المدير (ومنها المستويات الأعلى التي تتبع مدير كل مدير بالتتابع)، وإشعارات المدير، والمخطط التنظيمي، وتقارير المديرين. ويُتجاهل في كل ذلك أي سجل يجعل الموظف مديراً لنفسه.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
