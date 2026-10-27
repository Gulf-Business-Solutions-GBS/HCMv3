---
helpId: pay.ticket-pax-type.edit
lang: ar
title: أنواع ركاب التذاكر — إنشاء / تحرير
entity: HCMv3.PayrollManagement.TicketPaxType
surfaces:
  - angular/src/app/admin/payroll-management/ticket-pax-types/create-or-edit-ticket-pax-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TicketPaxTypes.Create]
---

## Overview

نوع مسافر بحسب العمر -- بالغ أو طفل أو رضيع -- يُستخدم لاختيار أجرة المسافر. ويحدد عمر العائل في ذلك اليوم أي أجرة لنوع مسافر تُستخدم.

## AgeFrom {#TicketPaxType.AgeFrom}
<!-- alias: loc:AgeFrom, dom:TicketPaxType_AgeFrom -->

**ما هو.** أصغر عمر في نوع المسافر هذا.

**مثال.** `2`

**القواعد.** اختياري. رقم.

## AgeTo {#TicketPaxType.AgeTo}
<!-- alias: loc:AgeTo, dom:TicketPaxType_AgeTo -->

**ما هو.** أكبر عمر في نوع المسافر هذا.

**مثال.** `11`

**لماذا يهم.** اجعل نطاقات أعمار أنواع المسافرين غير متداخلة، وإلا طُبِّق أول تطابق.

**القواعد.** اختياري. رقم.
