---
helpId: pay.wage-type-register.edit
lang: ar
title: سجلات أنواع الأجور — إنشاء / تحرير
entity: HCMv3.PayrollManagement.WageTypeRegister
surfaces:
  - angular/src/app/admin/payroll-management/wage-type-registers/create-or-edit-wage-type-register-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageTypeRegisters.Create]
---

## Overview

سطر سجل لفترة رواتب. لا يُقرأ حالياً: لا يكتب نظام الرواتب هذه الأسطر ولا يقرؤها، وهي محفوظة للرجوع إليها فقط.

## EmployeeId {#WageTypeRegister.EmployeeId}
<!-- alias: loc:EmployeeId, dom:WageTypeRegister_EmployeeId -->

**ما هو.** الموظف الذي يخصه السطر.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## ForPeriod {#WageTypeRegister.ForPeriod}
<!-- alias: loc:ForPeriod, dom:WageTypeRegister_ForPeriod -->

**ما هو.** الفترة التي يخصها السطر.

**مثال.** `202604`

**القواعد.** إلزامي. رقم صحيح.

## InPeriod {#WageTypeRegister.InPeriod}
<!-- alias: loc:InPeriod, dom:WageTypeRegister_InPeriod -->

**ما هو.** الفترة التي سُجِّل فيها السطر.

**مثال.** `202605`

**القواعد.** إلزامي. رقم صحيح.
