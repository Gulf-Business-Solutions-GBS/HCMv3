---
helpId: pa.employee-fn-fpayment.edit
lang: ar
title: الدفعات الكاملة والنهائية للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeFnFPayment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/fnf-payments/create-or-edit-employee-fnf-payment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeFnFPayments.Create]
---

## Overview

التسوية النهائية الكاملة للموظف عند نهاية الخدمة.

تشمل مسيرة نهاية الخدمة الموظفين الذين يقع تاريخ تعويضهم في فترتها، وتحتاج إلى تسوية يساوي تاريخ تعويضها تاريخ دفع المسيرة. أما المبالغ والأيام والأعداد في هذا النموذج فتُسجَّل للرجوع إليها: إذ تحتسب الرواتب التسوية بنفسها ولا تقرؤها حالياً.

## CompensationDate {#EmployeeFnFPayment.CompensationDate}
<!-- alias: loc:CompensationDate, dom:EmployeeFnFPayment_CompensationDate -->

**ما هو.** تاريخ صرف التسوية.

**مثال.** 15 سبتمبر 2026

**لماذا يهم.** يحدد مسيرة نهاية الخدمة التي تشمل الموظف. ويُظهر تقرير بيانات الموظفين التسوية منجزة بعد مرور هذا التاريخ.

**القواعد.** إلزامي. تاريخ.

## EmployeeActionId {#EmployeeFnFPayment.EmployeeActionId}
<!-- alias: loc:EmployeeActionId, dom:EmployeeFnFPayment_EmployeeActionId -->

**ما هو.** الإجراء الشخصي الذي تنتمي إليه هذه التسوية، إن وُجد.

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. يُختار من قائمة.

## EmployeeBasicPayDetailId {#EmployeeFnFPayment.EmployeeBasicPayDetailId}
<!-- alias: loc:EmployeeBasicPayDetailId, dom:EmployeeFnFPayment_EmployeeBasicPayDetailId -->

**ما هو.** بند الأجر الأساسي الذي تستند إليه التسوية، إن وُجد.

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. يُختار من قائمة.

## LeaveCompensateDays {#EmployeeFnFPayment.LeaveCompensateDays}
<!-- alias: loc:LeaveCompensateDays, dom:EmployeeFnFPayment_LeaveCompensateDays -->

**ما هو.** أيام الإجازة المعوَّضة.

**مثال.** 18

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.

## GratuityCompensateDays {#EmployeeFnFPayment.GratuityCompensateDays}
<!-- alias: loc:GratuityCompensateDays, dom:EmployeeFnFPayment_GratuityCompensateDays -->

**ما هو.** أيام مكافأة نهاية الخدمة المعوَّضة.

**مثال.** 255

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.

## IndemnityCompensateAmount {#EmployeeFnFPayment.IndemnityCompensateAmount}
<!-- alias: loc:IndemnityCompensateAmount, dom:EmployeeFnFPayment_IndemnityCompensateAmount -->

**ما هو.** مبلغ مكافأة نهاية الخدمة المعوَّض.

**مثال.** 4250.000

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.

## IndemnityPayAdjustment {#EmployeeFnFPayment.IndemnityPayAdjustment}
<!-- alias: loc:IndemnityPayAdjustment, dom:EmployeeFnFPayment_IndemnityPayAdjustment -->

**ما هو.** تعديل على صرف مكافأة نهاية الخدمة.

**مثال.** -50.000

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.

## TicketCompensateCount {#EmployeeFnFPayment.TicketCompensateCount}
<!-- alias: loc:TicketCompensateCount, dom:EmployeeFnFPayment_TicketCompensateCount -->

**ما هو.** تذاكر الموظف المعوَّضة.

**مثال.** 1

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.

## Dependant1TicketCompensateCount {#EmployeeFnFPayment.Dependant1TicketCompensateCount}
<!-- alias: loc:Dependant1TicketCompensateCount, dom:EmployeeFnFPayment_Dependant1TicketCompensateCount -->

**ما هو.** التذاكر المعوَّضة للمعال الأول.

**مثال.** 1

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.

## Dependant2TicketCompensateCount {#EmployeeFnFPayment.Dependant2TicketCompensateCount}
<!-- alias: loc:Dependant2TicketCompensateCount, dom:EmployeeFnFPayment_Dependant2TicketCompensateCount -->

**ما هو.** التذاكر المعوَّضة للمعال الثاني.

**مثال.** 1

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.

## Dependant3TicketCompensateCount {#EmployeeFnFPayment.Dependant3TicketCompensateCount}
<!-- alias: loc:Dependant3TicketCompensateCount, dom:EmployeeFnFPayment_Dependant3TicketCompensateCount -->

**ما هو.** التذاكر المعوَّضة للمعال الثالث.

**مثال.** 0

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.

## CompensateLoanAmount {#EmployeeFnFPayment.CompensateLoanAmount}
<!-- alias: loc:CompensateLoanAmount, dom:EmployeeFnFPayment_CompensateLoanAmount -->

**ما هو.** مبلغ القرض المسوّى.

**مثال.** 300.000

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.

## CompensateBonusPay {#EmployeeFnFPayment.CompensateBonusPay}
<!-- alias: loc:CompensateBonusPay, dom:EmployeeFnFPayment_CompensateBonusPay -->

**ما هو.** مبلغ المكافأة المسوّى.

**مثال.** 500.000

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم.
