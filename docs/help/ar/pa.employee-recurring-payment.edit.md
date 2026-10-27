---
helpId: pa.employee-recurring-payment.edit
lang: ar
title: المدفوعات المتكررة للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeRecurringPayment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/recurring-payments/create-or-edit-employee-recurring-payment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRecurringPayments.Create]
---

## Overview

مبلغ ثابت يُصرف للموظف أو يُخصم منه على نوع أجر واحد خلال فترة -- كبدل السيارة أو بدل الهاتف أو خصم شهري ثابت. ويُفتح من قسم المدفوعات المتكررة في سجل الموظف.

تقرأ الرواتب المدفوعات الشهرية والسنوية. فالدفعة الشهرية تضيف مبلغها كاملاً عن كل شهر تمسّه فترتها، ولو غطّت جزءاً منه فقط. والدفعة السنوية تضيف مبلغها مرة واحدة في السنة، في شهر تاريخ بدايتها. أما الدفعات اليومية وبالساعة ونهاية الخدمة فتُحفظ ولا تُصرف حالياً.

يجب أن يكون المبلغ أكبر من صفر، ولا يجوز أن تتداخل دفعتان من نوع الأجر نفسه للموظف نفسه. وعند تفعيل خيار تفعيل تاريخ السريان في الدفعات المتكررة في إعدادات الرواتب، فإن الدفعة الجديدة المفتوحة (تاريخ نهايتها 31/12/9999) تُغلق الدفعة المفتوحة الحالية في اليوم السابق لبدايتها، بدلاً من رفضها بسبب التداخل.

## WageTypeId {#EmployeeRecurringPayment.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeRecurringPayment_WageTypeId -->

**ما هو.** نوع الأجر الذي يُصرف عليه المبلغ أو يُخصم.

**مثال.** بدل سيارة

**لماذا يهم.** تضيف الرواتب المبلغ إلى بند نوع الأجر هذا في كشف الراتب، فنوع الفوترة في نوع الأجر هو الذي يحدد أهو استحقاق أم خصم.

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

:::admin
تُدار من إدارة الرواتب ← أنواع الأجور.
:::

## PayInterval {#EmployeeRecurringPayment.PayInterval}
<!-- alias: loc:PayInterval, dom:EmployeeRecurringPayment_PayInterval -->

**ما هو.** عدد مرات صرف المبلغ.

**مثال.** شهري

**لماذا يهم.** الشهري يُصرف في كل شهر من الفترة، والسنوي يُصرف مرة في السنة في شهر تاريخ البداية. ولا تصرف الرواتب حالياً الدفعات اليومية أو بالساعة أو نهاية الخدمة.

**القواعد.** إلزامي. إحدى القيم: شهري، يومي، بالساعة، نهاية الخدمة، سنوي.

## Amount {#EmployeeRecurringPayment.Amount}
<!-- alias: loc:Amount, dom:EmployeeRecurringPayment_Amount -->

**ما هو.** المبلغ الذي يُصرف في كل مرة، بعملة الرواتب.

**مثال.** 75.000

**لماذا يهم.** يُصرف كاملاً عن كل شهر مستحق، ولا يُنقص عن جزء الشهر.

**القواعد.** إلزامي. رقم.
