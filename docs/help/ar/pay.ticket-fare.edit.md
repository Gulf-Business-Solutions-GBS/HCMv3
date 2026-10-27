---
helpId: pay.ticket-fare.edit
lang: ar
title: أجور التذاكر — إنشاء / تحرير
entity: HCMv3.PayrollManagement.TicketFare
surfaces:
  - angular/src/app/admin/payroll-management/ticket-fares/create-or-edit-ticket-fare-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TicketFares.Create]
---

## Overview

الأجرة لمسار واحد ونوع مسافر ودرجة تذكرة، اعتباراً من تاريخ معيّن.

والتذاكر المقيَّمة بـ*FareAmount* تأخذ أجرة مسار الموظف، ودرجة التذكرة لمجموعته الفرعية، ونوع المسافر بحسب عمره. ويقيّم مخصص التذاكر الشهري التذاكر المتراكمة بأجرة الذهاب والعودة.

## TicketRouteId {#TicketFare.TicketRouteId}
<!-- alias: loc:TicketRouteId, dom:TicketFare_TicketRouteId -->

**ما هو.** المسار.

**مثال.** الكويت – القاهرة

**القواعد.** إلزامي. يُختار من قائمة مسارات التذاكر.

## TicketPaxTypeId {#TicketFare.TicketPaxTypeId}
<!-- alias: loc:TicketPaxTypeId, dom:TicketFare_TicketPaxTypeId -->

**ما هو.** نوع المسافر، بحسب العمر.

**مثال.** بالغ

**القواعد.** إلزامي. يُختار من قائمة أنواع ركاب التذاكر.

## TicketClassId {#TicketFare.TicketClassId}
<!-- alias: loc:TicketClassId, dom:TicketFare_TicketClassId -->

**ما هو.** درجة التذكرة.

**مثال.** الدرجة السياحية

**القواعد.** إلزامي. يُختار من قائمة درجات التذاكر.

## OneWayTicketFare {#TicketFare.OneWayTicketFare}
<!-- alias: loc:OneWayTicketFare, dom:TicketFare_OneWayTicketFare -->

**ما هو.** أجرة الذهاب فقط.

**مثال.** `95`

**لماذا يهم.** تُستخدم إذا كانت قاعدة التذاكر للمجموعة الفرعية على الذهاب فقط، ولا يجوز أن تكون سالبة.

**القواعد.** اختياري. رقم.

## ReturnTicketFare {#TicketFare.ReturnTicketFare}
<!-- alias: loc:ReturnTicketFare, dom:TicketFare_ReturnTicketFare -->

**ما هو.** أجرة الذهاب والعودة.

**مثال.** `170`

**لماذا يهم.** تُستخدم إذا كانت قاعدة التذاكر للمجموعة الفرعية على الذهاب والعودة، ولمخصص التذاكر الشهري. ولا يجوز أن تكون سالبة.

**القواعد.** اختياري. رقم.
