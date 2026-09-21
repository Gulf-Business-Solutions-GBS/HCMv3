---
helpId: pay.wage-type-leave.edit
lang: ar
title: إجازات أنواع الأجور — إنشاء / تحرير
entity: HCMv3.PayrollManagement.WageTypeLeave
surfaces:
  - angular/src/app/admin/payroll-management/wage-type-leaves/create-or-edit-wage-type-leave-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageTypeLeaves.Create]
---

## Overview

أنواع الأجور التي تتكوّن منها دفعة نوع معيّن من الإجازات. فدفعة الإجازة وقيمة الإجازة المستحقة ومخصص الإجازة السنوية كلها تجمع المبالغ الشهرية لأنواع الأجور المدرجة هنا لنوع الغياب ذاك.

## WageTypeId {#WageTypeLeave.WageTypeId}
<!-- alias: loc:WageTypeId, dom:WageTypeLeave_WageTypeId -->

**ما هو.** نوع أجر مُدرج في دفعة هذه الإجازة.

**مثال.** بدل السكن

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

## AbsenceTypeId {#WageTypeLeave.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:WageTypeLeave_AbsenceTypeId -->

**ما هو.** نوع الإجازة.

**مثال.** الإجازة السنوية

**لماذا يهم.** لا يُدرج نوع الأجر إلا مرة واحدة لنوع الغياب الواحد.

**القواعد.** إلزامي. يُختار من قائمة أنواع الغياب.
