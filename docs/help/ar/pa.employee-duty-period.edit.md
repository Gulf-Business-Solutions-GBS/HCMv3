---
helpId: pa.employee-duty-period.edit
lang: ar
title: فترات دوام الموظف — إنشاء / تحرير
entity: HCMv3.TimeManagement.EmployeeDutyPeriod
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/duty-periods/create-or-edit-employee-duty-period-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeDutyPeriods.Create]
---

## Overview

نمط الدوام الذي يعمل به الموظف اعتباراً من تاريخ معين. عند حفظ نمط جديد يُغلق السابق في اليوم السابق لتاريخ البداية الجديد.

يُؤخذ دوام الموظف لكل يوم من الدوام البديل لذلك التاريخ بعينه إن وُجد، وإلا فمن فترة الدوام السارية. ويحدد ذلك أيام العمل في الإجازات، والوردية المتوقعة في محرك الوقت، وعدد الأيام في الرواتب.

## DutyPeriod {#EmployeeDutyPeriod.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:EmployeeDutyPeriod_DutyPeriodId -->

**ما هو.** فترة الدوام -- نمط الدوام الأسبوعي -- المخصصة اعتباراً من هذا التاريخ.

**مثال.** الأحد–الخميس 8:00–16:00

**لماذا يهم.** يُؤخذ دوام كل يوم من نمط هذه الفترة لذلك اليوم من الأسبوع.

**القواعد.** إلزامي. يُختار من قائمة فترات الدوام.

:::admin
تحتاج الأنماط الدوّارة إلى نقطة بداية للتدوير، وهذا النموذج لا يحددها؛ لذلك يستخدم التعيين المحفوظ هنا دائماً نمط أيام الأسبوع. وتُدار فترات الدوام من إدارة الوقت ← فترات الدوام.
:::
