---
helpId: perf.paemployee.edit
lang: ar
title: موظفو تقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAEmployee
surfaces:
  - angular/src/app/admin/performance-management/pa-employees/create-or-edit-pa-employee-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PAEmployees.Create]
---

## Overview

تقييم شامل (360) لموظف واحد: الدورة ونوع النموذج والنتيجة -- الزيادة المقترحة والمسمى الوظيفي المقترح ومجالات التحسين. وتُحفظ مؤشراته ومراجعوه وتقديراته في تفاصيله.

ويُنشأ التقييم بشأن الموظف لا من قِبله، ويمر بسير العمل للاعتماد. ويعرض تقرير تقييم الموظفين حقول نتيجته.

## Employee {#PAEmployee.EmployeeId}
<!-- alias: loc:Employee, dom:PAEmployee_EmployeeId -->

**ما هو.** الموظف المُقيَّم.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## PACycle {#PAEmployee.PACycleId}
<!-- alias: loc:PACycle, dom:PAEmployee_PACycleId -->

**ما هو.** دورة التقييم. مطلوبة لتقديم التقييم.

**مثال.** التقييم السنوي 2026

**القواعد.** إلزامي. يُختار من قائمة دورات تقييم الأداء.

## FormType {#PAEmployee.FormTypeId}
<!-- alias: loc:FormType, dom:PAEmployee_FormTypeId -->

**ما هو.** نوع النموذج.

**لماذا يهم.** يحدّ الحد الأقصى للمراجعين فيه عدد المراجعين الذين يمكن إضافتهم. وهو مطلوب لتقديم التقييم.

**القواعد.** إلزامي. يُختار من قائمة أنواع النماذج.

## ProposedIncrement {#PAEmployee.ProposedIncrement}
<!-- alias: loc:ProposedIncrement, dom:PAEmployee_ProposedIncrement -->

**ما هو.** زيادة الراتب المقترحة.

**مثال.** `5%`

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## ProposedJobTitle {#PAEmployee.ProposedJobTitle}
<!-- alias: loc:ProposedJobTitle, dom:PAEmployee_ProposedJobTitle -->

**ما هو.** المسمى الوظيفي المقترح.

**مثال.** محاسب أول

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## AreaOfImprovement {#PAEmployee.AreaOfImprovement}
<!-- alias: loc:AreaOfImprovement, dom:PAEmployee_AreaOfImprovement -->

**ما هو.** المجالات التي ينبغي أن يتحسن فيها الموظف.

**القواعد.** اختياري.
