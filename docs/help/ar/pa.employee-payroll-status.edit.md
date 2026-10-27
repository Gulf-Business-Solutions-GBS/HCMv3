---
helpId: pa.employee-payroll-status.edit
lang: ar
title: حالات رواتب الموظف — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeePayrollStatus
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/payroll-status/create-or-edit-employee-payroll-status-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeePayrollStatuses.Create]
---

## Overview

المدى الذي وصلت إليه معالجة الموظف في الرواتب ومحرك الوقت، والتواريخ التي تتحكم في إعادة الاحتساب. ولا يجوز أن يكون للموظف أكثر من سجل واحد.

ينشئ محرك الوقت هذا السجل عند أول معالجة للموظف، وتتوقف الرواتب برسالة «Payroll Status not found» للموظف الذي ليس له سجل. ويحدّث المحركان التواريخ أثناء تشغيلهما؛ فلا تغيّرها إلا لفرض إعادة احتساب أو تقييدها.

## EarliestRetroAccountingDate {#EmployeePayrollStatus.EarliestRetroAccountingDate}
<!-- alias: loc:EarliestRetroAccountingDate, dom:EmployeePayrollStatus_earliestRetroAccountingDate -->

**ما هو.** أقدم تاريخ يجوز أن تعود إليه إعادة الاحتساب.

**مثال.** 1 يناير 2026

**لماذا يهم.** إذا امتد تغيير إلى ما قبل هذا التاريخ، يتوقف محرك الوقت والرواتب العادية للموظف برسالة «EarliestRetroAccountingDate set to …»، ما لم يكن التاريخ تاريخ الالتحاق.

**القواعد.** اختياري. تاريخ.

## AccountedTo {#EmployeePayrollStatus.AccountedTo}
<!-- alias: loc:AccountedTo, dom:EmployeePayrollStatus_accountedTo -->

**ما هو.** آخر تاريخ دفع عالجته الرواتب للموظف.

**مثال.** 31 أغسطس 2026

**لماذا يهم.** تعيد الرواتب الاحتساب من الشهر التالي لهذا التاريخ حتى تاريخ الدفع الحالي، وتقدّمه بعد كل تشغيل.

**القواعد.** اختياري. تاريخ.

## RunPayrollUpto {#EmployeePayrollStatus.RunPayrollUpto}
<!-- alias: loc:RunPayrollUpto, dom:EmployeePayrollStatus_runPayrollUpto -->

**ما هو.** تاريخ يجب أن تُشغَّل الرواتب حتى بلوغه.

**لماذا يهم.** يُسجَّل فقط، ولا يقرؤه أي من المحركين حالياً.

**القواعد.** اختياري. تاريخ.

## EarliestMasterDataChange {#EmployeePayrollStatus.EarliestMasterDataChange}
<!-- alias: loc:EarliestMasterDataChange, dom:EmployeePayrollStatus_earliestMasterDataChange -->

**ما هو.** أقدم تاريخ تأثر بتغيير على سجلات الموظف منذ آخر تشغيل للرواتب.

**مثال.** 1 يونيو 2026

**لماذا يهم.** تعيد الرواتب احتساب كل شهر اعتباراً من هذا التاريخ، ويبدأ منه محرك الوقت عند إيقاف التشغيل التلقائي. ويحدده محرك الوقت عند تغيّر يوم، وتمسحه الرواتب بعد تشغيل ناجح.

**القواعد.** اختياري. تاريخ.

## DoNoAccountAfter {#EmployeePayrollStatus.DoNoAccountAfter}
<!-- alias: loc:DoNoAccountAfter, dom:EmployeePayrollStatus_doNoAccountAfter -->

**ما هو.** آخر فترة يجوز للرواتب احتسابها للموظف.

**مثال.** 31 ديسمبر 2026

**لماذا يهم.** تتخطى الرواتب ومحرك تكلفة المشاريع أي فترة بعد هذا التاريخ.

**القواعد.** اختياري. تاريخ.

## EarliestRecalculationDate {#EmployeePayrollStatus.EarliestRecalculationDate}
<!-- alias: loc:EarliestRecalculationDate, dom:EmployeePayrollStatus_earliestRecalculationDate -->

**ما هو.** تاريخ أقدم إعادة احتساب.

**لماذا يهم.** يُسجَّل فقط، ولا يقرؤه أي من المحركين حالياً.

**القواعد.** اختياري. تاريخ.

## PersonalCalendarFrom {#EmployeePayrollStatus.PersonalCalendarFrom}
<!-- alias: loc:PersonalCalendarFrom, dom:EmployeePayrollStatus_personalCalendarFrom -->

**ما هو.** تاريخ بدء التقويم الشخصي للموظف.

**لماذا يهم.** يُسجَّل فقط، ولا يقرؤه أي من المحركين حالياً.

**القواعد.** اختياري. تاريخ.

## NextTimeEvaluationFrom {#EmployeePayrollStatus.NextTimeEvaluationFrom}
<!-- alias: loc:NextTimeEvaluationFrom, dom:EmployeePayrollStatus_nextTimeEvaluationFrom -->

**ما هو.** التاريخ الذي سيعالج منه محرك الوقت الموظف في المرة القادمة.

**مثال.** 1 سبتمبر 2026

**لماذا يهم.** يقدّمه محرك الوقت أثناء تشغيله. وتتوقف مسيرة الرواتب خارج الدورة، عدا نهاية الخدمة، برسالة «Time Engine not run» إذا سبق هذا التاريخ تاريخَ الدفع.

**القواعد.** اختياري. تاريخ.

## PersonLocked {#EmployeePayrollStatus.PersonLocked}
<!-- alias: loc:PersonLocked, dom:EmployeePayrollStatus_PersonLocked -->

**ما هو.** يحدد الموظف مقفلاً للرواتب.

**لماذا يهم.** يُحفظ فقط؛ ولا تتخطى الرواتب حالياً الموظف المقفل.

**القواعد.** نعم أو لا.

## PayrollCorrection {#EmployeePayrollStatus.PayrollCorrection}
<!-- alias: loc:PayrollCorrection, dom:EmployeePayrollStatus_PayrollCorrection -->

**ما هو.** يحدد أن الموظف يحتاج إلى تصحيح في الرواتب.

**لماذا يهم.** يُحفظ فقط؛ ولا يقرؤه أي محرك حالياً.

**القواعد.** نعم أو لا.
