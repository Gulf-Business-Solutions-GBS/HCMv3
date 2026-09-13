---
helpId: pa.employee-ticket-route.edit
lang: ar
title: مسارات تذاكر الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeTicketRoute
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/ticket-routes/create-or-edit-employee-ticket-route-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTicketRoutes.Create]
---

## Overview

المسار الذي تُسعَّر عليه تذاكر طيران الموظف خلال فترة. ولا يجوز أن تتداخل مسارات الموظف نفسه.

يأخذ احتسابا مخصص التذاكر ومتوسط التذكرة الأجرة لهذا المسار.

## TicketRouteId {#EmployeeTicketRoute.TicketRouteId}
<!-- alias: loc:TicketRouteId, dom:EmployeeTicketRoute_TicketRouteId -->

**ما هو.** المسار.

**مثال.** الكويت – القاهرة

**لماذا يهم.** تُستخرج أجرة التذكرة لهذا المسار؛ وإن لم يكن للمسار صف أجرة يبقى بند المخصص دون تغيير.

**القواعد.** إلزامي. يُختار من قائمة مسارات التذاكر.

:::admin
تُدار من إدارة الرواتب ← مسارات التذاكر، والأجور من إدارة الرواتب ← أجور التذاكر.
:::
