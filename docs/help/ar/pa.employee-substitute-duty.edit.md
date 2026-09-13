---
helpId: pa.employee-substitute-duty.edit
lang: ar
title: الدوامات البديلة للموظف — إنشاء / تحرير
entity: HCMv3.TimeManagement.EmployeeSubstituteDuty
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/substitute-duties/create-or-edit-employee-substitute-duty-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeSubstituteDuties.Create]
---

## Overview

دوام مختلف للموظف في تاريخ واحد محدد -- كتغطية وردية ليلية. دوام بديل واحد لكل موظف في كل تاريخ.

يتقدّم الدوام البديل على فترة دوام الموظف في ذلك التاريخ، أينما بُحث عن دوام الموظف لليوم: أيام العمل في الإجازات، والتراكم، وبيانات الوقت، وعدد الأيام في الرواتب.

## SubstituteDate {#EmployeeSubstituteDuty.SubstituteDate}
<!-- alias: loc:SubstituteDate, dom:EmployeeSubstituteDuty_SubstituteDate -->

**ما هو.** التاريخ الذي ينطبق عليه الدوام البديل.

**مثال.** 14 أغسطس 2026

**لماذا يهم.** لا يجوز أن يكون للموظف أكثر من دوام بديل في هذا التاريخ.

**القواعد.** إلزامي. تاريخ.

## Duty {#EmployeeSubstituteDuty.DutyId}
<!-- alias: loc:Duty, dom:EmployeeSubstituteDuty_DutyId -->

**ما هو.** الدوام المعمول به في ذلك التاريخ.

**مثال.** الوردية الليلية 22:00–06:00

**القواعد.** إلزامي. يُختار من قائمة الدوامات.

:::admin
يُدار من إدارة الوقت ← الدوامات.
:::
