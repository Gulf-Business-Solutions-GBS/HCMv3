---
helpId: pay.absence-type-exception.edit
lang: ar
title: استثناءات أنواع الغياب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AbsenceTypeException
surfaces:
  - angular/src/app/admin/payroll-management/absence-type-exceptions/create-or-edit-absence-type-exception-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceTypeExceptions.Create]
---

## Overview

أزواج من أنواع الإجازات لا ينبغي الجمع بينها. لا يُتحقق منها حالياً: تُحفظ الأزواج، لكن لا شيء يمنع أخذ النوعين معاً.

## AbsenceTypes {#AbsenceTypeException.AbsenceTypeId}
<!-- alias: loc:AbsenceTypes, dom:AbsenceTypeException_AbsenceTypeId -->

**ما هو.** نوع الإجازة.

**مثال.** الإجازة السنوية

**القواعد.** إلزامي. يُختار من قائمة أنواع الإجازات.

## ExceptAbsenceType {#AbsenceTypeException.ExceptAbsenceTypeId}
<!-- alias: loc:ExceptAbsenceType, dom:AbsenceTypeException_ExceptAbsenceTypeId -->

**ما هو.** نوع الإجازة الذي لا ينبغي الجمع بينه وبينه. ولا يجوز أن يستثني النوع نفسه.

**مثال.** الإجازة المرضية

**القواعد.** إلزامي. يُختار من قائمة أنواع الإجازات.
