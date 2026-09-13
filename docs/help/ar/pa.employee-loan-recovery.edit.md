---
helpId: pa.employee-loan-recovery.edit
lang: ar
title: استردادات قروض الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeLoanRecovery
surfaces:
  - angular/src/app/admin/personnel-administration/employee-loan-recoveries/create-or-edit-employee-loan-recovery-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeLoanRecoveries.Create]
---

## Overview

سداد على أحد قروض الموظف. وتسجل الرواتب أقساطها هنا باسترداد عبر الرواتب.

لا يجوز أن يسبق تاريخ الاسترداد تاريخ بدء استرداد القرض، ولا أن يزيد مجموع الاستردادات على المبلغ المستحق للقرض. ولا يؤدي حفظ استرداد في هذا النموذج إلى تحديث الرصيد المستحق للقرض؛ إذ تحدّثه الرواتب عند الاسترداد.

## EmployeeLoanId {#EmployeeLoanRecovery.EmployeeLoanId}
<!-- alias: loc:EmployeeLoanId, dom:EmployeeLoanRecovery_EmployeeLoanId -->

**ما هو.** القرض المسدَّد.

**القواعد.** إلزامي. يُختار من قائمة.

## RecoveryDate {#EmployeeLoanRecovery.RecoveryDate}
<!-- alias: loc:RecoveryDate, dom:EmployeeLoanRecovery_RecoveryDate -->

**ما هو.** تاريخ السداد.

**مثال.** 31 أكتوبر 2026

**لماذا يهم.** لا يجوز أن يسبق تاريخ بدء استرداد القرض.

**القواعد.** إلزامي. تاريخ.

## RecoveryAmount {#EmployeeLoanRecovery.RecoveryAmount}
<!-- alias: loc:RecoveryAmount, dom:EmployeeLoanRecovery_RecoveryAmount -->

**ما هو.** المبلغ المسدَّد.

**مثال.** 100.000

**لماذا يهم.** لا يجوز أن تتجاوز جميع استردادات القرض مجتمعةً مبلغه المستحق.

**القواعد.** اختياري. رقم.

## LoanRecoveryMethod {#EmployeeLoanRecovery.LoanRecoveryMethod}
<!-- alias: loc:LoanRecoveryMethod, dom:EmployeeLoanRecovery_LoanRecoveryMethod -->

**ما هو.** طريقة السداد.

**مثال.** استرداد خارجي

**لماذا يهم.** تتعرف الرواتب على صفوفها بطريقة الاسترداد عبر الرواتب، وتستبدلها عند إعادة احتساب الشهر.

**القواعد.** إلزامي. إحدى القيم: الاسترداد عن طريق الرواتب، الاسترداد خارجياً، الاسترداد من مكافأة نهاية الخدمة، إعفاء من الاسترداد، استرداد إضافي عن طريق الرواتب.
