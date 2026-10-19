---
helpId: pa.employee-loan.edit
lang: ar
title: قروض الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeLoan
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/loans/create-or-edit-employee-loan-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeLoans.Create]
---

## Overview

قرض للموظف وطريقة استرداده. تسترده الرواتب اعتباراً من تاريخ بدء الاسترداد وفق نوع القرض؛ ويُسجَّل كل قسط تقتطعه استرداداً للقرض فينقص الرصيد المستحق.

لا يجوز أن يقل المبلغ الفعلي عن المبلغ المستحق، ولا أن يسبق تاريخ بدء الاسترداد أو تاريخ الانتهاء تاريخَ البداية.

يفتح خيار **الاستردادات والإعفاءات**، في قائمة صف القرض ضمن قسم القروض في سجل الموظف، مبالغ السداد والأشهر المُعفاة للقرض في نافذة واحدة، تحت المبلغ الفعلي للقرض والمبلغ المستحق والرصيد المستحق. وتُضاف الاستردادات والإعفاءات وتُعدَّل هناك بالنوافذ نفسها، وبالقواعد نفسها، المستخدمة في شاشتي استردادات قروض الموظف وإعفاءات قروض الموظف، مع تعبئة القرض مسبقاً. أما الاستردادات التي يكتبها تشغيل الرواتب (استرداد عبر الرواتب) فتظهر ولا يمكن تعديلها أو حذفها هناك، لأن التشغيل التالي لتلك الفترة يحذفها ويعيد كتابتها.

## LoanTypeId {#EmployeeLoan.LoanTypeId}
<!-- alias: loc:LoanTypeId, dom:EmployeeLoan_LoanTypeId -->

**ما هو.** نوع القرض.

**مثال.** قرض شخصي

**لماذا يهم.** يحدد نوع الاقتطاع وخيار الاسترداد فيه طريقة استرداد الرواتب للقرض.

**القواعد.** إلزامي. يُختار من قائمة أنواع القروض.

:::admin
تُدار من البيانات الأساسية العامة ← أنواع القروض. وإذا كان نوع الاقتطاع متغيراً توقفت الرواتب بخطأ عند معالجة الموظف.
:::

## RecoveryStartDate {#EmployeeLoan.RecoveryStartDate}
<!-- alias: loc:RecoveryStartDate, dom:EmployeeLoan_RecoveryStartDate -->

**ما هو.** التاريخ الذي تبدأ فيه الرواتب استرداد القرض.

**مثال.** 1 أكتوبر 2026

**لماذا يهم.** تسترد الرواتب اعتباراً من أول مسيرة تنتهي في هذا التاريخ أو بعده. ويجب أن تقع الإعفاءات بين هذا التاريخ وتاريخ انتهاء القرض.

**القواعد.** إلزامي. تاريخ.

## ActualAmount {#EmployeeLoan.ActualAmount}
<!-- alias: loc:ActualAmount, dom:EmployeeLoan_ActualAmount -->

**ما هو.** المبلغ الذي يستلمه الموظف.

**مثال.** 1200.000

**لماذا يهم.** لا يجوز أن يقل عن المبلغ المستحق.

**القواعد.** اختياري. رقم.

## DueAmount {#EmployeeLoan.DueAmount}
<!-- alias: loc:DueAmount, dom:EmployeeLoan_DueAmount -->

**ما هو.** المبلغ الواجب استرداده.

**مثال.** 1200.000

**لماذا يهم.** يبدأ الرصيد المستحق بهذا المبلغ عند إنشاء القرض. وعند صرف القرض عبر الرواتب، يكون هو المبلغ المصروف.

**القواعد.** اختياري. رقم.

## FixedAmount {#EmployeeLoan.FixedAmount}
<!-- alias: loc:FixedAmount, dom:EmployeeLoan_FixedAmount -->

**ما هو.** القسط الذي تقتطعه الرواتب كل شهر.

**مثال.** 100.000

**لماذا يهم.** لا تقتطع الرواتب أكثر من الرصيد المتبقي المستحق، فقد يكون القسط الأخير أصغر.

**القواعد.** اختياري. رقم.

## RecoverFracInLastTrans {#EmployeeLoan.RecoverFracInLastTrans}
<!-- alias: loc:RecoverFracInLastTrans, dom:EmployeeLoan_RecoverFracInLastTrans -->

**ما هو.** هل يُضاف الباقي الصغير إلى القسط الأخير.

**لماذا يهم.** عندما لا يكون هناك استرداد لاحق مجدول ويكون الرصيد المتبقي بعد القسط أصغر من القسط، تضيفه الرواتب ليُسدَّد القرض.

**القواعد.** نعم أو لا.

## IncludeInLoanPay {#EmployeeLoan.IncludeInLoanPay}
<!-- alias: loc:IncludeLoanPaymentInPayroll, dom:EmployeeLoan_IncludeInLoanPay -->

**ما هو.** هل يُصرف القرض للموظف عبر الرواتب.

**لماذا يهم.** عند تحديده، تصرف الرواتب المبلغ المستحق في شهر الرواتب الذي يبدأ فيه القرض.

**القواعد.** نعم أو لا.
