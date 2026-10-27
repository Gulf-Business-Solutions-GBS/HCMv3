---
helpId: pay.employee-sub-group-fund.edit
lang: ar
title: صناديق المجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupFund
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/funds/create-or-edit-employee-sub-group-fund-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupFunds.Create]
---

## Overview

شريحة أجر واحدة من خصم الصندوق للمجموعة الفرعية -- مثل صندوق ادخار أو صندوق ضمان. وأجر الموظف للصندوق هو المجموع الشهري لأنواع الأجور المؤشَّر عليها *إدراج في الصندوق*، والشريحة التي يقع هذا المجموع ضمن نطاقها تحدد الخصم.

ولا يُخصم إلا للموظفين المشتركين في صندوق، ولا يُخصم ما دام لهم إعفاء من الصندوق أو بعد آخر يوم لهم في الخدمة.

## FromPay {#EmployeeSubGroupFund.FromPay}
<!-- alias: loc:FromPay, dom:EmployeeSubGroupFund_FromPay -->

**ما هو.** أدنى أجر في هذه الشريحة.

**مثال.** `0`

**لماذا يهم.** لا يجوز أن يكون "من" أكبر من "إلى".

**القواعد.** اختياري. رقم.

## ToPay {#EmployeeSubGroupFund.ToPay}
<!-- alias: loc:ToPay, dom:EmployeeSubGroupFund_ToPay -->

**ما هو.** أعلى أجر في هذه الشريحة.

**مثال.** `999`

**القواعد.** اختياري. رقم.

## FundDedType {#EmployeeSubGroupFund.FundDedType}
<!-- alias: loc:FundDedType, dom:EmployeeSubGroupFund_FundDedType -->

**ما هو.** طريقة احتساب الخصم لهذه الشريحة.

**مثال.** نسبة مئوية

**لماذا يهم.** *مبلغ ثابت* يخصم المبلغ الثابت. و*نسبة مئوية* يخصم النسبة من الأجر. و*PercentageWithinLimit* يخصم النسبة، على ألا تزيد على الحد الأقصى ولا تقل عن الحد الأدنى. وتُقرَّب النتيجة إلى منزلتين عشريتين.

**القواعد.** إلزامي. إحدى القيم: النسبة المئوية، PercentageWithinLimit، مبلغ ثابت.

## Percentage {#EmployeeSubGroupFund.Percentage}
<!-- alias: loc:Percentage, dom:EmployeeSubGroupFund_Percentage -->

**ما هو.** النسبة المئوية المخصومة من الأجر، لنوعي النسبة المئوية.

**مثال.** `5`

**القواعد.** اختياري. رقم.

## MinAmount {#EmployeeSubGroupFund.MinAmount}
<!-- alias: loc:MinAmount, dom:EmployeeSubGroupFund_MinAmount -->

**ما هو.** أقل ما يُخصم، لـ*PercentageWithinLimit*.

**مثال.** `10`

**القواعد.** اختياري. رقم.

## MaxAmount {#EmployeeSubGroupFund.MaxAmount}
<!-- alias: loc:MaxAmount, dom:EmployeeSubGroupFund_MaxAmount -->

**ما هو.** أقصى ما يُخصم، لـ*PercentageWithinLimit*.

**مثال.** `100`

**القواعد.** اختياري. رقم.

## FixAmount {#EmployeeSubGroupFund.FixAmount}
<!-- alias: loc:FixAmount, dom:EmployeeSubGroupFund_FixAmount -->

**ما هو.** المبلغ المخصوم، لـ*مبلغ ثابت*.

**مثال.** `25`

**القواعد.** اختياري. رقم.
