---
helpId: pay.employee-sub-group-payment.edit
lang: ar
title: مدفوعات المجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupPayment
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/payments/create-or-edit-employee-sub-group-payment-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupPayments.Create]
---

## Overview

دفعة دائمة لكل من في الدرجة، تُصرف عبر نوع أجر.

ولا يُستخدم إلا فاصلان للدفع: فالأسطر *سنوية* تُصرف مرة في السنة في **شهر الدفع** الخاص بها، والأسطر *نهاية الخدمة* تُصرف في تشغيل نهاية خدمة الموظف. أما الأسطر ذات الفواصل الأخرى فلا تُصرف.

## WageType {#EmployeeSubGroupPayment.WageTypeId}
<!-- alias: loc:WageType, dom:EmployeeSubGroupPayment_WageTypeId -->

**ما هو.** نوع الأجر الذي تُصرف تحته الدفعة.

**مثال.** المكافأة السنوية

**القواعد.** اختياري. يُختار من قائمة.

## Amount {#EmployeeSubGroupPayment.Amount}
<!-- alias: loc:Amount, dom:EmployeeSubGroupPayment_Amount -->

**ما هو.** المبلغ المصروف.

**مثال.** `300`

**القواعد.** اختياري. رقم.

## PayInterval {#EmployeeSubGroupPayment.PayInterval}
<!-- alias: loc:PayInterval, dom:EmployeeSubGroupPayment_PayInterval -->

**ما هو.** متى تُصرف الدفعة.

**مثال.** سنوي

**لماذا يهم.** لا يُصرف إلا *سنوي* و*نهاية الخدمة*.

**القواعد.** إلزامي. إحدى القيم: شهري، يومي، بالساعة، نهاية الخدمة، سنوي.

## PayMonth {#EmployeeSubGroupPayment.PayMonth}
<!-- alias: loc:PayMonth, dom:EmployeeSubGroupPayment_PayMonth -->

**ما هو.** الشهر الذي تُصرف فيه الدفعة *السنوية*.

**مثال.** ديسمبر

**القواعد.** إلزامي. إحدى القيم: لا شيء، يناير، فبراير، مارس، أبريل، مايو، يونيو، يوليو، أغسطس، سبتمبر، أكتوبر، نوفمبر، ديسمبر.
