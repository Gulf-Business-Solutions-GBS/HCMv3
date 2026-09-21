---
helpId: pay.employee-sub-group-bonus.edit
lang: ar
title: مكافآت المجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupBonus
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/bonuses/create-or-edit-employee-sub-group-bonus-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupBonuses.Create]
---

## Overview

مكافأة الخدمة للمجموعة الفرعية: نسبة تُصرف في ذكرى الالتحاق بالعمل. ففي شهر ذكرى التحاق الموظف يبحث نظام الرواتب عن السطر الذي يساوي **تغيير السنة** فيه عدد السنوات التي أكملها الموظف، ويصرف تلك النسبة من المجموع الشهري لأنواع الأجور المؤشَّر عليها *إدراج في مكافأة الخدمة*.

ولا تُصرف إلا الذكرى التي لها سطر -- فأضف سطراً لكل ذكرى تستحق مكافأة.

## ChangeYear {#EmployeeSubGroupBonus.ChangeYear}
<!-- alias: loc:ChangeYear, dom:EmployeeSubGroupBonus_ChangeYear -->

**ما هو.** سنوات الخدمة المكتملة التي يُصرف عليها هذا السطر.

**مثال.** `5` -- تُصرف في شهر الذكرى الخامسة.

**القواعد.** اختياري. رقم.

:::admin
لا يُتحقق من تاريخي بداية السطر ونهايته عند صرف المكافأة، فالمهم المجموعة الفرعية وعدد السنوات فقط.
:::

## Percentage {#EmployeeSubGroupBonus.Percentage}
<!-- alias: loc:Percentage, dom:EmployeeSubGroupBonus_Percentage -->

**ما هو.** النسبة المئوية من أجر مكافأة الخدمة التي تُصرف مكافأةً.

**مثال.** `50`

**القواعد.** اختياري. رقم.
