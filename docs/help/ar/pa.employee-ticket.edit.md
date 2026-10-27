---
helpId: pa.employee-ticket.edit
lang: ar
title: تذاكر الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeTicket
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/tickets/create-or-edit-employee-ticket-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTickets.Create]
---

## Overview

استحقاق تذكرة طيران أُخذ أو صُرف للموظف أو أحد المعالين.

عندما تصرف الدرجة للموظف تذاكر الطيران تلقائياً، ينشئ محرك الوقت هذه السجلات بنفسه في يوم بدء فترة تراكم كل تذكرة. ويقرأ احتساب متوسط التذكرة في الرواتب التذاكرَ في أحدث تاريخ دفع. ولا تحتسب رواتب HCMv3 حالياً صرف تذاكر الطيران من هذه السجلات.

## TicketTypeId {#EmployeeTicket.TicketTypeId}
<!-- alias: loc:TicketTypeId, dom:EmployeeTicket_TicketTypeId -->

**ما هو.** لمن التذكرة -- للموظف نفسه أو لنوع تذكرة معال.

**مثال.** الموظف

**القواعد.** إلزامي. يُختار من قائمة أنواع التذاكر.

:::admin
تُدار من إدارة الرواتب ← أنواع التذاكر.
:::

## TicketDate {#EmployeeTicket.TicketDate}
<!-- alias: loc:TicketDate, dom:EmployeeTicket_TicketDate -->

**ما هو.** تاريخ التذكرة.

**مثال.** 1 يوليو 2026

**القواعد.** إلزامي. تاريخ.

## PaymentDate {#EmployeeTicket.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeTicket_PaymentDate -->

**ما هو.** تاريخ صرف التذكرة أو استخدامها.

**مثال.** 1 يوليو 2026

**لماذا يهم.** تُحتسب التذاكر المصروفة ضمن فترة التراكم مستخدمةً عندما يحسب محرك الوقت الاستحقاق المتبقي.

**القواعد.** إلزامي. تاريخ.

## Compensate {#EmployeeTicket.Compensate}
<!-- alias: loc:Compensate, dom:EmployeeTicket_Compensate -->

**ما هو.** عدد التذاكر.

**مثال.** 1

**لماذا يهم.** يطرحه محرك الوقت من الاستحقاق، ويضربه احتساب متوسط التذكرة في أجرة المسار.

**القواعد.** اختياري. رقم.

## TicketPaymentType {#EmployeeTicket.TicketPaymentType}
<!-- alias: loc:TicketPaymentType, dom:EmployeeTicket_TicketPaymentType -->

**ما هو.** متى تُصرف التذكرة.

**مثال.** مع الرواتب

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** إلزامي. إحدى القيم: الدفع مع الرواتب، الدفع مقدماً.

## Amount {#EmployeeTicket.Amount}
<!-- alias: loc:Amount, dom:EmployeeTicket_Amount -->

**ما هو.** مبلغ التذكرة، عندما يحل محل أجرة المسار.

**مثال.** 180.000

**لماذا يهم.** يملؤه محرك الوقت من استثناء ميزة أو استثناء معال.

**القواعد.** اختياري. رقم.

## AdvancedTicketPaid {#EmployeeTicket.AdvancedTicketPaid}
<!-- alias: loc:AdvancedTicketPaid, dom:EmployeeTicket_AdvancedTicketPaid -->

**ما هو.** علامة لدفعة تذكرة مقدمة.

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه الرواتب ولا التقارير حالياً.

**القواعد.** اختياري. رقم صحيح.

## OverrideAmount {#EmployeeTicket.OverrideAmount}
<!-- alias: loc:OverrideAmount, dom:EmployeeTicket_OverrideAmount -->

**ما هو.** هل يحل المبلغ محل أجرة المسار.

**لماذا يهم.** يحدده محرك الوقت عندما يأخذ المبلغ من استثناء.

**القواعد.** نعم أو لا.
