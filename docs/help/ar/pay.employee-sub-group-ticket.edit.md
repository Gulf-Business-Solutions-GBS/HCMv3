---
helpId: pay.employee-sub-group-ticket.edit
lang: ar
title: تذاكر المجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupTicket
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/tickets/create-or-edit-employee-sub-group-ticket-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupTickets.Create]
---

## Overview

استحقاق المجموعة الفرعية من تذاكر الطيران لنوع تذكرة واحد -- تذكرة الموظف نفسه أو تذكرة عائل: كم تذكرة، وعلى أي فترة، وكيف تُقيَّم كل تذكرة.

وتُختار طريقة تقييم التذكرة في المجموعة الفرعية نفسها (**نوع دفع تذكرة الطيران**)، ويقدّم هذا السطر المبالغ التي يستخدمها ذلك الاختيار.

## TicketType {#EmployeeSubGroupTicket.TicketTypeId}
<!-- alias: loc:TicketType, dom:EmployeeSubGroupTicket_TicketTypeId -->

**ما هو.** نوع التذكرة الذي يخصه هذا الاستحقاق.

**مثال.** تذكرة الموظف

**لماذا يهم.** نوع التذكرة `1` هو تذكرة الموظف نفسه، والبقية للعائلين.

**القواعد.** اختياري. يُختار من قائمة.

## TicketCount {#EmployeeSubGroupTicket.TicketCount}
<!-- alias: loc:TicketCount, dom:EmployeeSubGroupTicket_TicketCount -->

**ما هو.** عدد التذاكر المكتسبة في كل فترة استحقاق.

**مثال.** `1`

**لماذا يهم.** يراكمه محرك الوقت يوماً بيوم -- على سنة، أو على سنتين لفترة *نصف سنوي* -- إلا إذا كان نوع التذكرة يُستحق فوراً. ويستخدمه الصرف التلقائي للتذاكر استحقاقاً.

**القواعد.** اختياري. رقم صحيح.

## MaxAmount {#EmployeeSubGroupTicket.MaxAmount}
<!-- alias: loc:MaxAmount, dom:EmployeeSubGroupTicket_MaxAmount -->

**ما هو.** أقصى قيمة للتذكرة الواحدة.

**مثال.** `250`

**لماذا يهم.** *ActualAmountWithinMaxlimit* لا يتجاوزه أبداً، و*المبلغ الفعلي* يستخدمه إذا لم يُدفع شيء فعلاً.

**القواعد.** اختياري. رقم.

## FixAmount {#EmployeeSubGroupTicket.FixAmount}
<!-- alias: loc:FixAmount, dom:EmployeeSubGroupTicket_FixAmount -->

**ما هو.** قيمة التذكرة الواحدة، لنوع الدفع *مبلغ ثابت*.

**مثال.** `180`

**القواعد.** اختياري. رقم.

## FareType {#EmployeeSubGroupTicket.FareType}
<!-- alias: loc:FareType, dom:EmployeeSubGroupTicket_FareType -->

**ما هو.** ما إذا كانت تُستخدم أجرة الذهاب فقط أو أجرة الذهاب والعودة، لنوع الدفع *FareAmount*.

**مثال.** ذهاب وعودة

**القواعد.** إلزامي. إحدى القيم: OneWay، Return.

## AgeLimit {#EmployeeSubGroupTicket.AgeLimit}
<!-- alias: loc:AgeLimit, dom:EmployeeSubGroupTicket_AgeLimit -->

**ما هو.** لتذكرة العائل، أكبر عمر يكون فيه العائل مستحقاً.

**مثال.** `21`

**لماذا يهم.** يتجاوز الصرف التلقائي للتذاكر العائلين الأكبر من هذا العمر، والعائلين الذين لم يُسجَّل تاريخ ميلادهم، ما لم يكن لهم استثناء تذكرة.

**القواعد.** اختياري. رقم.

:::admin
حد العمر 0 يعني ألا يستحق أي عائل -- لا يعني "بلا حد". حدّد أكبر عمر يستحق.
:::

## AccrualPeriod {#EmployeeSubGroupTicket.AccrualPeriod}
<!-- alias: loc:AccrualPeriod, dom:EmployeeSubGroupTicket_AccrualPeriod -->

**ما هو.** الفترة التي يسري عليها استحقاق التذاكر.

**مثال.** سنة العمل

**لماذا يهم.** يضيف الصرف التلقائي للتذاكر التذكرة في أول يوم من كل فترة. و*نصف سنوي* يوزّع التراكم على سنتين.

**القواعد.** إلزامي. إحدى القيم: بلا تراكم، السنة الميلادية، السنة المالية، سنة العمل، نصف سنوي، لكل فترة توظيف، مخصص.
